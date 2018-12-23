using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlSugar.Tools.SQLHelper
{
    internal class PostgreSqlHelper
    {
        private static NpgsqlConnection con;

        private static NpgsqlConnection NewConnectionMethod(string conStr)
        {
            return con = new NpgsqlConnection(conStr);
        }

        /// <summary>
        /// 查询方法  返回DataTable
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryDataTable(string conStr, string sql, List<NpgsqlParameter> list = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (PostgreSqlHelper.con = PostgreSqlHelper.NewConnectionMethod(conStr))
                {
                    var sda = new NpgsqlDataAdapter(sql, PostgreSqlHelper.con);
                    if (list != null)
                    {
                        foreach (NpgsqlParameter item in list)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        PostgreSqlHelper.con.Close();
                        PostgreSqlHelper.con.Dispose();
                    }
                    catch (Exception e)
                    {
                        PostgreSqlHelper.con.Close();
                        PostgreSqlHelper.con.Dispose();
                        throw e;
                    }
                    return dt;
                }
            });
        }

        /// <summary>
        /// 查询表信息
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryTableInfo(string conStr, string sql)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                PostgreSqlHelper.con = PostgreSqlHelper.NewConnectionMethod(conStr);
                var cmd = new NpgsqlCommand(sql, PostgreSqlHelper.con);
                try
                {
                    PostgreSqlHelper.con.Open();
                    NpgsqlDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                    dt = sdr.GetSchemaTable();  //获得表的结构
                    sdr.Close();
                    PostgreSqlHelper.con.Close();
                    PostgreSqlHelper.con.Dispose();
                }
                catch (Exception e)
                {
                    PostgreSqlHelper.con.Close();
                    PostgreSqlHelper.con.Dispose();
                    throw e;
                }
                return dt;
            });
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="conStr">连接字符串</param>
        /// <returns></returns>
        internal async static Task<bool> TestLink(string conStr)
        {
            return await Task.Run(() =>
            {
                PostgreSqlHelper.con = PostgreSqlHelper.NewConnectionMethod(conStr);
                try
                {
                    PostgreSqlHelper.con.Open();
                    if (PostgreSqlHelper.con.State == ConnectionState.Open)
                    {
                        PostgreSqlHelper.con.Close();
                        PostgreSqlHelper.con.Dispose();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    PostgreSqlHelper.con.Close();
                    PostgreSqlHelper.con.Dispose();
                    throw ex;
                }
            });
        }
    }
}
