using Chromium.Event;
using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.DBMoveTools.DBHelper;
using SqlSugar.Tools.Model;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
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

            this.RegiestFunc("SQL Server", "sqlServer");
            this.RegiestFunc("MySQL", "mysql");
            base.LoadHandler.OnLoadEnd += (object sender, CfxOnLoadEndEventArgs e) =>
            {
                base.Chromium.ShowDevTools();
            };
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
                            if (dbName == "SQL Server")
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

        private IDBHelper CreateDBHelper(string dbName)
        {
            switch (dbName)
            {
                case "SQL Server": return new SqlServerDBHelper();
                case "MySQL": return new MySqlDBHelper();
                default: return null;
            }
        }

        private DataBaseType GetDataBaseType(string dbName)
        {
            switch (dbName)
            {
                case "SQL Server": return DataBaseType.SQLServer;
                case "MySQL": return DataBaseType.MySQL;
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
    }
}