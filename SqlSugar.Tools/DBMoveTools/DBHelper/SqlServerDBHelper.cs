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
                using (this.NewConnectionMethod(connectionString))
                {
                    SqlDataAdapter sda = new SqlDataAdapter(sqlString, this._Con);
                    if (parameters != null)
                    {
                        foreach (SqlParameter item in parameters)
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
            SqlCommand cmd = new SqlCommand($"select * from {tableName} where 1 = 2", this._Con);
            try
            {
                this._Con.Open();
                SqlDataReader sdr = await cmd.ExecuteReaderAsync(CommandBehavior.KeyInfo);
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