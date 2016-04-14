using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Jdforsythe.MySQLConnection {

  /// <summary>
  ///  MySQLConnection makes it easier to use MySQL in your Visual Studio applications by abstracting the implementation of the
  ///  MySQL .NET Connector away so you can work with strictly native .NET data types.
  /// </summary>
  /// <remarks>
  ///   This class implements the IDisposable interface and as such should be used exclusively with the Using() pattern to ensure
  ///   proper cleanup of MySQL connections and prevent dormant, dangling persistent connections.
  /// </remarks>
  /// <example>
  ///   <code>
  ///     string myValue;
  /// 
  ///     using (MySqlConn sql = new MySqlConn(connectionString)) {
  ///       sql.Query = "SELECT my_column FROM table LIMIT 1";
  ///       myValue = sql.selectQueryForSingleValue();
  ///     } // here the connection is closed and the resources are properly disposed by the Garbage Collector
  /// 
  ///     Console.WriteLine(myValue);
  ///   </code>
  /// </example>
  public class MySQLConn : IDisposable {

    private bool _isDisposed = false;
    private MySqlConnection _connection = null;
    private Dictionary<string, object> _parameters = new Dictionary<string, object>();

    /// <summary>The query string for querying the MySQL database</summary>
    /// <remarks>Use the @ParamName syntax along with the addParam() method to safely parameterize your queries</remarks>
    public string Query { get; set; }

    /// <summary>
    ///   The base class constructor uses a full connection string, like one for the MySQL .NET Connector
    ///   <see cref="https://dev.mysql.com/doc/connector-net/en/connector-net-programming-connecting-open.html"/>
    /// </summary>
    /// <param name="connectionString">The connection string</param>
    public MySQLConn(string connectionString) {
    if (this._isDisposed)
      throw new ObjectDisposedException("Cannot open connection - class has been disposed.");

      if (string.IsNullOrEmpty(connectionString))
        throw new Exception("Invalid connection parameters for database.");

      try {
        this._connection = new MySqlConnection(connectionString);
        this._connection.Open();
      }
      catch {
        throw;
      }
      
    }

    /// <summary>
    ///   The class constructor for passing the connection parameters as separate strings
    /// </summary>
    /// <param name="server">The address to the MySQL server</param>
    /// /// <param name="database">The name of the database to connect to</param>
    /// <param name="username">The username to connect to the MySQL Database</param>
    /// <param name="password">The password to connect to the MySQL Database</param>
    /// <param name="options">The options string to use when connecting to the MySQL Databse <see cref="https://dev.mysql.com/doc/connector-net/en/connector-net-programming-connecting-open.html"/></param>
    public MySQLConn(string server, string database, string username, string password, string options = "")
      : this(ConstructConnectionStringFromParameters(server, database, username, password, options)) {}
    
    /// <summary>When using parameterized queries, this method allows you to add the name and value of parameters. The name should match the @ParamName used in the query</summary>
    /// <param name="key">The @ParamName name of the parameter to add</param>
    /// <param name="value">The value of the parameter</param>
    public void addParam(string key, object value) {
      this._parameters.Add(key, value);
    }

    /// <summary>When using parameterized queries, this method allows you to remove a parameter that has already been set</summary>
    /// <param name="key">The @ParamName name of the parameter to remove</param>
    public void removeParam(string key) {
      this._parameters.Remove(key);
    }

    /// <summary>When using parameterized queries, this method allows you to update an existing parameter's value, or add it if it doesn't already exist</summary>
    /// <remarks>This is useful when you need to reuse a connection within a Using() statement without having to .clearParams() when you just need to change one or two things</remarks>
    /// <param name="key">The @ParamName name of the parameter to update/add</param>
    /// <param name="value">The value of the parameter</param>
    public void updateParam(string key, object value) {
      if (this._parameters.ContainsKey(key)) {
        this._parameters[key] = value;
      }
      else {
        this._parameters.Add(key, value);
      }
    }

    /// <summary>When using parameterized queries, this method allows you to clear out all currently-set parameters and values</summary>
    public void clearParams() {
      this._parameters.Clear();
    }

    /// <summary>This method allows you to run a query that expects a single value back (i.e. one column from one record)</summary>
    /// <returns>The string value result of the query, or <c>null</c> if there is no result</returns>
    public string selectQueryForSingleValue() {
      CheckIfInstanceIsReadyForQuery();

      string single_value = null;

      using (MySqlCommand command = new MySqlCommand(this.Query, this._connection)) {
        AddParametersToCommand(command);

        object dbValue = null;

        try {
          dbValue = command.ExecuteScalar();
        }
        catch {
          throw;
        }

        if (dbValue == DBNull.Value) {
          single_value = null;
        }
        else { 
          single_value = Convert.ToString(dbValue);
        }
      }

      return single_value;
    }

    /// <summary>This method allows you to run a query that expects any number of columns, but one single record (e.g. <c>SELECT * FROM table_name LIMIT 1</c>)</summary>
    /// <returns>A <c>Dictionary&lt;string, string&gt;</c> representation of the record resulting from the query, or <c>null</c> if there is no result</returns>
    public Dictionary<string, string> selectQueryForSingleRecord() {
      CheckIfInstanceIsReadyForQuery();

      // we'll return null if there are no results, rather than an empty Dictionary
      Dictionary<string, string> record = null;

      using (MySqlCommand command = new MySqlCommand(this.Query, this._connection)) {
        AddParametersToCommand(command);

        try {
          using (MySqlDataReader reader = command.ExecuteReader()) {
            if (reader.HasRows) {
              record = new Dictionary<string, string>();

              // read the first row only
              reader.Read();

              // get the keys/values for the first record and add them to the dictionary
              for (int i = 0; i < reader.FieldCount; i++) {

                // catch null and don't set as empty string
                if (reader[i] == DBNull.Value) {
                  record.Add(reader.GetName(i), null);
                }
                else {
                  record.Add(reader.GetName(i), reader[i].ToString());
                }
              }

            }
          }

        }
        catch {
          throw;
        }

      }

      return record;
    }

    /// <summary>This method allows you to run a query that expects any number of records, but one single column (e.g. <c>SELECT field FROM table_name</c>)</summary>
    /// <returns>A <c>List&lt;string&gt;</c> listing the values returned from the query, or an empty <c>List&lt;string&gt;</c> if there is no result</returns>
    public List<string> selectQueryForSingleColumn() {
      CheckIfInstanceIsReadyForQuery();

      // if the result set is empty, we'll return an empty list
      List<string> allRecords = new List<string>();

      using (MySqlCommand command = new MySqlCommand(this.Query, this._connection)) {
        AddParametersToCommand(command);
        
        try {
          using (MySqlDataReader reader = command.ExecuteReader()) {

            if (reader.HasRows) {
              // only reader(0) because we only want one column of data
              while (reader.Read()) {
                if (reader[0] == DBNull.Value) {
                  allRecords.Add(null);
                }
                else {
                  allRecords.Add(reader[0].ToString());
                }
              }

            }
          }
        }
        catch {
          throw;
        }
      }

      return allRecords;
    }

    /// <summary>This method allows you to run a query that expects any number of columns, and any number of records (e.g. <c>SELECT * FROM table_name</c>)</summary>
    /// <returns>A <c>List&lt;Dictionary&lt;string, string&gt;&gt;</c> representation of the records resulting from the query, or an empty <c>List&lt;Dictionary&lt;string, string&gt;&gt;</c> if there is no result</returns>
    public List<Dictionary<string, string>> selectQuery() {
      CheckIfInstanceIsReadyForQuery();

      List<Dictionary<string, string>> allRecords = new List<Dictionary<string, string>>();

      using (MySqlCommand command = new MySqlCommand(this.Query, this._connection)) {
        AddParametersToCommand(command);

        try {
          using (MySqlDataReader reader = command.ExecuteReader()) {

            Dictionary<string, string> record;

            if (reader.HasRows) {

              while (reader.Read()) {
                // create a new dictionary for the record, adding each column and value
                record = new Dictionary<string, string>();

                for (int i = 0; i <= (reader.FieldCount - 1); i++) {
                  // catch null values, don't return as empty string
                  if (reader[i] == DBNull.Value) {
                    record.Add(reader.GetName(i), null);
                  }
                  else {
                    record.Add(reader.GetName(i), reader[i].ToString());
                  }
                }

                // add the dictionary to the List
                allRecords.Add(record);
              }
            }
          }
        }
        catch {
          throw;
        }
      }

      return allRecords;
    }

    /// <summary>This method allows you to run an <c>INSERT</c> query</summary>
    /// <returns>An integer value indicating the number of records affected by the query</returns>
    public int insertQuery() {
      CheckIfInstanceIsReadyForQuery();

      int affectedRecords = 0;

      using (MySqlCommand command = new MySqlCommand(this.Query, this._connection)) {
        AddParametersToCommand(command);

        try {
          affectedRecords = command.ExecuteNonQuery();
        }
        catch {
          throw;
        }
      }

      return affectedRecords;
    }

    /// <summary>This method allows you to run an <c>UPDATE</c> query</summary>
    /// <returns>An integer value indicating the number of records affected by the query</returns>
    public int updateQuery() {
      return insertQuery();
    }

    /// <summary>This method allows you to run a <c>DELETE</c> query</summary>
    /// <returns>An integer value indicating the number of records affected by the query</returns>
    public int deleteQuery() {
      return insertQuery();
    }


    /// <summary>This method allows you to use the MySQL <c>LOAD DATA INFILE</c> feature to bulk import records from a delimited text file</summary>
    /// <param name="tableName">The table to import records to</param>
    /// <param name="fieldTerminator">The terminator for each field (e.g. "," for CSV)</param>
    /// <param name="lineTerminator">The terminator for each line</param>
    /// <param name="filename">The path to the file to import</param>
    /// <param name="linesToSkip">The number of lines to skip (e.g. skip header row)</param>
    /// <returns>An integer value indicating the number of records affected by the import</returns>
    public int loadDataInfile(string tableName, string fieldTerminator, string lineTerminator, string filename, int linesToSkip) {
      MySqlBulkLoader bl = new MySqlBulkLoader(this._connection);

      bl.TableName = tableName;
      bl.FieldTerminator = fieldTerminator;
      bl.LineTerminator = lineTerminator;
      bl.FileName = filename;
      bl.NumberOfLinesToSkip = linesToSkip;

      try {
        return bl.Load();
      }
      catch {
        throw;
      }
    }


    /*
     * Private methods
     */

    private static string ConstructConnectionStringFromParameters(string server, string database, string username, string password, string options = "") {
      if (string.IsNullOrEmpty(server) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(database))
        throw new Exception("Invalid connection parameters for database.");
      return "Data Source=" + server + "; user id=" + username + "; password=" + password + "; database=" + database + "; " + options;
    }

    private void CheckIfInstanceIsReadyForQuery() {
      if (this._isDisposed)
        throw new ObjectDisposedException("Cannot execute query. Class has been disposed.");

      if (this._connection == null)
        throw new Exception("No connection opened.");

      if (string.IsNullOrEmpty(this.Query))
        throw new Exception("Cannot execute empty query.");
    }

    private void AddParametersToCommand(MySqlCommand command) {
      if (this._parameters.Count > 0) {
        foreach (KeyValuePair<string, object> kvp in this._parameters) {
          command.Parameters.AddWithValue(kvp.Key, kvp.Value);
        }
      }
    }

    /*
     * Implement IDispose
     */

    /// <summary>
    ///   This class implements IDisposable to properly dispose of the MySQL connections. As such, this class should be used exclusively with the Using() pattern
    ///   to ensure proper disposal of resources and prevent memory leaks and dormant persistent connections.
    /// </summary>
    /// <param name="disposing">Was Dispose() called from code</param>
    protected virtual void Dispose(bool disposing) {
      // if already disposed, don't do anything
      if (this._isDisposed)
        return;

      if ((disposing)) {
        this._connection.Close();
        this._connection = null;
        this._isDisposed = true;
      }

    }

    /// <summary>Implementation of the IDisposable interface</summary>
    public void Dispose() {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  
  }
}
