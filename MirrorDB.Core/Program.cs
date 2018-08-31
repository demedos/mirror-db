using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirrorDB.Core
{
    class Program
    {
        #region Connection parameters
        const string HOST = "HOST";
        const string PORT = "1521"; // Default port: 1521
        const string SERVICE_NAME = "SERVICE_NAME";
        const string USERID = "USERID";
        const string PASSWORD = "PASSWORD";

        // We have to use the TNS syntax here, otherwise we will get an ORA-12154 error (could not resolve TNS)
        // The older Oracle.DataAccess library doesn't seem to be affected by this problem
        static string ConnectionString => $"DATA SOURCE=(DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = {HOST})(PORT = {PORT})) (CONNECT_DATA = (SERVER = DEDICATED) (SERVICE_NAME = {SERVICE_NAME}) ) ); USER ID={USERID}; PASSWORD={PASSWORD};";
        static string DestinationFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db.sqlite");
        static string m_dbConnectionString => $"Data Source={DestinationFile};Version=3;"; 
        #endregion

        static void Main(string[] args)
        {
            Mirror();

            Console.WriteLine("Press any key to close (just avoid the power button)...");
            Console.ReadLine();
        }

        public static void Mirror()
        {
            var tables = new string[] { };
            var ignoreColumns = new string[] { };

            // Step 1: Create the .sqlite file
            SQLiteConnection.CreateFile(DestinationFile);

            // Step 2: Gather data from Oracle DB
            var ds = new DataSet();
            using (var dal = new OracleDal(ConnectionString))
                ds = dal.GetDataSet(tables);

            // Step 3: Create the tables
            CreateTables(ds, ignoreColumns);

            // Step 4: Launch the scripts in the db
            foreach (DataTable table in ds.Tables)
            {
                foreach (var colName in ignoreColumns)
                    if (table.Columns.Contains(colName))
                        table.Columns.Remove(colName);

                var sqliteDt = GetDataTable(table.TableName);

                foreach (DataRow row in table.Rows)
                    sqliteDt.Rows.Add(row.ItemArray);

                SaveDataTable(sqliteDt);
            }

            DisposeSQLite();

            Console.WriteLine($"Imported {ds.Tables.Count} {(ds.Tables.Count == 1 ? "table" : "tables")}. You can find the sqlite file here:");
            Console.WriteLine($"{DestinationFile}");
            Console.WriteLine();
        }

        #region Helper functions
        private static DataTable GetDataTable(string table)
        {
            DataTable dt = new DataTable();

            using (var m_dbConnection = new SQLiteConnection(m_dbConnectionString))
            {
                m_dbConnection.Open();

                using (SQLiteCommand m_dbCommand = new SQLiteCommand(m_dbConnection))
                {
                    m_dbCommand.CommandText = $"select * from {table}";

                    using (var adapter = new SQLiteDataAdapter(m_dbCommand))
                    {
                        adapter.Fill(dt);
                    }
                }
            }

            dt.TableName = table;
            return dt;
        }

        private static void ExecuteQuery(string query)
        {
            using (var m_dbConnection = new SQLiteConnection(m_dbConnectionString))
            {
                m_dbConnection.Open();

                using (SQLiteCommand m_dbCommand = new SQLiteCommand(m_dbConnection))
                {
                    m_dbCommand.CommandText = query;
                    m_dbCommand.ExecuteNonQuery();
                }
            }
        }

        private static void SaveDataTable(DataTable dt)
        {
            ExecuteQuery($"delete from {dt.TableName}");

            using (var m_dbConnection = new SQLiteConnection(m_dbConnectionString))
            {
                m_dbConnection.Open();

                var transaction = m_dbConnection.BeginTransaction();

                using (SQLiteCommand m_dbCommand = new SQLiteCommand(m_dbConnection))
                {
                    m_dbCommand.CommandText = $"select * from {dt.TableName}";

                    using (var adapter = new SQLiteDataAdapter(m_dbCommand))
                    {
                        var builder = new SQLiteCommandBuilder(adapter);

                        try
                        {
                            adapter.Update(dt);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }
                }

                m_dbConnection.Close();
            }
        }

        private static void CreateTables(DataSet data, string[] ignoredColums = null)
        {
            #region Step 1: Create the command
            var tableCommands = new List<string>();

            foreach (DataTable table in data.Tables)
            {
                string tableCommand = $"CREATE TABLE {table.TableName} (";

                var columns = new List<string>();
                foreach (DataColumn column in table.Columns)
                {
                    if (ignoredColums?.Contains(column.ColumnName) ?? false)
                        continue;

                    var columnCommand = $"\n    {column.ColumnName} ";

                    switch (column.DataType.ToString())
                    {
                        case "System.Int32":
                            columnCommand += "int ";
                            break;
                        case "System.Int64":
                            columnCommand += "bigint ";
                            break;
                        case "System.Int16":
                            columnCommand += "smallint";
                            break;
                        case "System.Byte":
                            columnCommand += "tinyint";
                            break;
                        case "System.Decimal":
                            columnCommand += "decimal ";
                            break;
                        case "System.DateTime":
                            columnCommand += "datetime ";
                            break;
                        case "System.String":
                        default:
                            columnCommand += $"nvarchar({ (column.MaxLength == -1 ? "max" : column.MaxLength.ToString()) }) ";
                            break;
                    }

                    if (column.AutoIncrement)
                        columnCommand += $"IDENTITY({column.AutoIncrementSeed},{column.AutoIncrementStep}) ";
                    if (!column.AllowDBNull)
                        columnCommand += $"NOT NULL ";

                    columns.Add(columnCommand);
                }

                var columnsCommand = string.Join(",", columns);

                tableCommand += columnsCommand;
                tableCommand += $"\n)";

                tableCommands.Add(tableCommand);
            }

            string createTablesCommand = string.Join(";\n", tableCommands);
            #endregion

            #region Step 2: Execute it
            using (var m_dbConnection = new SQLiteConnection(m_dbConnectionString))
            {
                m_dbConnection.Open();

                using (var command = new SQLiteCommand(m_dbConnection))
                {
                    command.CommandText = createTablesCommand;
                    command.ExecuteNonQuery();
                }
            }
            #endregion
        }

        private static void DisposeSQLite()
        {
            SQLiteConnection.ClearAllPools();
            GC.Collect();
        } 
        #endregion
    }
}
