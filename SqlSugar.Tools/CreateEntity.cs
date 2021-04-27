using Chromium.Event;
using NetDimension.NanUI;
using Newtonsoft.Json;
using SqlSugar.Tools.Model;
using SqlSugar.Tools.SQLHelper;
using SqlSugar.Tools.Tools;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web;

namespace SqlSugar.Tools
{
    public partial class CreateEntity : Formium
    {
        public static CreateEntity _CreateEntity = null;

        public CreateEntity()
            : base("http://my.resource.local/pages/CreateEntity.html")
        {
            InitializeComponent();
            this.MinimumSize = new Size(1100, 690);
            this.StartPosition = FormStartPosition.CenterParent;
            GlobalObject.AddFunction("exit").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    this.Close();
                    GC.Collect();
                });
            };
            this.RegiestSQLServerFunc();
            this.RegiestSQLiteFunc();
            this.RegiestMySqlFunc();
            this.RegiestPGSqlFunc();
            this.RegiestOracleFunc();
            base.LoadHandler.OnLoadEnd += LoadHandler_OnLoadEnd;
        }

        /// <summary>
        /// 注册SQL Server数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestSQLServerFunc()
        {
            var sqlServer = base.GlobalObject.AddObject("sqlServer");
            var testLink = sqlServer.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await SQLServerHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                            var dbList = await SQLServerHelper.QueryDataTable(linkString, "select name from sysdatabases where dbid>4");
                            var dbListJson = JsonConvert.SerializeObject(dbList);
                            dbList.Clear(); dbList.Dispose(); dbList = null;
                            EvaluateJavascript($"setDbList('{dbListJson}')", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = sqlServer.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.SQLServer);
                        tables.Columns["TableName"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
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

            var createOne = sqlServer.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.SQLServer, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
			code = HttpUtility.JavaScriptStringEncode(code);
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = sqlServer.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.SQLServer, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = sqlServer.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.SQLServer, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        /// <summary>
        /// 注册SQLite数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestSQLiteFunc()
        {
            var sqlite = base.GlobalObject.AddObject("sqlite");
            var selectDBFile = sqlite.AddFunction("selectDBFile");  //选择db文件方法
            selectDBFile.Execute += (func, args) =>
            {
                using(var openFileDialog = new OpenFileDialog
                {
                    Multiselect = false,
                    Title = "请选择SQLite文件",
                    Filter = "SQLite文件(*.db)|*.db|所有文件(*.*)|*.*"
                })
                {
                    if (openFileDialog.ShowDialog()== DialogResult.OK)
                    {
                        //string file = openFileDialog.FileName;//返回文件的完整路径
                        EvaluateJavascript($"setSQLiteFilePath('{openFileDialog.FileName.Replace("\\","\\\\")}')", (value, exception) => { });
                    }
                }
            };

            var testLink = sqlite.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await SQLiteHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接SQLite", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = sqlite.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.SQLite);
                        tables.Columns["name"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables);
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
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

            var createOne = sqlite.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableName"], settings, DataBaseType.SQLite, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = sqlite.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableName"], settings, DataBaseType.SQLite, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = sqlite.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["label"], settings, DataBaseType.SQLite, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        /// <summary>
        /// 注册MySql数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestMySqlFunc()
        {
            var mysql = base.GlobalObject.AddObject("mysql");
            var testLink = mysql.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await MySQLHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                            var dbList = await MySQLHelper.QueryDataTable(linkString, "SELECT `SCHEMA_NAME` as name  FROM `information_schema`.`SCHEMATA` order by `SCHEMA_NAME`");
                            var dbListJson = JsonConvert.SerializeObject(dbList);
                            dbList.Clear(); dbList.Dispose(); dbList = null;
                            EvaluateJavascript($"setDbList('{dbListJson}')", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接MySql", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = mysql.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.MySQL);
                        tables.Columns["TableName"].ColumnName = "label";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
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

            var createOne = mysql.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.MySQL, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = mysql.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.MySQL, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = mysql.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.MySQL, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        /// <summary>
        /// 注册pgSql数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestPGSqlFunc()
        {
            var pgsql = base.GlobalObject.AddObject("pgsql");
            var testLink = pgsql.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await PostgreSqlHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接PostgreSQL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = pgsql.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.PostgreSQL);
                        tables.Columns["TableName"].ColumnName = "label";
                        tables.Columns["tabledesc"].ColumnName = "TableDesc";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
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

            var createOne = pgsql.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.PostgreSQL, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = pgsql.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.PostgreSQL, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = pgsql.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.PostgreSQL, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        /// <summary>
        /// 注册Oracle数据库操作要用到的方法到JS
        /// </summary>
        private void RegiestOracleFunc()
        {
            var oracle = base.GlobalObject.AddObject("oracle");
            var testLink = oracle.AddFunction("testLink");    //测试数据库连接
            testLink.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        if (await OracleHelper.TestLink(linkString))
                        {
                            EvaluateJavascript("testSuccessMsg()", (value, exception) => { });
                        }
                        else
                        {
                            MessageBox.Show("测试连接失败", "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "测试连接Oracle", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var loadingTables = oracle.AddFunction("loadingTables");    //加载数据库的表
            loadingTables.Execute += async (func, args) =>
            {
                var linkString = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(linkString))
                {
                    try
                    {
                        var tables = await this.LoadingTables(linkString, DataBaseType.Oracler);
                        tables.Columns["TableName"].ColumnName = "label";
                        tables.Columns["tabledesc"].ColumnName = "TableDesc";
                        var tablesJson = JsonConvert.SerializeObject(tables).Replace("\r\n", "").Replace("\\r\\n", "").Replace("\\", "\\\\");
                        tables.Clear(); tables.Dispose(); tables = null;
                        EvaluateJavascript($"setTables('{tablesJson}')", (value, exception) => { });
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

            var createOne = oracle.AddFunction("createOne");    //生成一个表
            createOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.Oracler, true);
                        code = code
                            .Replace("\r\n", "<br/>")
                            .Replace("using ", "<span style=\"color:#CE04B0\">using </span>")
                            .Replace("namespace ", "<span style=\"color:#CE04B0\">namespace </span>")
                            .Replace("public ", "<span style=\"color:#CE04B0\">public </span>")
                            .Replace("private ", "<span style=\"color:#CE04B0\">private </span>")
                            .Replace("class ", "<span style=\"color:#CE04B0\">class </span>")
                            .Replace("get ", "<span style=\"color:#CE04B0\">get </span>")
                            .Replace("set ", "<span style=\"color:#CE04B0\">set </span>")
                            .Replace("get;", "<span style=\"color:#CE04B0\">get;</span>")
                            .Replace("set;", "<span style=\"color:#CE04B0\">set;</span>")
                            .Replace("return ", "<span style=\"color:#FF4500\">return </span>")
                            .Replace("this.", "<span style=\"color:#CE04B0\">this.</span>")
                            .Replace("SugarColumn", "<span style=\"color:red\">SugarColumn</span>")
                            .Replace("true", "<span style=\"color:#008B8B\">true</span>")
                            .Replace("??", "<span style=\"color:#E9D372\">??</span>")
                            .Replace("?.", "<span style=\"color:#E9D372\">?.</span>")
                            .Replace("default(", "<span style=\"color:#CE04B0\">default(</span>");
                        code = Regex.Replace(code, @"/// <summary>(?<str>.*?)/// </summary>", "<span style=\"color:green\">/// &lt;summary&gt;${str}/// &lt;/summary&gt;</span>");
                        EvaluateJavascript($"getEntityCode('{code}')", (value, exception) => { });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "预览代码", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveOne = oracle.AddFunction("saveOne"); //保存单个实体类
            saveOne.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var code = await this.GetEntityCode(infos["linkString"], infos["tableName"], infos["tableDesc"], settings, DataBaseType.Oracler, false);
                        using (var saveFileDialog = new SaveFileDialog()
                        {
                            DefaultExt = "cs",
                            Filter = "C#类(*.cs)|*.cs",
                            FileName = $"{(settings.ClassCapsCount > 0 ? infos["tableName"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : infos["tableName"])}.cs",
                            RestoreDirectory = true,
                            Title = "保存单个实体类"
                        })
                        {
                            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                            {
                                var localFilePath = saveFileDialog.FileName.ToString();
                                using (StreamWriter sw = new StreamWriter(localFilePath, false))
                                {
                                    await sw.WriteLineAsync(code);
                                }
                                EvaluateJavascript("saveOneSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };

            var saveAllTables = oracle.AddFunction("saveAllTables"); //保存所有表生成的实体类
            saveAllTables.Execute += async (func, args) =>
            {
                var info = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(info))
                {
                    try
                    {
                        var initPath = "";
                        if (File.Exists($"{Environment.CurrentDirectory}\\default.ini"))
                        {
                            initPath = File.ReadAllText($"{Environment.CurrentDirectory}\\default.ini", Encoding.Default);
                        }
                        var infos = JsonConvert.DeserializeObject<Dictionary<string, string>>(info);
                        var settings = JsonConvert.DeserializeObject<SettingsModel>(infos["settings"]);
                        var tableList = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(infos["tableList"]);
                        using (var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = initPath })
                        {
                            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                            {
                                foreach (var item in tableList)
                                {
                                    var code = await this.GetEntityCode(infos["linkString"], item["label"], item["TableDesc"], settings, DataBaseType.Oracler, false);
                                    using (StreamWriter sw = new StreamWriter(folderBrowserDialog.SelectedPath + "\\" + (settings.ClassCapsCount > 0 ? item["label"].SetLengthToUpperByStart((int)settings.ClassCapsCount) : item["label"]) + ".cs"))
                                    {
                                        await sw.WriteAsync(code);
                                    }
                                }
                                using (StreamWriter sw = new StreamWriter($"{Environment.CurrentDirectory}\\default.ini"))
                                {
                                    await sw.WriteAsync(folderBrowserDialog.SelectedPath);
                                }
                                EvaluateJavascript("saveAllTablesSuccess()", (value, exception) => { });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        EvaluateJavascript("hideLoading()", (value, exception) => { });
                        GC.Collect();
                    }
                }
                else
                {
                    MessageBox.Show("获取数据库连接字符串错误", "保存所有实体类", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    EvaluateJavascript("hideLoading()", (value, exception) => { });
                }
            };
        }

        public static void ShowWindow()
        {
            if (CreateEntity._CreateEntity == null)
            {
                CreateEntity._CreateEntity = new CreateEntity();
            }
            CreateEntity._CreateEntity.Show();
            CreateEntity._CreateEntity.Focus();
        }

        private void CreateEntity_FormClosed(object sender, FormClosedEventArgs e)
        {
            CreateEntity._CreateEntity.Dispose();
            this.Dispose();
            CreateEntity._CreateEntity = null;
            GC.Collect();
        }

        private void LoadHandler_OnLoadEnd(object sender, CfxOnLoadEndEventArgs e)
        {
            // Check if it is the main frame when page has loaded.
            //if (e.Frame.IsMain)
            //{
            //    EvaluateJavascript("sayHelloToSomeone('C#1111111')", (value, exception) =>
            //    {
            //        if (value.IsString)
            //        {
            //            // Get value from Javascript.
            //            var jsValue = value.StringValue;

            //            MessageBox.Show(jsValue);
            //        }
            //    });
            //}
            //base.Chromium.ShowDevTools();
        }

        /// <summary>
        /// 加载所有表
        /// </summary>
        /// <param name="linkString">连接字符串</param>
        private async Task<DataTable> LoadingTables(string linkString, DataBaseType type)
        {
            switch (type)
            {
                case DataBaseType.SQLServer:
                    var sql = @"select name as TableName, ISNULL(j.TableDesc, '') as TableDesc  From sysobjects g
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
                    var table1 = await SQLServerHelper.QueryDataTable(linkString, sql);
                    sql = @"select name as TableName,'' as TableDesc   From sysobjects j where j.xtype='V' order by name asc";
                    var table2 = await SQLServerHelper.QueryDataTable(linkString, sql);
                    DataTable newDataTable = table1.Clone();
                    object[] obj = new object[newDataTable.Columns.Count];
                    for (int i = 0; i < table1.Rows.Count; i++)
                    {
                        table1.Rows[i].ItemArray.CopyTo(obj, 0);
                        newDataTable.Rows.Add(obj);
                    }

                    for (int i = 0; i < table2.Rows.Count; i++)
                    {
                        table2.Rows[i].ItemArray.CopyTo(obj, 0);
                        newDataTable.Rows.Add(obj);
                    }

                    return newDataTable;
                case DataBaseType.MySQL:
                    var database = linkString.Substring(linkString.IndexOf("Database=") + 9, linkString.IndexOf(";port=") - linkString.IndexOf("Database=") - 9);
                    var sql1 = $"SELECT TABLE_NAME as TableName, Table_Comment as TableDesc FROM INFORMATION_SCHEMA.TABLES where TABLE_SCHEMA = '{database}' order by TableName asc";
                    return await MySQLHelper.QueryDataTable(linkString, sql1);
                case DataBaseType.Oracler:
                    var oracleSql = "select table_name as TableName,comments as tabledesc from user_tab_comments order by table_name asc";
                    return await OracleHelper.QueryDataTable(linkString, oracleSql);
                case DataBaseType.SQLite:
                    return await SQLiteHelper.QueryDataTable(linkString, "SELECT name FROM sqlite_master order by name asc");
                case DataBaseType.PostgreSQL:
                    //var tableowner = linkString.Substring(linkString.IndexOf("Username=") + 9, linkString.IndexOf(";Password=") - linkString.IndexOf("Username=") - 9);
                    var sql2 = $@"SELECT
	t2.tablename AS TableName,
	CAST (obj_description(t1.oid, 'pg_class') AS VARCHAR) AS TableDesc 
FROM
	pg_class t1
	LEFT JOIN pg_tables t2 ON t1.relname = t2.tablename 
WHERE
	t2.schemaname = 'public'
ORDER BY
	t1.relname ASC";
                    return await PostgreSqlHelper.QueryDataTable(linkString, sql2);
                default:
                    return null;
            }
        }

        /// <summary>
        /// 获得类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        Type GetTypeByString(string type)
        {
            switch (type.ToLower())
            {
                case "system.boolean":
                    return Type.GetType("System.Boolean", true, true);
                case "system.byte":
                    return Type.GetType("System.Byte", true, true);
                case "system.sbyte":
                    return Type.GetType("System.SByte", true, true);
                case "system.char":
                    return Type.GetType("System.Char", true, true);
                case "system.decimal":
                    return Type.GetType("System.Decimal", true, true);
                case "system.double":
                    return Type.GetType("System.Double", true, true);
                case "system.single":
                    return Type.GetType("System.Single", true, true);
                case "system.int32":
                    return Type.GetType("System.Int32", true, true);
                case "system.uint32":
                    return Type.GetType("System.UInt32", true, true);
                case "system.int64":
                    return Type.GetType("System.Int64", true, true);
                case "system.uint64":
                    return Type.GetType("System.UInt64", true, true);
                case "system.object":
                    return Type.GetType("System.Object", true, true);
                case "system.int16":
                    return Type.GetType("System.Int16", true, true);
                case "system.uint16":
                    return Type.GetType("System.UInt16", true, true);
                case "system.string":
                    return Type.GetType("System.String", true, true);
                case "system.datetime":
                case "datetime":
                    return Type.GetType("System.DateTime", true, true);
                case "system.guid":
                    return Type.GetType("System.Guid", true, true);
                default:
                    return Type.GetType(type, true, true);
            }
        }

        /// <summary>
        /// 生成实体类代码
        /// </summary>
        /// <param name="linkString">连接字符串</param>
        /// <param name="nodeDesc">表注释</param>
        /// <param name="nodeName">表名称</param>
        /// <param name="settings">设置</param>
        /// <param name="type">数据库类型</param>
        /// <param name="isYuLan">是否是单笔预览</param>
        /// <returns></returns>
        private async Task<string> GetEntityCode(string linkString, string nodeName, string nodeDesc, SettingsModel settings, DataBaseType type, bool isYuLan)
        {
            StringBuilder codeString = new StringBuilder();
            DataTable tableInfo = null;
            DataTable colsInfos = null;
            if (type == DataBaseType.SQLServer)
            {
                tableInfo = await SQLServerHelper.QueryTableInfo(linkString, $"select * from [{nodeName}] where 1=2");
                colsInfos = await SQLServerHelper.QueryDataTable(linkString, "SELECT objname,value FROM ::fn_listextendedproperty (NULL, 'user', 'dbo', 'table', '" + nodeName + "', 'column', DEFAULT)", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsIdentity",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.MySQL)
            {
                var database = linkString.Substring(linkString.IndexOf("Database=") + 9, linkString.IndexOf(";port=") - linkString.IndexOf("Database=") - 9);
                tableInfo = await MySQLHelper.QueryTableInfo(linkString, $"select * from `{nodeName}` where 1=2");
                colsInfos = await MySQLHelper.QueryDataTable(linkString, $"select COLUMN_NAME as OBJNAME,column_comment as VALUE from INFORMATION_SCHEMA.Columns where table_name='{nodeName}' and table_schema='{database}'", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.SQLite)
            {
                tableInfo = await SQLiteHelper.QueryTableInfo(linkString, $"select * from '{nodeName}' where 1=2");
                colsInfos = await SQLiteHelper.QueryDataTable(linkString, $"PRAGMA table_info('{nodeName}')", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "name",
                    "ColumnName",
                    "name",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.PostgreSQL)
            {
                tableInfo = await PostgreSqlHelper.QueryTableInfo(linkString, $"select * from \"{nodeName}\" where 1=2");
                colsInfos = await PostgreSqlHelper.QueryDataTable(linkString, $@"SELECT
	col_description(A.attrelid, A.attnum) AS value,
	A.attname AS objname
FROM
	pg_class AS C,
	pg_attribute AS A 
WHERE
	C.relname = '{nodeName}'
	AND A.attrelid = C.oid 
	AND A.attnum >0", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "objname",
                    "ColumnName",
                    "value",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            else if (type == DataBaseType.Oracler)
            {
                tableInfo = await OracleHelper.QueryTableInfo(linkString, $"select * from \"{nodeName}\" where 1=2");
                tableInfo.Columns.Add("IsAutoIncrement", typeof(bool));
                for (int i = 0; i < tableInfo.Rows.Count; i++)
                {
                    tableInfo.Rows[i]["IsAutoIncrement"] = false;
                }
                colsInfos = await OracleHelper.QueryDataTable(linkString, $"select column_name as OBJNAME,comments as VALUE from user_col_comments where table_name = '{nodeName}'", null);
                this.GetCode(
                    tableInfo,
                    colsInfos,
                    "OBJNAME",
                    "ColumnName",
                    "VALUE",
                    "IsKey",
                    "IsAutoIncrement",
                    "DataType",
                    "AllowDBNull",
                    linkString,
                    nodeName,
                    nodeDesc,
                    settings,
                    isYuLan,
                    codeString);
            }
            tableInfo?.Clear();
            tableInfo?.Dispose();
            colsInfos?.Clear();
            colsInfos?.Dispose();
            GC.Collect();
            return codeString.ToString();
        }

        /// <summary>
        /// 获得实体类代码
        /// </summary>
        /// <param name="tableInfo">表信息</param>
        /// <param name="colsInfos">列信息</param>
        /// <param name="objname">从列信息DataTabel中取列名的key</param>
        /// <param name="columnName">从表信息DataTabel中取列名的key</param>
        /// <param name="zhuShiValueName">从列信息DataTabel中取列注释的key</param>
        /// <param name="isKeyName">从表信息DataTabel中取列名是不是主键的key</param>
        /// <param name="isIdentityName">从表信息DataTabel中取列是不是自增的key</param>
        /// <param name="dataTypeName">从表信息DataTabel中取列名数据类型的key</param>
        /// <param name="allowDBNullName">从表信息DataTabel中取列名是不是允许为null的key</param>
        /// <param name="linkString">连接字符串</param>
        /// <param name="nodeName">表名</param>
        /// <param name="nodeDesc">表注释</param>
        /// <param name="settings">设置信息</param>
        /// <param name="isYuLan">是否是预览</param>
        /// <param name="codeString"></param>
        /// <returns></returns>
        private void GetCode(
            DataTable tableInfo, 
            DataTable colsInfos, 
            string objname,
            string columnName,
            string zhuShiValueName,
            string isKeyName,
            string isIdentityName,
            string dataTypeName,
            string allowDBNullName,
            string linkString, 
            string nodeName, 
            string nodeDesc, 
            SettingsModel settings, 
            bool isYuLan, 
            StringBuilder codeString)
        {
            string tableName = (settings.ClassCapsCount > 0 ? nodeName.SetLengthToUpperByStart((int)settings.ClassCapsCount) : nodeName);
            codeString.Append($@"using SqlSugar;{(string.IsNullOrWhiteSpace(settings.Namespace) ? "" : $"{Environment.NewLine}{settings.Namespace.Trim()}")}

namespace {settings.EntityNamespace.Trim()}
{{
    /// <summary>
    /// {nodeDesc}
    /// </summary>{(string.IsNullOrWhiteSpace(settings.CusAttr) ? "" : $"{Environment.NewLine}    {settings.CusAttr.Trim()}")}
    public class {tableName}{(string.IsNullOrWhiteSpace(settings.BaseClassName) ? "" : $" : {settings.BaseClassName.Trim()}")}
    {{
        /// <summary>
        /// {nodeDesc}
        /// </summary>
        public {tableName}()
        {{{(string.IsNullOrWhiteSpace(settings.CusGouZao) ? "" : Environment.NewLine + "          " + settings.CusGouZao.Trim().Replace("-tableName-", isYuLan ? $"<span style=\"color:yellow\">{tableName}</span>" : tableName))}
        }}
");
            if (settings.PropType== PropType.Easy)  //建议模式, 属性只生成get; set; 属性自定义模版失效
            {
                foreach (DataRow dr in tableInfo.Rows)
                {
                    var zhuShi = string.Empty;//列名注释
                    foreach (DataRow uu in colsInfos.Rows)
                    {
                        if (uu[objname].ToString().ToUpper() == dr[columnName].ToString().ToUpper())
                            zhuShi = uu[zhuShiValueName].ToString();
                    }
                    if ((bool)dr[isKeyName] && !(bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarPK)
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public -dbType- -colName- {{ get; set; }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get; set; }}
");
                        }
                    }
                    else if ((bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarPK && settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public -dbType- -colName- {{ get; set; }}
");
                        }
                        else if (settings.SqlSugarPK && !settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public -dbType- -colName- {{ get; set; }}
");
                        }
                        else if (!settings.SqlSugarPK && settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsIdentity = true)]
        public -dbType- -colName- {{ get; set; }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get; set; }}
");
                        }
                    }
                    else if (!(bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsIdentity = true)]
        public -dbType- -colName- {{ get; set; }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get; set; }}
");
                        }
                    }
                    else
                    {
                        codeString.Append($@"
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get; set; }}
");
                    }
                    Type ttttt = this.GetTypeByString(dr[dataTypeName].ToString());
                    if (ttttt.IsValueType && dr[allowDBNullName].ToString() == "True")
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}?</span>" : dr[dataTypeName].ToString() + "?");  //替换数据类型
                        if (settings.PropDefault)
                        {
                            codeString.Replace("-value-", $"value ?? default({(isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString())})");
                        }
                        else
                        {
                            codeString.Replace("-value-", "value");
                        }
                    }
                    else if (ttttt.IsValueType)
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                        codeString.Replace("-value-", "value");
                    }
                    else
                    {
                        if (dr[dataTypeName].ToString() == "System.String")
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                            if (settings.PropTrim)
                            {
                                codeString.Replace("-value-", "value?.Trim()");
                            }
                            else
                            {
                                codeString.Replace("-value-", "value");
                            }
                        }
                        else
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                            codeString.Replace("-value-", "value");
                        }
                    }
                    codeString.Replace("-colName-", settings.PropCapsCount > 0 ? dr[columnName].ToString().SetLengthToUpperByStart((int)settings.PropCapsCount) : dr[columnName].ToString());  //替换列名（属性名）
                    codeString.Replace("-zhuShi-", zhuShi.Replace("\r\n", "\r\n        ///"));
                }



            }
            else
            {
                var getString = settings.GetCus.Trim();
                if (string.IsNullOrWhiteSpace(getString))
                {
                    getString = "return this._-colName-;";
                }
                else
                {
                    getString = getString.Replace("属性", "-colName-");
                }
                var setString = settings.SetCus.Trim();
                if (string.IsNullOrWhiteSpace(setString))
                {
                    setString = "this._-colName- = -value-;";
                }
                else
                {
                    setString = setString.Replace("属性", "-colName-");
                }
                foreach (DataRow dr in tableInfo.Rows)
                {
                    var zhuShi = string.Empty;//列名注释
                    foreach (DataRow uu in colsInfos.Rows)
                    {
                        if (uu[objname].ToString().ToUpper() == dr[columnName].ToString().ToUpper())
                            zhuShi = uu[zhuShiValueName].ToString();
                    }
                    if ((bool)dr[isKeyName] && !(bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarPK)
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                    }
                    else if ((bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarPK && settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                        else if (settings.SqlSugarPK && !settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                        else if (!settings.SqlSugarPK && settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsIdentity = true)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                    }
                    else if (!(bool)dr[isKeyName] && (bool)dr[isIdentityName])
                    {
                        if (settings.SqlSugarBZL)
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        [SugarColumn(IsIdentity = true)]
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                        else
                        {
                            codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                        }
                    }
                    else
                    {
                        codeString.Append($@"
        private -dbType- _-colName-;
        /// <summary>
        /// -zhuShi-
        /// </summary>
        public -dbType- -colName- {{ get {{ {getString} }} set {{ {setString} }} }}
");
                    }
                    Type ttttt = this.GetTypeByString(dr[dataTypeName].ToString());
                    if (ttttt.IsValueType && dr[allowDBNullName].ToString() == "True")
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}?</span>" : dr[dataTypeName].ToString() + "?");  //替换数据类型
                        if (settings.PropDefault)
                        {
                            codeString.Replace("-value-", $"value ?? default({(isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString())})");
                        }
                        else
                        {
                            codeString.Replace("-value-", "value");
                        }
                    }
                    else if (ttttt.IsValueType)
                    {
                        codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:#23C645\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                        codeString.Replace("-value-", "value");
                    }
                    else
                    {
                        if (dr[dataTypeName].ToString() == "System.String")
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                            if (settings.PropTrim)
                            {
                                codeString.Replace("-value-", "value?.Trim()");
                            }
                            else
                            {
                                codeString.Replace("-value-", "value");
                            }
                        }
                        else
                        {
                            codeString.Replace("-dbType-", isYuLan ? $"<span style=\"color:red\">{dr[dataTypeName].ToString()}</span>" : dr[dataTypeName].ToString());  //替换数据类型
                            codeString.Replace("-value-", "value");
                        }
                    }
                    codeString.Replace("-colName-", settings.PropCapsCount > 0 ? dr[columnName].ToString().SetLengthToUpperByStart((int)settings.PropCapsCount) : dr[columnName].ToString());  //替换列名（属性名）
                    codeString.Replace("-zhuShi-", zhuShi.Replace("\r\n", "\r\n        ///"));
                }
            }
            codeString.Append(@"    }
}");
        }
    }
}
