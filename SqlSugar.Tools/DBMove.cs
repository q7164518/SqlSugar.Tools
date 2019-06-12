using Chromium.Event;
using MySql.Data.MySqlClient;
using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.DBMoveTools.DBHelper;
using SqlSugar.Tools.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    public partial class DBMove : Formium
    {
        public static DBMove _DBMove = null;

        public DBMove()
            : base("http://my.resource.local/pages/DBMove.html")
        {
            InitializeComponent();
            this.MinimumSize = new Size(1100, 690);
            this.StartPosition = FormStartPosition.CenterParent;
            base.GlobalObject.AddFunction("exit").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    this.Close();
                    GC.Collect();
                });
            };
            base.GlobalObject.AddFunction("showDBTypeSetting").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    new DBTypeSetting().ShowDialog();
                });
            };

            var sqlite = base.GlobalObject.AddObject("sqlite");
            var selectDBFile = sqlite.AddFunction("selectDBFile");  //选择db文件方法
            selectDBFile.Execute += (func, args) =>
            {
                using (var openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Title = "请选择SQLite文件",
                    Filter = "SQLite文件(*.db)|*.db|所有文件(*.*)|*.*"
                })
                {
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        //string file = openFileDialog.FileName;//返回文件的完整路径
                        EvaluateJavascript($"setSQLiteFilePath('{openFileDialog.FileName.Replace("\\", "\\\\")}')", (value, exception) => { });
                    }
                }
            };

            this.RegiestFunc("SqlServer", "sqlServer");
            this.RegiestFunc("MySQL", "mysql");
            this.RegiestStartMove();
#if DEBUG
            base.LoadHandler.OnLoadEnd += (object sender, CfxOnLoadEndEventArgs e) =>
            {
                base.Chromium.ShowDevTools();
            };
