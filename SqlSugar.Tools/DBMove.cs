using Chromium.Event;
using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.DBMoveTools.DBHelper;
using SqlSugar.Tools.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
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
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "");
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
                var tablesJson = (args.Arguments[0].StringValue ?? string.Empty).Trim();
                var yuanDBName = (args.Arguments[1].StringValue ?? string.Empty).Trim();
                var yuanConnectionString = (args.Arguments[2].StringValue ?? string.Empty).Trim();

                var mubiaoDBName = (args.Arguments[3].StringValue ?? string.Empty).Trim();
                var mubiaoConnectionString = (args.Arguments[4].StringValue ?? string.Empty).Trim();
                tablesJson = (tablesJson ?? string.Empty).Trim();
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
                    foreach (var table in tables)
                    {
                        var dy = await yuanDBHelper.QueryTableInfo(yuanConnectionString, table.TableName);
                        var createTableSqlString = this.SqlServerTableToMySql(dy, table.TableName);
                        var result = await mubiaoDBHelper.CreateTable(mubiaoConnectionString, createTableSqlString);
                        ;
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

        private readonly List<DBTypeMappingModel> SqlServerMappingTest = new List<DBTypeMappingModel>
        {
            new DBTypeMappingModel { MSSQL = "bigint", MySql = "bigint", SQLite = "integer", Oracle = "number(19)", PostregSQL = "bigint", Desc = "" },
            new DBTypeMappingModel { MSSQL = "binary", MySql = "binary", SQLite = "integer", Oracle = "raw(1-2000)/blob", PostregSQL = "bytea", Desc = "" },
            new DBTypeMappingModel { MSSQL = "bit", MySql = "tinyint", SQLite = "integer", Oracle = "number(1)", PostregSQL = "boolean", Desc = "" },
            new DBTypeMappingModel { MSSQL = "char", MySql = "char", SQLite = "integer", Oracle = "char(1-2000)/varchar2(2001-4000)/clob", PostregSQL = "char", Desc = "" },
            new DBTypeMappingModel { MSSQL = "date", MySql = "date", SQLite = "integer", Oracle = "date", PostregSQL = "date", Desc = "" },
            new DBTypeMappingModel { MSSQL = "datetime", MySql = "datetime", SQLite = "integer", Oracle = "date", PostregSQL = "timestamp", Desc = "" },
            new DBTypeMappingModel { MSSQL = "datetime2", MySql = "datetime", SQLite = "integer", Oracle = "timestamp(7)", PostregSQL = "timestamp", Desc = "" },
            new DBTypeMappingModel { MSSQL = "datetimeoffset", MySql = "datetime", SQLite = "integer", Oracle = "timestamp(7)", PostregSQL = "timestamp", Desc = "" },
            new DBTypeMappingModel { MSSQL = "decimal", MySql = "decimal", SQLite = "integer", Oracle = "number", PostregSQL = "numeric", Desc = "" },
            new DBTypeMappingModel { MSSQL = "float", MySql = "float", SQLite = "integer", Oracle = "float", PostregSQL = "double", Desc = "" },
            new DBTypeMappingModel { MSSQL = "int", MySql = "int", SQLite = "integer", Oracle = "number(10)", PostregSQL = "integer", Desc = "" },
            new DBTypeMappingModel { MSSQL = "money", MySql = "float", SQLite = "integer", Oracle = "number(19,4)", PostregSQL = "numeric(19,4)", Desc = "" },
            new DBTypeMappingModel { MSSQL = "nchar", MySql = "char", SQLite = "integer", Oracle = "char(1-1000)/nclob", PostregSQL = "varchar", Desc = "" },
            new DBTypeMappingModel { MSSQL = "ntext", MySql = "text", SQLite = "integer", Oracle = "nclob", PostregSQL = "text", Desc = "" },
            new DBTypeMappingModel { MSSQL = "numeric", MySql = "decimal", SQLite = "integer", Oracle = "number", PostregSQL = "numeric", Desc = "" },
            new DBTypeMappingModel { MSSQL = "nvarchar", MySql = "varchar", SQLite = "integer", Oracle = "varchar2(1-2000)/nclob", PostregSQL = "varchar", Desc = "" },
            new DBTypeMappingModel { MSSQL = "real", MySql = "float", SQLite = "integer", Oracle = "real", PostregSQL = "real", Desc = "" },
            new DBTypeMappingModel { MSSQL = "smalldatetime", MySql = "datetime", SQLite = "integer", Oracle = "date", PostregSQL = "timestamp", Desc = "" },
            new DBTypeMappingModel { MSSQL = "smallint", MySql = "smallint", SQLite = "integer", Oracle = "number(5)", PostregSQL = "smallint", Desc = "" },
            new DBTypeMappingModel { MSSQL = "smallmoney", MySql = "float", SQLite = "integer", Oracle = "number(10,4)", PostregSQL = "numeric(10,4)", Desc = "" },
            new DBTypeMappingModel { MSSQL = "text", MySql = "text", SQLite = "integer", Oracle = "clob", PostregSQL = "text", Desc = "" },
            new DBTypeMappingModel { MSSQL = "time", MySql = "time", SQLite = "integer", Oracle = "varchar(16)", PostregSQL = "timestamp", Desc = "" },
            new DBTypeMappingModel { MSSQL = "timestamp", MySql = "timestamp", SQLite = "integer", Oracle = "raw(8)", PostregSQL = "bigint", Desc = "" },
            new DBTypeMappingModel { MSSQL = "tinyint", MySql = "tinyint", SQLite = "integer", Oracle = "number(3)", PostregSQL = "smallint", Desc = "" },
            new DBTypeMappingModel { MSSQL = "uniqueidentifier", MySql = "varchar(40)", SQLite = "integer", Oracle = "char(40)", PostregSQL = "smallint", Desc = "" },
            new DBTypeMappingModel { MSSQL = "varbinary", MySql = "varbinary", SQLite = "integer", Oracle = "raw(1-2000)/clob", PostregSQL = "bytea", Desc = "" },
            new DBTypeMappingModel { MSSQL = "varchar", MySql = "varchar", SQLite = "integer", Oracle = "varchar2(1-4000)/clob", PostregSQL = "varchar", Desc = "" },
            new DBTypeMappingModel { MSSQL = "xml", MySql = "text", SQLite = "integer", Oracle = "nclob", PostregSQL = "text", Desc = "" }
        };

        /// <summary>
        /// SQL Server的表, 转Mysql建表SQL
        /// </summary>
        /// <param name="table">数据表信息</param>
        /// <returns>MySQL建表SQL</returns>
        private string SqlServerTableToMySql(DataTable table, string tableName)
        {
            var keys = new List<string>();  //保存主键列集合
            var sqlString = new StringBuilder($@"
CREATE TABLE `{tableName}`  (
");
            foreach (DataRow item in table.Rows)
            {
                var columnName = item["ColumnName"].ToString();
                var dataTypeName = item["DataTypeName"].ToString();
                var newDataTypeName = SqlServerMappingTest.FirstOrDefault(f => f.MSSQL.ToLower() == dataTypeName.ToLower()).MySql;
                switch (Convert.ToInt32(item["ProviderType"]))
                {
                    case 3:     //char
                    case 12:    //nvarchar
                    case 22:    //varchar
                        newDataTypeName += $"({item["ColumnSize"]})"; break;   //表示该字段有一个长度设置, 比如varchar(20)
                    case 5:     //decimal
                        newDataTypeName += $"({item["NumericPrecision"]},{item["NumericScale"]})"; break;   //表示该字段有两个个长度设置, decimal(18,2)
                    default: break;
                }
                sqlString.Append($"`{columnName}` {newDataTypeName} {(((bool)item["AllowDBNull"]) ? "NULL" : "NOT NULL")} {(((bool)item["IsIdentity"]) ? "AUTO_INCREMENT" : "")},{Environment.NewLine}");
                if ((bool)item["IsKey"]) keys.Add(columnName);  //保存主键
            }
            sqlString.Append("PRIMARY KEY (");
            foreach (var item in keys)
            {
                sqlString.Append($"`{item}`,");
            }
            sqlString = sqlString.Remove(sqlString.Length - 1, 1);
            sqlString.Append($"){Environment.NewLine});");
            return sqlString.ToString();
        }
    }
}