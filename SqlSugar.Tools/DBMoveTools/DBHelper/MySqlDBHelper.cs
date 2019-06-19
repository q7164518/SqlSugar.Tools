using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace SqlSugar.Tools.DBMoveTools.DBHelper
{
    internal class MySqlDBHelper : IDBHelper
    {
        private MySqlConnection _Con;

        public IDbConnection NewConnectionMethod(string connectionString)
        {
            this._Con = new MySqlConnection(connectionString);
            return this._Con;
        }

        public async Task<DataTable> QueryDataTable(string connectionString, string sqlString, List<IDbDataParameter> parameters = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlDataAdapter sda = new MySqlDataAdapter(sqlString, this._Con))
                {
                    if (parameters != null)
                    {
                        foreach (MySqlParameter item in parameters)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    return dt;
                }
            });
        }

        public async Task<DataTable> QueryTableInfo(string connectionString, string tableName)
        {
            return await Task.Run(() =>
            {
                DataTable dt;
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlCommand cmd = new MySqlCommand($"select * from {tableName} where 1 = 2", this._Con))
                {
                    try
                    {
                        this._Con.Open();
                        using (DbDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                        {
                            dt = sdr.GetSchemaTable();  //获得表的结构
                            sdr.Close();
                        }
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                    //return dt;
                }
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlDataAdapter sda = new MySqlDataAdapter($"SHOW FULL COLUMNS FROM `{tableName}`", this._Con))
                {
                    DataTable dt1 = new DataTable();
                    try
                    {
                        sda.Fill(dt1);
                        sda.Dispose();
                        dt.Columns.Add("DataTypeName", typeof(string));
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            var colName = dt.Rows[i]["ColumnName"].ToString();
                            foreach (DataRow item in dt1.Rows)
                            {
                                if (item["Field"].ToString() == colName)
                                {
                                    var type = item["Type"].ToString();
                                    var index = type.IndexOf('(');
                                    if (index >= 0)
                                    {
                                        type = type.Substring(0, index);
                                    }
                                    dt.Rows[i]["DataTypeName"] = type;
                                }
                            }
                        }
                        dt1.Clear();
                        dt1.Dispose();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }
                }
                return dt;
            });
        }

        public async Task<bool> TestLink(string connectionString)
        {
            return await Task.Run(() =>
            {
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                {
                    try
                    {

                        this._Con.Open();
                        if (this._Con.State == ConnectionState.Open)
                        {
                            return true;
                        }
                        return false;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
            });
        }

        public async Task<int> CreateTable(string connectionString, string sqlString)
        {
            return await Task.Run(() =>
            {
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlCommand cmd = new MySqlCommand(sqlString, this._Con))
                {
                    this._Con.Open();
                    return cmd.ExecuteNonQuery();
                }
            });
        }

        public async Task<bool> TableAny(string connectionString, string tableName)
        {
            return await Task.Run(() =>
            {
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlCommand cmd = new MySqlCommand($"SHOW TABLES LIKE '{tableName}'", this._Con))
                {
                    this._Con.Open();
                    var table = (cmd.ExecuteScalar())?.ToString()?.ToLower();
                    return table == tableName.ToLower();
                }
            });
        }

        public async Task<IDataReader> QueryDataReader(string connectionString, string querySql)
        {
            return await Task.Run(() =>
            {
                this.NewConnectionMethod(connectionString);
                MySqlCommand cmd = new MySqlCommand(querySql, this._Con);
                this._Con.Open();
                return cmd.ExecuteReader(CommandBehavior.CloseConnection);
            });
        }

        public async Task<int> Insert(string connectionString, string insertSql, List<IDataParameter> @params)
        {
            return await Task.Run(() =>
            {
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (MySqlCommand cmd = new MySqlCommand(insertSql, this._Con))
                {
                    if (@params?.Count > 0)
                    {
                        foreach (var item in @params)
                        {
                            cmd.Parameters.Add(item as MySqlParameter);
                        }
                    }
                    this._Con.Open();
                    var result = cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                    return result;
                }
            });
        }

        public Task<long> QueryMaxID(string connectionString, string tableName, string colName)
        {
            throw new NotImplementedException();
        }
    }
}