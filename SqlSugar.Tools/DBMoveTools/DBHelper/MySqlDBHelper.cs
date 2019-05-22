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
                using (this.NewConnectionMethod(connectionString))
                {
                    MySqlDataAdapter sda = new MySqlDataAdapter(sqlString, this._Con);
                    if (parameters != null)
                    {
                        foreach (MySqlParameter item in parameters)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        this._Con.Close();
                        this._Con.Dispose();
                    }
                    catch (Exception e)
                    {
                        this._Con.Close();
                        this._Con.Dispose();
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
            MySqlCommand cmd = new MySqlCommand($"select * from {tableName} where 1 = 2", this._Con);
            try
            {
                this._Con.Open();
                DbDataReader sdr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
                dt = sdr.GetSchemaTable();  //获得表的结构
                sdr.Close();
                this._Con.Close();
                this._Con.Dispose();
            }
            catch (Exception e)
            {
                this._Con.Close();
                this._Con.Dispose();
                throw e;
            }
            return dt;
        }

        public async Task<bool> TestLink(string connectionString)
        {
            this.NewConnectionMethod(connectionString);
            try
            {
                await this._Con.OpenAsync();
                if (this._Con.State == ConnectionState.Open)
                {
                    this._Con.Close();
                    this._Con.Dispose();
                    return true;
                }
                else
                {
                    this._Con.Close();
                    this._Con.Dispose();
                    return false;
                }
            }
            catch (Exception ex)
            {
                this._Con.Close();
                this._Con.Dispose();
                throw ex;
            }
        }
    }
}