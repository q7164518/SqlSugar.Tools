using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlSugar.Tools.SQLHelper
{
    /// <summary>
    /// SQL Server
    /// </summary>
    internal class SQLServerHelper
    {
        private static SqlConnection con;

        private static SqlConnection NewConnectionMethod(string conStr)
        {
            return con = new SqlConnection(conStr);
        }

        /// <summary>
        /// 查询方法  返回DataTable
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryDataTable(string conStr, string sql, List<SqlParameter> list = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (SQLServerHelper.con = SQLServerHelper.NewConnectionMethod(conStr))
                {
                    SqlDataAdapter sda = new SqlDataAdapter(sql, SQLServerHelper.con);
                    if (list != null)
                    {
                        foreach (SqlParameter item in list)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        SQLServerHelper.con.Close();
                        SQLServerHelper.con.Dispose();
                    }
                    catch (Exception e)
                    {
                        SQLServerHelper.con.Close();
                        SQLServerHelper.con.Dispose();
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
                SQLServerHelper.con = SQLServerHelper.NewConnectionMethod(conStr);
                SqlCommand cmd = new SqlCommand(sql, SQLServerHelper.con);
                try
                {
                    SQLServerHelper.con.Open();
                    SqlDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                    dt = sdr.GetSchemaTable();  //获得表的结构
                    sdr.Close();
                    SQLServerHelper.con.Close();
                    SQLServerHelper.con.Dispose();
                }
                catch (Exception e)
                {
                    SQLServerHelper.con.Close();
                    SQLServerHelper.con.Dispose();
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
                SQLServerHelper.con = SQLServerHelper.NewConnectionMethod(conStr);
                try
                {
                    SQLServerHelper.con.Open();
                    if (SQLServerHelper.con.State == ConnectionState.Open)
                    {
                        SQLServerHelper.con.Close();
                        SQLServerHelper.con.Dispose();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    SQLServerHelper.con.Close();
                    SQLServerHelper.con.Dispose();
                    throw ex;
                }
            });
        }
    }
}