#endif
        }

        public static void ShowWindow()
        {
            if (DBMove._DBMove == null)
            {
                DBMove._DBMove = new DBMove();
            }
            DBMove._DBMove.Show();
            DBMove._DBMove.Focus();
        }

        private void DBMove_FormClosed(object sender, FormClosedEventArgs e)
        {
            DBMove._DBMove.Dispose();
            this.Dispose();
            DBMove._DBMove = null;
            GC.Collect();
        }

        /// <summary>
        /// 注册C#方法到JS
        /// </summary>
        /// <param name="dbName">数据库名称</param>
        /// <param name="jsObjName">注册到JS的对象名称</param>
        private void RegiestFunc(string dbName, string jsObjName)
        {
            var objName = base.GlobalObject.AddObject(jsObjName);
            var testLink = objName.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var dBHelper = this.CreateDBHelper(dbName);
                        if (await dBHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                            string sqlString = string.Empty;
                            if (dbName == "SqlServer")
                            {
                                sqlString = "select name from sysdatabases where dbid>4";
                            }
                            else if (dbName == "MySQL")
                            {
                                sqlString = "SELECT `SCHEMA_NAME` as name  FROM `information_schema`.`SCHEMATA` order by `SCHEMA_NAME`";
                            }
                            var dbList = await dBHelper.QueryDataTable(linkString, sqlString);
                            var dbListJson = JsonConvert.SerializeObject(dbList);
                            dbList.Clear(); dbList.Dispose(); dbList = null;
                            EvaluateJavascript($"setDbList('{dbListJson}')", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", $"测试连接{dbName}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, $"测试连接{dbName}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", $"测试连接{dbName}", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = objName.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                var isYuan = args.Arguments.FirstOrDefault(p => p.IsBool).BoolValue;
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, this.GetDataBaseType(dbName), this.CreateDBHelper(dbName));
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        var propName = isYuan ? "yuanTableData" : "mubiaoTableData";
                        EvaluateJavascript($"setTables('{tablesJson}', '{propName}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "加载表", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        /// <summary>
        /// 注册开始迁移方法到JS
        /// </summary>
        private void RegiestStartMove()
        {
            var objName = base.GlobalObject.AddObject("move");
            var startMove = objName.AddFunction("startMove");
            startMove.Execute += async (func, args) =>
            {
                var tablesJson = (args.Arguments[0].StringValue ?? string.Empty).Trim();    //选择要迁移的表JSON数组
                var yuanDBName = (args.Arguments[1].StringValue ?? string.Empty).Trim();    //源数据库名字
                var yuanDBType = this.GetDataBaseType(yuanDBName);
                var yuanConnectionString = (args.Arguments[2].StringValue ?? string.Empty).Trim();//源数据库连接字符串

                var mubiaoDBName = (args.Arguments[3].StringValue ?? string.Empty).Trim();  //目标数据库名字
                var mubiaoDBType = this.GetDataBaseType(mubiaoDBName);
                var mubiaoConnectionString = (args.Arguments[4].StringValue ?? string.Empty).Trim();//目标数据库连接字符串
                tablesJson = (tablesJson ?? string.Empty).Trim();

                var settingJson = (args.Arguments[5].StringValue ?? string.Empty).Trim();   //迁移设置JSON
                var mappingJson = (args.Arguments[6].StringValue ?? string.Empty).Trim();    //映射关系JSON
                if (string.IsNullOrEmpty(tablesJson))
                {
                    MessageBox.Show("请至少选择一个表进行迁移");
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
                try
                {
                    var yuanDBHelper = this.CreateDBHelper(yuanDBName);
                    var mubiaoDBHelper = this.CreateDBHelper(mubiaoDBName);
                    var tables = JsonConvert.DeserializeObject<RegiestStartMoveTablesModel[]>(tablesJson);
                    var mapping = JsonConvert.DeserializeObject<List<DBTypeMappingModel>>(mappingJson);
                    var setting = JsonConvert.DeserializeObject<SettingMove>(settingJson);
                    if (setting.OnlySql)
                    {
                        StringBuilder createTableSql = new StringBuilder();
                        foreach (var table in tables)
                        {
                            var dy = await yuanDBHelper.QueryTableInfo(yuanConnectionString, table.TableName);
                            var createTableSqlString = string.Empty;
                            if (yuanDBType == DataBaseType.SQLServer && mubiaoDBType == DataBaseType.MySQL)
                            {
                                createTableSqlString = await this.SqlServerTableToMySql(mubiaoDBHelper, mubiaoConnectionString, dy, table.TableName, setting.TableCover, mapping);
                            }
                            else if (yuanDBType == DataBaseType.MySQL && mubiaoDBType == DataBaseType.SQLServer)
                            {
                                createTableSqlString = await this.MySqlTableToSqlServer(mubiaoDBHelper, mubiaoConnectionString, dy, table.TableName, setting.TableCover, mapping);
                            }
                            createTableSql.Append(createTableSqlString).Append(Environment.NewLine).Append(Environment.NewLine);
                        }
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "sql",
                            Filter = "SQL(*.sql)|*.sql",
                            FileName = "迁移建表SQL.sql",
                            RestoreDirectory = true,
                            Title = "保存迁移建表SQL"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(createTableSql.ToString());
                                }
                                EvaluateJavascript("hideLoadingSuccess('生成建表SQL成功!')", (value, exception) => { });
                            }
                            else
                            {
                                EvaluateJavascript("hideLoading()", (value, exception) => { });
                            }
                        }
                    }
                    else
                    {
                        foreach (var table in tables)
                        {
                            var dy = await yuanDBHelper.QueryTableInfo(yuanConnectionString, table.TableName);
                            if (yuanDBType == DataBaseType.SQLServer && mubiaoDBType == DataBaseType.MySQL)
                                await this.SqlServerToMySql(yuanDBHelper, yuanConnectionString, mubiaoDBHelper, mubiaoConnectionString, dy, table.TableName, setting, mapping);
                            else if (yuanDBType == DataBaseType.MySQL && mubiaoDBType == DataBaseType.SQLServer)
                                await this.MySqlToSqlServer(yuanDBHelper, yuanConnectionString, mubiaoDBHelper, mubiaoConnectionString, dy, table.TableName, setting, mapping);
                        }
                        EvaluateJavascript("hideLoadingSuccess('迁移成功!')", (value, exception) => { });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"迁移表错误, Msg: {ex.Message}");
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                    return;
                }
            };
        }

        private IDBHelper CreateDBHelper(string dbName)
        {
            switch (dbName.ToLower())
            {
                case "sqlserver": return new SqlServerDBHelper();
                case "mysql": return new MySqlDBHelper();
                default: return null;
            }
        }

        private DataBaseType GetDataBaseType(string dbName)
        {
            switch (dbName.ToLower())
            {
                case "sqlserver": return DataBaseType.SQLServer;
                case "mysql": return DataBaseType.MySQL;
                default: return DataBaseType.SQLServer;
            }
        }

        /// <summary>
        /// 加载所有表
        /// </summary>
        /// <param name="linkString">连接字符串</param>
        private async Task<DataTable> LoadingTables(string linkString, DataBaseType type, IDBHelper dBHelper)
        {
            var sqlString = string.Empty;
            switch (type)
            {
                case DataBaseType.SQLServer:
                    sqlString = @"select name as TableName, ISNULL(j.TableDesc, '') as TableDesc  From sysobjects g
left join
(
select * from
(SELECT 
    TableName       = case when a.colorder=1 then d.name else '' end,
    TableDesc     = case when a.colorder=1 then isnull(f.value,'') else '' end
FROM 
    syscolumns a
inner join 
    sysobjects d 
on 
    a.id=d.id  and d.xtype='U' and  d.name<>'dtproperties'
inner join
sys.extended_properties f
on 
    d.id=f.major_id and f.minor_id=0) t
	where t.TableName!=''
	) j on g.name = j.TableName
	Where g.xtype='U'
	order by TableName ASC";
                    break;
                case DataBaseType.MySQL:
                    var database = linkString.Substring(linkString.IndexOf("Database=") + 9, linkString.IndexOf(";port=") - linkString.IndexOf("Database=") - 9);
                    sqlString = $"SELECT TABLE_NAME as TableName, Table_Comment as TableDesc FROM INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '{database}' order by TableName asc";
                    break;
                case DataBaseType.Oracler:
                    sqlString = "select table_name as TableName,comments as tabledesc from user_tab_comments order by table_name asc";
                    break;
                case DataBaseType.SQLite:
                    sqlString = "SELECT name FROM sqlite_master order by name asc";
                    break;
                case DataBaseType.PostgreSQL:
                    //var tableowner = linkString.Substring(linkString.IndexOf("Username=") + 9, linkString.IndexOf(";Password=") - linkString.IndexOf("Username=") - 9);
                    sqlString = $@"SELECT
	t2.tablename AS TableName,
	CAST (obj_description(relfilenode, 'pg_class') AS VARCHAR) AS TableDesc 
FROM
	pg_class t1
	LEFT JOIN pg_tables t2 ON t1.relname = t2.tablename 
WHERE
	t2.tableowner != 'postgres' 
ORDER BY
	t1.relname ASC";
                    break;
                default:
                    break;
            }
            return await dBHelper.QueryDataTable(linkString, sqlString);
        }

        /// <summary>
        /// SQL Server的表, 转Mysql建表SQL
        /// </summary>
        /// <param name="db">目标数据库操作对象</param>
        /// <param name="connectionString">目标数据库连接字符串</param>
        /// <param name="table">数据表信息</param>
        /// <param name="isCover">是否覆盖数据库表</param>
        /// <returns>MySQL建表SQL</returns>
        private async Task<string> SqlServerTableToMySql(IDBHelper db, string connectionString, DataTable table, string tableName, bool isCover, List<DBTypeMappingModel> mapping)
        {
            var keys = new List<string>();  //保存主键列集合
            var (sqlString, newTableName) = await this.MySqlCreateTableBefore(db, connectionString, tableName, isCover);
            var colsString = this.MSSQLTableConvert(table, mapping, DataBaseType.MySQL, (columnName, dataTypeName, mysqlType, isNull, isIdentity, isKey) =>
            {
                if (isKey) keys.Add(columnName);  //保存主键
                return $"  `{columnName}` {mysqlType} {(isNull ? "NULL" : "NOT NULL")}{(isIdentity ? " AUTO_INCREMENT" : "")},{Environment.NewLine}";
            });
            sqlString.Append(colsString);
            sqlString.Append("  PRIMARY KEY (");
            foreach (var item in keys)
            {
                sqlString.Append($"`{item}`,");
            }
            sqlString = sqlString.Remove(sqlString.Length - 1, 1);
            sqlString.Append($"){Environment.NewLine});");
            return sqlString.ToString();
        }

        /// <summary>
        /// 解析MSSQL的数据表信息
        /// </summary>
        /// <param name="table">MSSQL的表结构信息</param>
        /// <param name="mapping">数据库类型映射关系</param>
        /// <param name="dataBaseType">目标数据库类型</param>
        /// <param name="colFunc">每列的格式化函数, 有六个参数, 一个返回值
        /// <para>第一个string参数: columnName --> 列的名称</para>
        /// <para>第二个string参数: dataTypeName --> 源列数据类型, 如: varchar(30)</para>
        /// <para>第三个string参数: dbType --> 转换之后的列数据类型, 如: varchar(30)</para>
        /// <para>第四个bool参数: AllowDBNull --> 列是否可空</para>
        /// <para>第五个bool参数: IsIdentity --> 列是否是自增列(标识列)</para>
        /// <para>第六个bool参数: IsKey --> 列是否是主键</para>
        /// <para>返回值: string --> 格式化之后, 对应目标数据库的建表的列SQL</para>
        /// </param>
        /// <returns></returns>
        private string MSSQLTableConvert(in DataTable table, in List<DBTypeMappingModel> mapping, in DataBaseType dataBaseType, in Func<string, string, string, bool, bool, bool, string> colFunc)
        {
            StringBuilder result = new StringBuilder();
            foreach (DataRow item in table.Rows)
            {
                var columnName = item["ColumnName"].ToString();
                var dataTypeName = item["DataTypeName"].ToString();
                var mappingRow = mapping.FirstOrDefault(f => f.MSSQL.TrimEnd('(', ',', ')').ToLower().Trim() == dataTypeName.ToLower());
                if (mappingRow == null) throw new Exception($"找不到源数据库 [{dataTypeName}] 类型的映射设置");
                var dbType = mappingRow.GetMappingByType(dataBaseType);
                if (dbType.EndsWith("()")) //表示该类型有一个长度设置, 保持和源数据库长度一致
                {
                    dbType = dbType.Insert(dbType.Length - 1, item["ColumnSize"].ToString());
                }
                else if (dbType.EndsWith("(,)")) //表示该类型有两个长度设置, 如(10,2)
                {
                    dbType = dbType.Insert(dbType.Length - 2, item["NumericPrecision"].ToString());
                    dbType = dbType.Insert(dbType.Length - 1, item["NumericScale"].ToString());
                }
                result.Append(colFunc?.Invoke(columnName, dataTypeName, dbType, (bool)item["AllowDBNull"], (bool)item["IsIdentity"], (bool)item["IsKey"]));
            }
            return result.ToString();
        }

        /// <summary>
        /// MySql建表前缀
        /// </summary>
        /// <param name="db"></param>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="isCover"></param>
        /// <returns></returns>
        private async Task<(StringBuilder, string)> MySqlCreateTableBefore(IDBHelper db, string connectionString, string tableName, bool isCover)
        {
            var sqlString = new StringBuilder();
            var newTableName = tableName;
            if (isCover)    //覆盖表, 如果有
            {
                sqlString.Append($"DROP TABLE IF EXISTS `{tableName}`;{Environment.NewLine}CREATE TABLE `{tableName}` ({Environment.NewLine}");
            }
            else
            {
                var isAny = await db.TableAny(connectionString, tableName);
                if (isAny)
                {
                    newTableName = $"{tableName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                    sqlString.Append($"CREATE TABLE `{newTableName}` ({Environment.NewLine}");
                }
                else
                {
                    sqlString.Append($"CREATE TABLE `{tableName}` ({Environment.NewLine}");
                }
            }
            return (sqlString, newTableName);
        }

        /// <summary>
        /// MSSQL迁移到MySQL
        /// </summary>
        /// <param name="yuanDB">源数据库DB</param>
        /// <param name="yuanConnectionString">源数据库连接字符串</param>
        /// <param name="mubiaoDB">目标数据库DB</param>
        /// <param name="mubiaoConnectionString">目标数据库连接字符串</param>
        /// <param name="table">源数据表信息</param>
        /// <param name="tableName">表名</param>
        /// <param name="setting">设置</param>
        /// <param name="mapping">类型映射信息</param>
        /// <returns></returns>
        private async Task<bool> SqlServerToMySql(
            IDBHelper yuanDB,
            string yuanConnectionString,
            IDBHelper mubiaoDB,
            string mubiaoConnectionString,
            DataTable table,
            string tableName,
            SettingMove setting,
            List<DBTypeMappingModel> mapping)
        {
            int GetColumnNameType(DataTable tableInfo, string colName)
            {
                foreach (DataRow item in tableInfo.Rows)
                {
                    if (item["ColumnName"].ToString().ToLower() == colName.ToLower())
                    {
                        return (int)item["ProviderType"];
                    }
                }
                return -1;
            }

            var keys = new List<string>();  //保存主键列集合
            var (sqlString, newTableName) = await this.MySqlCreateTableBefore(mubiaoDB, mubiaoConnectionString, tableName, setting.TableCover);
            string identityName = string.Empty;
            List<string> columnNameList = new List<string>();
            var colsString = this.MSSQLTableConvert(table, mapping, DataBaseType.MySQL, (columnName, dataTypeName, mysqlType, isNull, isIdentity, isKey) =>
            {
                columnNameList.Add(columnName);
                if (isIdentity) identityName = columnName;                          //保存自增列名字
                if (isKey) keys.Add(columnName);                                    //保存主键
                return $"  `{columnName}` {mysqlType} {(isNull ? "NULL" : "NOT NULL")},{Environment.NewLine}";
            });
            sqlString.Append(colsString);
            sqlString.Append("  PRIMARY KEY (");
            foreach (var item in keys)
            {
                sqlString.Append($"`{item}`,");
            }
            sqlString = sqlString.Remove(sqlString.Length - 1, 1);
            sqlString.Append($"){Environment.NewLine});");
            await mubiaoDB.CreateTable(mubiaoConnectionString, sqlString.ToString());
            if (setting.TableData)
            {
                var mubiaoTableInfo = await mubiaoDB.QueryTableInfo(mubiaoConnectionString, newTableName);
                using (var dataReader = await yuanDB.QueryDataReader(yuanConnectionString, $"SELECT * FROM [{tableName}]"))
                {
                    while (dataReader.Read())
                    {
                        var insertSqlStirng = new StringBuilder($"INSERT INTO `{newTableName}`(");
                        var cols = new StringBuilder();
                        var @params = new List<IDataParameter>();
                        var colsParams = new StringBuilder();
                        foreach (var item in columnNameList)
                        {
                            var colType = GetColumnNameType(mubiaoTableInfo, item);
                            cols.Append($"`{item}`,");
                            colsParams.Append($"@{item},");
                            @params.Add(new MySqlParameter($"@{item}", dataReader[item]) { MySqlDbType = (MySqlDbType)colType });
                        }
                        cols.Remove(cols.Length - 1, 1);
                        colsParams.Remove(colsParams.Length - 1, 1);
                        insertSqlStirng.Append($"{cols.ToString()}) VALUES({colsParams.ToString()});");
                        await mubiaoDB.Insert(mubiaoConnectionString, insertSqlStirng.ToString(), @params);
                    }
                }
            }
            if (!string.IsNullOrEmpty(identityName))
            {
                await mubiaoDB.CreateTable(mubiaoConnectionString, $"ALTER TABLE `{newTableName}` MODIFY `{identityName}` INT AUTO_INCREMENT;");
            }
            return true;
        }

        /// <summary>
        /// Mysql的表, 转SQL Server建表SQL
        /// </summary>
        /// <param name="db">目标数据库操作对象</param>
        /// <param name="connectionString">目标数据库连接字符串</param>
        /// <param name="table">数据表信息</param>
        /// <param name="isCover">是否覆盖数据库表</param>
        /// <returns>MySQL建表SQL</returns>
        private async Task<string> MySqlTableToSqlServer(IDBHelper db, string connectionString, DataTable table, string tableName, bool isCover, List<DBTypeMappingModel> mapping)
        {
            var (sqlString, newTableName) = await this.MSSQLCreateTableBefore(db, connectionString, tableName, isCover);
            var colsString = this.MySqlTableConvert(table, mapping, DataBaseType.SQLServer, (columnName, dataTypeName, sqlServerType, isNull, isIdentity, isKey) =>
            {
                return $"{Environment.NewLine}  [{columnName}] {sqlServerType} {(isIdentity ? "IDENTITY(1,1)" : "")} {(isNull ? "NULL" : "NOT NULL")}{(isKey ? " PRIMARY KEY" : "")},";
            });
            sqlString.Append(colsString);
            sqlString = sqlString.Remove(sqlString.Length - 1, 1);
            sqlString.Append($"{Environment.NewLine}){Environment.NewLine}GO");
            return sqlString.ToString();
        }

        /// <summary>
        /// SQL Server建表前缀
        /// </summary>
        /// <param name="db"></param>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="isCover"></param>
        /// <returns></returns>
        private async Task<(StringBuilder, string)> MSSQLCreateTableBefore(IDBHelper db, string connectionString, string tableName, bool isCover)
        {
            var sqlString = new StringBuilder();
            var newTableName = tableName;
            if (isCover)    //覆盖表, 如果有
            {
                sqlString.Append($@"IF EXISTS (SELECT * FROM sys.all_objects WHERE object_id = OBJECT_ID(N'[dbo].[{tableName}]') AND type IN ('U'))
	DROP TABLE [dbo].[{tableName}]
CREATE TABLE [dbo].[{tableName}] (");
            }
            else
            {
                var isAny = await db.TableAny(connectionString, tableName);
                if (isAny)
                {
                    newTableName = $"{tableName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}";
                    sqlString.Append($"CREATE TABLE [dbo].[{newTableName}] (");
                }
                else
                {
                    sqlString.Append($"CREATE TABLE [dbo].[{tableName}] (");
                }
            }
            return (sqlString, newTableName);
        }

        /// <summary>
        /// 解析MySql的数据表信息
        /// </summary>
        /// <param name="table">MySql的表结构信息</param>
        /// <param name="mapping">数据库类型映射关系</param>
        /// <param name="dataBaseType">目标数据库类型</param>
        /// <param name="colFunc">每列的格式化函数, 有六个参数, 一个返回值
        /// <para>第一个string参数: columnName --> 列的名称</para>
        /// <para>第二个string参数: dataTypeName --> 源列数据类型, 如: varchar(30)</para>
        /// <para>第三个string参数: dbType --> 转换之后的列数据类型, 如: varchar(30)</para>
        /// <para>第四个bool参数: AllowDBNull --> 列是否可空</para>
        /// <para>第五个bool参数: IsIdentity --> 列是否是自增列(标识列)</para>
        /// <para>第六个bool参数: IsKey --> 列是否是主键</para>
        /// <para>返回值: string --> 格式化之后, 对应目标数据库的建表的列SQL</para>
        /// </param>
        /// <returns></returns>
        private string MySqlTableConvert(in DataTable table, in List<DBTypeMappingModel> mapping, in DataBaseType dataBaseType, in Func<string, string, string, bool, bool, bool, string> colFunc)
        {
            StringBuilder result = new StringBuilder();
            foreach (DataRow item in table.Rows)
            {
                var columnName = item["ColumnName"].ToString();
                var dataTypeName = item["DataTypeName"].ToString();
                var mappingRow = mapping.FirstOrDefault(f => f.MySql.TrimEnd('(', ',', ')').ToLower().Trim() == dataTypeName.ToLower());
                if (mappingRow == null) throw new Exception($"找不到源数据库 [{dataTypeName}] 类型的映射设置");
                var dbType = mappingRow.GetMappingByType(dataBaseType);
                if (dbType.EndsWith("()")) //表示该类型有一个长度设置, 保持和源数据库长度一致
                {
                    dbType = dbType.Insert(dbType.Length - 1, item["ColumnSize"].ToString());
                }
                else if (dbType.EndsWith("(,)")) //表示该类型有两个长度设置, 如(10,2)
                {
                    dbType = dbType.Insert(dbType.Length - 2, item["NumericPrecision"].ToString());
                    dbType = dbType.Insert(dbType.Length - 1, item["NumericScale"].ToString());
                }
                result.Append(colFunc?.Invoke(columnName, dataTypeName, dbType, (bool)item["AllowDBNull"], (bool)item["IsAutoIncrement"], (bool)item["IsKey"]));
            }
            return result.ToString();
        }

        /// <summary>
        /// MySQL迁移到MSSQL
        /// </summary>
        /// <param name="yuanDB">源数据库DB</param>
        /// <param name="yuanConnectionString">源数据库连接字符串</param>
        /// <param name="mubiaoDB">目标数据库DB</param>
        /// <param name="mubiaoConnectionString">目标数据库连接字符串</param>
        /// <param name="table">源数据表信息</param>
        /// <param name="tableName">表名</param>
        /// <param name="setting">设置</param>
        /// <param name="mapping">类型映射信息</param>
        /// <returns></returns>
        private async Task<bool> MySqlToSqlServer(
            IDBHelper yuanDB,
            string yuanConnectionString,
            IDBHelper mubiaoDB,
            string mubiaoConnectionString,
            DataTable table,
            string tableName,
            SettingMove setting,
            List<DBTypeMappingModel> mapping)
        {
            int GetColumnNameType(DataTable tableInfo, string colName)
            {
                foreach (DataRow item in tableInfo.Rows)
                {
                    if (item["ColumnName"].ToString().ToLower() == colName.ToLower())
                    {
                        return (int)item["ProviderType"];
                    }
                }
                return -1;
            }

            var keys = new List<string>();  //保存主键列集合
            var (sqlString, newTableName) = await this.MSSQLCreateTableBefore(mubiaoDB, mubiaoConnectionString, tableName, setting.TableCover);
            string identityName = string.Empty;
            List<string> columnNameList = new List<string>();
            var colsString = this.MySqlTableConvert(table, mapping, DataBaseType.MySQL, (columnName, dataTypeName, sqlServerType, isNull, isIdentity, isKey) =>
            {
                columnNameList.Add(columnName);
                if (isIdentity) identityName = columnName;                          //保存自增列名字
                if (isKey) keys.Add(columnName);                                    //保存主键
                return $"{Environment.NewLine}  [{columnName}] {sqlServerType} {(isIdentity ? "IDENTITY(1,1)" : "")} {(isNull ? "NULL" : "NOT NULL")}{(isKey ? " PRIMARY KEY" : "")},";
            });
            sqlString.Append(colsString);
            sqlString = sqlString.Remove(sqlString.Length - 1, 1);
            sqlString.Append($"{Environment.NewLine})");
            await mubiaoDB.CreateTable(mubiaoConnectionString, sqlString.ToString());
            if (setting.TableData)
            {
                var mubiaoTableInfo = await mubiaoDB.QueryTableInfo(mubiaoConnectionString, newTableName);
                using (var dataReader = await yuanDB.QueryDataReader(yuanConnectionString, $"SELECT * FROM `{tableName}`"))
                {
                    while (dataReader.Read())
                    {
                        var insertSqlStirng = new StringBuilder($"SET IDENTITY_INSERT [dbo].[{newTableName}] ON;{Environment.NewLine}INSERT INTO [dbo].[{newTableName}](");
                        var cols = new StringBuilder();
                        var @params = new List<IDataParameter>();
                        var colsParams = new StringBuilder();
                        foreach (var item in columnNameList)
                        {
                            var colType = GetColumnNameType(mubiaoTableInfo, item);
                            cols.Append($"[{item}],");
                            colsParams.Append($"@{item},");
                            @params.Add(new SqlParameter($"@{item}", dataReader[item]) { SqlDbType = (SqlDbType)colType });
                        }
                        cols.Remove(cols.Length - 1, 1);
                        colsParams.Remove(colsParams.Length - 1, 1);
                        insertSqlStirng.Append($"{cols.ToString()}) VALUES({colsParams.ToString()});{Environment.NewLine}SET IDENTITY_INSERT [dbo].[{newTableName}] OFF;");
                        await mubiaoDB.Insert(mubiaoConnectionString, insertSqlStirng.ToString(), @params);
                    }
                }
            }
            return true;
        }
    }
}