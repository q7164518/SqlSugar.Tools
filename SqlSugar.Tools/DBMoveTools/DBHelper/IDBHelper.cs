using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlSugar.Tools.DBMoveTools.DBHelper
{
    /// <summary>
    /// 提供数据库操作基本方法定义
    /// </summary>
    internal interface IDBHelper
    {
        /// <summary>
        /// 创建一个数据库连接
        /// </summary>
        /// <param name="connectionString">数据库连接字符串</param>
        /// <returns></returns>
        IDbConnection NewConnectionMethod(string connectionString);

        /// <summary>
        /// 查询方法  返回DataTable 异步的
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="sqlString"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        Task<DataTable> QueryDataTable(string connectionString, string sqlString, List<IDbDataParameter> parameters = null);

        /// <summary>
        /// 获得Table信息
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <param name="tableName">数据库表名</param>
        /// <returns></returns>
        Task<DataTable> QueryTableInfo(string connectionString, string tableName);

        /// <summary>
        /// 测试连接
        /// </summary>
        /// <param name="connectionString">连接字符串</param>
        /// <returns></returns>
        Task<bool> TestLink(string connectionString);
    }
}