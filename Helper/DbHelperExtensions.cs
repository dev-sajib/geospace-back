using System.Data;
using Microsoft.Data.SqlClient;

namespace geospace_back.Helper
{
    public static class DbHelperExtensions
    {
        private static SQLDbHelper _dbHelper;

        public static void Initialize(SQLDbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public static DataSet ExecuteQuery(string sqlText, bool procedure, Dictionary<string, SqlParameter> parameters)
        {
            return _dbHelper.ExecuteQuery(sqlText, procedure, parameters);
        }

        public static List<Dictionary<string, object>> ExecuteQueryAndConvertToList(string storedProcedure, Dictionary<string, SqlParameter> parameters)
        {
            var result = _dbHelper.ExecuteQuery(storedProcedure, true, parameters);

            if (result.Tables[0].Rows.Count > 0)
            {
                var dataTable = result.Tables[0];
                var list = new List<Dictionary<string, object>>();

                foreach (DataRow row in dataTable.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dataTable.Columns)
                    {
                        var value = row[col];
                        if (value == DBNull.Value)
                        {
                            dict[col.ColumnName] = "";
                        }
                        else if (value is DateTime dateTimeValue)
                        {
                            dict[col.ColumnName] = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                        }
                        else
                        {
                            dict[col.ColumnName] = value;
                        }
                    }
                    list.Add(dict);
                }

                return list;
            }
            else
            {
                return null;
            }
        }


        public static DataSet ExecuteQueryAndReturnDataset(string storedProcedure, Dictionary<string, SqlParameter> parameters)
        {
            return _dbHelper.ExecuteQuery(storedProcedure, true, parameters);
        }


        public static Dictionary<string, List<Dictionary<string, object>>> ExecuteQueryAndConvertToListAllTable(string storedProcedure, Dictionary<string, SqlParameter> parameters)
        {
            var result = _dbHelper.ExecuteQuery(storedProcedure, true, parameters);

            if (result.Tables.Count > 0)
            {
                var allTablesDict = new Dictionary<string, List<Dictionary<string, object>>>();

                for (int i = 0; i < result.Tables.Count; i++)
                {
                    var dataTable = result.Tables[i];
                    var tableList = new List<Dictionary<string, object>>();

                    foreach (DataRow row in dataTable.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in dataTable.Columns)
                        {
                            var value = row[col];
                            if (value == DBNull.Value)
                            {
                                dict[col.ColumnName] = "";
                            }
                            else if (value is DateTime dateTimeValue)
                            {
                                dict[col.ColumnName] = dateTimeValue.ToString("yyyy-MM-dd HH:mm:ss");
                            }
                            else
                            {
                                dict[col.ColumnName] = value;
                            }
                        }
                        tableList.Add(dict);
                    }

                    allTablesDict.Add($"Table{i}", tableList);
                }

                return allTablesDict;
            }
            else
            {
                return null;
            }
        }
         
        public static string ExecuteCommandWithOutput(string sqlText, bool procedure, Dictionary<string, SqlParameter> procParameters)
        {
            using (var connection = new SqlConnection(_dbHelper.Connection.ConnectionString))
            {
                connection.Open();
                using (var cmd = new SqlCommand(sqlText, connection))
                {
                    cmd.CommandType = procedure ? CommandType.StoredProcedure : CommandType.Text;

                    foreach (var procParameter in procParameters)
                    {
                        cmd.Parameters.Add(procParameter.Value);
                    }
                    cmd.Parameters.Add("@result", SqlDbType.VarChar, 500).Direction = ParameterDirection.Output;

                    cmd.ExecuteNonQuery();
                    connection.Close();
                    return cmd.Parameters["@result"].Value.ToString();
                }
            }
        }

        public static string ExecuteProcedureAndReturnJson(string procedureName, Dictionary<string, SqlParameter> parameters)
        {
            using (var connection = new SqlConnection(_dbHelper.Connection.ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.Add(param.Value);
                        }
                    }

                    var jsonResult = new System.Text.StringBuilder();
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            return "[]";
                        }
                        while (reader.Read())
                        {
                            jsonResult.Append(reader.GetValue(0).ToString());
                        }
                    }
                    return jsonResult.ToString();
                }
            }
        }
    }
}