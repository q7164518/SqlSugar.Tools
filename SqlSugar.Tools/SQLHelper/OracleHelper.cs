using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlSugar.Tools.SQLHelper
{
    internal class OracleHelper
    {
        private static OracleConnection con;

        private static OracleConnection NewConnectionMethod(string conStr)
        {
            return con = new OracleConnection(conStr);
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
                OracleHelper.con = OracleHelper.NewConnectionMethod(conStr);
                try
                {
                    OracleHelper.con.Open();
                    if (OracleHelper.con.State == ConnectionState.Open)
                    {
                        OracleHelper.con.Close();
                        OracleHelper.con.Dispose();
                        return true;
                    }
                    return false;
                }
                catch (Exception ex)
                {
                    OracleHelper.con.Close();
                    OracleHelper.con.Dispose();
                    throw ex;
                }
            });
        }

        /// <summary>
        /// 查询方法  返回DataTable
        /// </summary>
        /// <param name="conStr"></param>
        /// <param name="sql"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        internal async static Task<DataTable> QueryDataTable(string conStr, string sql, List<OracleParameter> list = null)
        {
            return await Task.Run(() =>
            {
                DataTable dt = new DataTable();
                using (OracleHelper.con = OracleHelper.NewConnectionMethod(conStr))
                {
                    OracleDataAdapter sda = new OracleDataAdapter(sql, OracleHelper.con);
                    if (list != null)
                    {
                        foreach (OracleParameter item in list)
                            sda.SelectCommand.Parameters.Add(item);
                    }
                    try
                    {
                        sda.Fill(dt);
                        sda.Dispose();
                        OracleHelper.con.Close();
                        OracleHelper.con.Dispose();
                    }
                    catch (Exception e)
                    {
                        OracleHelper.con.Close();
                        OracleHelper.con.Dispose();
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
                OracleHelper.con = OracleHelper.NewConnectionMethod(conStr);
                OracleCommand cmd = new OracleCommand(sql, OracleHelper.con);
                try
                {
                    OracleHelper.con.Open();
                    OracleDataReader sdr = cmd.ExecuteReader(CommandBehavior.KeyInfo);
                    dt = sdr.GetSchemaTable();  //获得表的结构
                    sdr.Close();
                    OracleHelper.con.Close();
                    OracleHelper.con.Dispose();
                }
                catch (Exception e)
                {
                    OracleHelper.con.Close();
                    OracleHelper.con.Dispose();
                    throw e;
                }
                return dt;
            });
        }
    }
}
