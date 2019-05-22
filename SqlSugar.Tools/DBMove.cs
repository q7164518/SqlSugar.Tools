using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.DBMoveTools.DBHelper;
using System;
using System.Drawing;
using System.Linq;
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
                            var dbListJson = string.Empty;
                            if (dbName == "SQL Server")
                            {
                                var dbList = await dBHelper.QueryDataTable(linkString, "select name from sysdatabases where dbid>4");
                                dbListJson = JsonConvert.SerializeObject(dbList);
                                dbList.Clear(); dbList.Dispose(); dbList = null;
                            }
                            else if (dbName == "MySQL")
                            {
                                var dbList = await dBHelper.QueryDataTable(linkString, "SELECT `SCHEMA_NAME` as name  FROM `information_schema`.`SCHEMATA` order by `SCHEMA_NAME`");
                                dbListJson = JsonConvert.SerializeObject(dbList);
                                dbList.Clear(); dbList.Dispose(); dbList = null;
                            }
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
    }
}