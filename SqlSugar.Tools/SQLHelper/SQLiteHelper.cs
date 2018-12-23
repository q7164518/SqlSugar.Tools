using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace SqlSugar.Tools.SQLHelper
{
    internal class SQLiteHelper
    {
        private static SQLiteConnection con;

        private static SQLiteConnection NewConnectionMethod(string conStr)
        {
            return con = new SQLiteConnection(conStr);
        }

        /// <summary>
        /// 查询方法  返回DataTable
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryDataTable(string conStr, string sql, List<SQLiteParameter> list = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (SQLiteHelper.con = SQLiteHelper.NewConnectionMethod(conStr))
                {
                    SQLiteDataAdapter sda = new SQLiteDataAdapter(sql, SQLiteHelper.con);
                    if (list != null)
                    {
                        foreach (SQLiteParameter item in list)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        SQLiteHelper.con.Close();
                        SQLiteHelper.con.Dispose();
                    }
                    catch (Exception e)
                    {
                        SQLiteHelper.con.Close();
                        SQLiteHelper.con.Dispose();
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
                SQLiteHelper.con = SQLiteHelper.NewConnectionMethod(conStr);
                SQLiteCommand cmd = new SQLiteCommand(sql, SQLiteHelper.con);
                try
                {
                    SQLiteHelper.con.Open();
                    SQLiteDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                    dt = sdr.GetSchemaTable();  //获得表的结构
                    sdr.Close();
                    SQLiteHelper.con.Close();
                    SQLiteHelper.con.Dispose();
                }
                catch (Exception e)
                {
                    SQLiteHelper.con.Close();
                    SQLiteHelper.con.Dispose();
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
                SQLiteHelper.con = SQLiteHelper.NewConnectionMethod(conStr);
                try
                {
                    SQLiteHelper.con.Open();
                    if (SQLiteHelper.con.State == ConnectionState.Open)
                    {
                        SQLiteHelper.con.Close();
                        SQLiteHelper.con.Dispose();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    SQLiteHelper.con.Close();
                    SQLiteHelper.con.Dispose();
                    throw ex;
                }
            });
        }
    }
}