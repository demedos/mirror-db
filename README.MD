# MirrorDB - Oracle to sqLite DB converter

The title says it all

## Getting Started

Clone the repo, open the `.sln` file and adjust the parameters at the top of `Program.cs`. CTRL + F5 to run and enjoy

### Prerequisites

* A working Oracle DB connection
* Visual Studio (made with V15 but may work with older versions too)
* .NET 4.6 (older versions may work too)

### Parameters

At the top of the main file you will find the connection parameters used to connect to Oracle. Since the standard connection string was causing errors we use a TNS-like syntax.
If you don't know where to find this parameters just look up at the `tnsnames.ora` file on your pc, usually located here: `C:\app\<username>\product\<version>\client_1\network\admin\`.

| Parameter Name | Default Value
| -------------- |:-------------:|
| HOST           |               |
| PORT           | 1521          |
| SERVICE_NAME   |               |
| USERID         |               |
| PASSWORD       |               |

## Built With

* [Oracle Data Provider for .NET](https://docs.oracle.com/database/121/ODPNT/intro003.htm#ODPNT131) - The standard connection libraries
* [System.Data.SQLite](https://system.data.sqlite.org/index.html/doc/trunk/www/index.wiki) - SQLite file manipulation

## Authors

* **_demedos** - *Initial work* - [_demedos](https://github.com/demedos)

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details