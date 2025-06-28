using System.Data;
using Microsoft.Data.SqlClient;

namespace geospace_back.Helper
{
    public class SQLDbHelper : IDisposable
    {
        private readonly SqlConnection _connection;
        private readonly string _connectionString;

        public SQLDbHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException(nameof(configuration), "Connection string cannot be null");
            _connection = new SqlConnection(_connectionString);
        }

        public SqlConnection Connection => _connection;

        public void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
            {
                _connection.Close();
            }
        }

        public DataSet ExecuteQuery(string sqlText, bool procedure, Dictionary<string, SqlParameter> procParameters)
        {
            if (string.IsNullOrEmpty(sqlText))
            {
                throw new ArgumentException("SQL text cannot be null or empty", nameof(sqlText));
            }

            var ds = new DataSet();

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var command = new SqlCommand(sqlText, conn))
                {
                    command.CommandType = procedure ? CommandType.StoredProcedure : CommandType.Text;

                    if (procParameters != null)
                    {
                        foreach (var procParameter in procParameters)
                        {
                            command.Parameters.Add(procParameter.Value);
                        }
                    }

                    var da = new SqlDataAdapter(command);
                    da.Fill(ds);
                }
                conn.Close();
            }

            return ds;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}