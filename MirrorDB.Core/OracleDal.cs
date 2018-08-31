using Oracle.ManagedDataAccess.Client;
using System;
using System.Data;

namespace MirrorDB.Core
{
    internal class OracleDal : IDisposable
    {
        IDbConnection _connection;
        IDbCommand _command;

        public OracleDal(string connectionString)
        {
            _connection = new OracleConnection(connectionString);
            if (_connection.State != ConnectionState.Open)
                _connection.Open();
        }

        internal DataSet GetDataSet(string[] tables)
        {
            var ds = new DataSet();

            using (_command = new OracleCommand())
            {
                _command.Connection = _connection;

                foreach (var t in tables)
                {
                    _command.CommandText = $"select * from {t} ";
                    var table = new DataTable(t);

                    using (var dr = _command.ExecuteReader())
                        table.Load(dr);

                    ds.Tables.Add(table);
                }
            }

            return ds;
        }

        public void Dispose()
        {
            if (!(_connection.State == ConnectionState.Closed))
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }
    }
}