# MySQLConnection

MySQLConnection makes it easier to use MySQL in your Visual Studio applications by
abstracting the implementation of the MySQL .NET Connector away so you can work with
strictly native .NET data types.

This class implements the `IDisposable` interface, and should be used _exclusively_
with the `using()` pattern to ensure proper cleanup of connections.

## Quick Example

```c#
string myValue;

using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "SELECT my_column FROM table LIMIT 1";
  myValue = sql.selectQueryForSingleValue();
}

Console.WriteLine(myValue);
```

## Create a connection

Again, always use `using()`. The `connectionString` parameter passed to the `MySQLConn`
constructor is the same format as you would use for the MySQL .NET Connector library:

[https://dev.mysql.com/doc/connector-net/en/connector-net-programming-connecting-open.html]

```c#
string connectionString = "server=127.0.0.1; uid=root; pwd=12345; database=test;";
// or
string connectionString = "Data Source=127.0.0.1; user id=root; password=12345; database=test;";
// and add any options at the end, such as AllowZeroDateTime=false

using (MySQLConn sql = new MySQLConn(connectionString)) {}
```

Or you can pass the parameters individually

```
// (server, database, username, password, options_string)
using (MySQLConn sql = new MySQLConn("127.0.0.1", "test", "root", "12345", "AllowZeroDateTime=false")) {}
```

## Setting a query

Queries are set on the `.Query` property.

```c#
sql.Query = "SELECT * FROM table";
```

### Parameterized queries

Parameters are set in the query the same way as the standard MySQL .NET Connector.

```c#
sql.Query = "SELECT * FROM table WHERE id=@IdNumber";
```

#### Add parameter value

```c#
sql.addParam("@IdNumber", "1");
```

#### Remove previously-set parameter value
```c#
sql.removeParam("@IdNumber");
```

#### Update previously-set parameter with new value
```c#
sql.updateParam("@IdNumber", "2");
```

#### Clear all previously-set parameters
```c#
sql.clearParams();
```

## Available SQL methods

### selectQueryForSingleValue()

* returns a `string`, or `NULL`, if there is no result from the database
* used when querying for a single column in a single record

```c#
string myValue;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "SELECT my_column FROM table LIMIT 1";
  myValue = sql.selectQueryForSingleValue();
}
```

### selectQueryForSingleRecord()

* returns a `Dictionary<string, string>`, or `NULL` if there is no result from the database
* used when querying for a single record but any number of columns
* dictionary is of form `<column_name, value>`

```c#
Dictionary<string, string> myRecord;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "SELECT * FROM my_column WHERE id=@IdNumber LIMIT 1";
  sql.addParam("@IdNumber", "42");
  myRecord = sql.selectQueryForSingleRecord();
}
```

### selectQueryForSingleColumn()

* returns a `List<string>`
* returns an empty `List<string>` if there is no result from the database (this allows a
  `foreach` on the result without a `NullReferenceException`)
* used when querying for a single column, but any number of records

```c#
List<string> myColVals;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "SELECT my_column FROM table";
  myColVals = sql.selectQueryForSingleColumn();
}
```

### selectQuery()

* returns a `List<Dictionary<string, string>>` representing a list of the records
* returns an empty `List<Dictionary<string, string>>` if there is no result from the
  database (this allows a `foreach` on the result without a `NullReferenceException`)
* used when querying for multiple columns and rows

```c#
List<Dictionary<string, string>> tableRows;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "SELECT * FROM table";
  tableRows = sql.selectQuery();
}
```

### insertQuery()

* returns an `int` indicating the number of affected rows

```c#
int affectedRows = 0;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "INSERT INTO table (my_column_1, my_column_2) VALUES (@Col1, @Col2)";
  sql.addParam("@Col1", "value_one");
  sql.addParam("@Col2", "value_two");
  affectedRows = sql.insertQuery();
}
```

### updateQuery()

* returns an `int` indicating the number of affected rows

```c#
int affectedRows = 0;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "UPDATE table SET my_column_1=@Col1, my_column_2=@Col2 WHERE id=@IdNumber LIMIT 1";
  sql.addParam("@Col1", "value_one");
  sql.addParam("@Col2", "value_two");
  sql.addParam("@IdNumber", "42");
  affectedRows = sql.updateQuery();
}
```

### deleteQuery()

* returns an `int` indicating the number of affected rows

```c#
int affectedRows = 0;
using (MySQLConn sql = new MySQLConn(connectionString)) {
  sql.Query = "DELETE FROM table WHERE id=@IdNumber";
  sql.addParam("@IdNumber", "42");
  affectedRows = sql.deleteQuery();
}
```

### loadDataInfile()

* returns an `int` indicating the number of affected rows
* uses `LOAD DATA INFILE` to load bulk records into the database

```c#
int affectedRows = 0;
string tableName = "table",
       fieldTerminator = ",",
       lineTerminator = "\r\n",
       filename = "infile.csv",
       linesToSkip = "1";

using (MySQLConn sql = new MySQLConn(connectionString)) {
  affectedRows = sql.loadDataInfile(tableName, fieldTerminator, lineTerminator, filename, linesToSkip);
}
```

## Exceptions

* Throws `ObjectDisposedException` if the class has been disposed - this probably shouldn't happen
* Throws generic `Exception` if no connection was opened (not connection error) - also probably shouldn't happen
* Throws generic `Exception` if the `Query` property is empty
* Throws generic `Exception` if the any of the connection paramters are null or empty strings
* Throws standard MySQL Connector exceptions for any database connection or query errors

## Contributions

Issues and pull requests are always welcome

## License

MIT
