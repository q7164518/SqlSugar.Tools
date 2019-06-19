using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlSugar.Tools.DBMoveTools.DBHelper
{
    internal class PostgreSQLDBHelper : IDBHelper
    {
        private NpgsqlConnection _Con;

        public IDbConnection NewConnectionMethod(string connectionString)
        {
            this._Con = new NpgsqlConnection(connectionString);
            return this._Con;
        }

        public async Task<int> CreateTable(string connectionString, string sqlString)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (NpgsqlCommand cmd = new NpgsqlCommand(sqlString, this._Con))
            {
                await this._Con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<int> Insert(string connectionString, string insertSql, List<IDataParameter> @params)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (NpgsqlCommand cmd = new NpgsqlCommand(insertSql, this._Con))
            {
                if (@params?.Count > 0)
                {
                    foreach (var item in @params)
                    {
                        cmd.Parameters.Add(item as NpgsqlParameter);
                    }
                }
                await this._Con.OpenAsync();
                var result = await cmd.ExecuteNonQueryAsync();
                cmd.Parameters.Clear();
                return result;
            }
        }

        public async Task<IDataReader> QueryDataReader(string connectionString, string querySql)
        {
            this.NewConnectionMethod(connectionString);
            using (var cmd = new NpgsqlCommand(querySql, this._Con))
            {
                await this._Con.OpenAsync();
                return await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            }
        }

        public async Task<DataTable> QueryDataTable(string connectionString, string sqlString, List<IDbDataParameter> parameters = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                this.NewConnectionMethod(connectionString);
                using (this._Con)
                using (NpgsqlDataAdapter sda = new NpgsqlDataAdapter(sqlString, this._Con))
                {
                    if (parameters != null)
                    {
                        foreach (NpgsqlParameter item in parameters)
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
            using (NpgsqlCommand cmd = new NpgsqlCommand($"SELECT * FROM \"{tableName}\" WHERE 1 = 2", this._Con))
            {
                try
                {
                    await this._Con.OpenAsync();
                    using (var sdr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo))
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

        public async Task<bool> TableAny(string connectionString, string tableName)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (NpgsqlCommand cmd = new NpgsqlCommand($"SELECT COUNT(1) FROM pg_tables WHERE tablename = '{tableName}' AND schemaname = 'public'", this._Con))
            {
                try
                {
                    await this._Con.OpenAsync();
                    return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                }
                catch (Exception e)
                {
                    throw e;
                }
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
                    return false;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public async Task<long> QueryMaxID(string connectionString, string tableName, string colName)
        {
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (NpgsqlCommand cmd = new NpgsqlCommand($"SELECT MAX(\"{colName}\") FROM \"public\".\"{tableName}\"", this._Con))
            {
                try
                {
                    await this._Con.OpenAsync();
                    return Convert.ToInt64(await cmd.ExecuteScalarAsync());
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
        }
    }
}