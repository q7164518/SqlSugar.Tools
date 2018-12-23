using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlSugar.Tools.SQLHelper
{
    internal class MySQLHelper
    {
        private static MySqlConnection con;

        private static MySqlConnection NewConnectionMethod(string conStr)
        {
            return con = new MySqlConnection(conStr);
        }

        /// <summary>
        /// 查询方法  返回DataTable
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryDataTable(string conStr, string sql, List<MySqlParameter> list = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (MySQLHelper.con = MySQLHelper.NewConnectionMethod(conStr))
                {
                    MySqlDataAdapter sda = new MySqlDataAdapter(sql, MySQLHelper.con);
                    if (list != null)
                    {
                        foreach (MySqlParameter item in list)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        MySQLHelper.con.Close();
                        MySQLHelper.con.Dispose();
                    }
                    catch (Exception e)
                    {
                        MySQLHelper.con.Close();
                        MySQLHelper.con.Dispose();
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
                MySQLHelper.con = MySQLHelper.NewConnectionMethod(conStr);
                MySqlCommand cmd = new MySqlCommand(sql, MySQLHelper.con);
                try
                {
                    MySQLHelper.con.Open();
                    MySqlDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                    dt = sdr.GetSchemaTable();  //获得表的结构
                    sdr.Close();
                    MySQLHelper.con.Close();
                    MySQLHelper.con.Dispose();
                }
                catch (Exception e)
                {
                    MySQLHelper.con.Close();
                    MySQLHelper.con.Dispose();
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
                MySQLHelper.con = MySQLHelper.NewConnectionMethod(conStr);
                try
                {
                    MySQLHelper.con.Open();
                    if (MySQLHelper.con.State == ConnectionState.Open)
                    {
                        MySQLHelper.con.Close();
                        MySQLHelper.con.Dispose();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    MySQLHelper.con.Close();
                    MySQLHelper.con.Dispose();
                    throw ex;
                }
            });
        }
    }
}
