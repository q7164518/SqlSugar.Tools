using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlSugar.Tools.DBMoveTools.DBHelper
{
    internal class SqlServerDBHelper : IDBHelper
    {
        private SqlConnection _Con;

        public IDbConnection NewConnectionMethod(string connectionString)
        {
            this._Con = new SqlConnection(connectionString);
            return this._Con;
        }

        public async Task<DataTable> QueryDataTable(string connectionString, string sqlString, List<IDbDataParameter> parameters = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (SqlDataAdapter sda = new SqlDataAdapter(sqlString, this._Con))
                {
                    if (parameters != null)
                    {
                        foreach (SqlParameter item in parameters)
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
            DataTable dt;
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (SqlCommand cmd = new SqlCommand($"select * from {tableName} where 1 = 2", this._Con))
            {
                try
                {
                    await this._Con.OpenAsync();
                    using (SqlDataReader sdr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo))
                    {
                        dt = sdr.GetSchemaTable();  //获得表的结构
                        sdr.Close();
                    }
                }
                catch (Exception e)
                {
                    throw e;
                }
                return dt;
            }
        }

        public async Task<bool> TestLink(string connectionString)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            {
                try
                {
                    await this._Con.OpenAsync();
                    if (this._Con.State == ConnectionState.Open)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public async Task<int> CreateTable(string connectionString, string sqlString)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (SqlCommand cmd = new SqlCommand(sqlString, this._Con))
            {
                await this._Con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<bool> TableAny(string connectionString, string tableName)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (SqlCommand cmd = new SqlCommand($"select count(1) from sysobjects where id = object_id('[{tableName}]')", this._Con))
            {
                await this._Con.OpenAsync();
                return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
            }
        }

        public async Task<IDataReader> QueryDataReader(string connectionString, string querySql)
        {
            this.NewConnectionMethod(connectionString);
            using (SqlCommand cmd = new SqlCommand(querySql, this._Con))
            {
                await this._Con.OpenAsync();
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
        }

        public async Task<int> Insert(string connectionString, string insertSql, List<IDataParameter> @params)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (SqlCommand cmd = new SqlCommand(insertSql, this._Con))
            {
                if (@params?.Count > 0)
                {
                    foreach (var item in @params)
                    {
                        cmd.Parameters.Add(item as SqlParameter);
                    }
                }
                await this._Con.OpenAsync();
                var result = await cmd.ExecuteNonQueryAsync();
                cmd.Parameters.Clear();
                return result;
            }
        }

        public Task<long> QueryMaxID(string connectionString, string tableName, string colName)
        {
            throw new NotImplementedException();
        }
    }
}