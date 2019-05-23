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
            DataTable dt;
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (MySqlCommand cmd = new MySqlCommand($"select * from {tableName} where 1 = 2", this._Con))
            {
                try
                {
                    this._Con.Open();
                    using (DbDataReader sdr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo))
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
            this.NewConnectionMethod(connectionString);
            using (this._Con)
            using (MySqlCommand cmd = new MySqlCommand(sqlString, this._Con))
            {
                this._Con.Open();
                return await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}