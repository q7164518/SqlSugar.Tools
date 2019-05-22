using Chromium.Event;
using NetDimension.NanUI;
using SqlSugar.Tools.SQLHelper;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    public partial class Main : Formium
    {
        public Main()
            : base("http://my.resource.local/pages/Index.html")
        {
            InitializeComponent();
            this.MinimumSize = new Size(1100, 690);
            //this.MaximumSize = new Size(1000, 690);
            this.StartPosition = FormStartPosition.CenterParent;
            GlobalObject.AddFunction("exit").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    Application.Exit();
                    GC.Collect();
                });
            };

            GlobalObject.AddFunction("toGithub").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    Process.Start("https://github.com/sunkaixuan/sqlsugar/");
                });
            };

            GlobalObject.AddFunction("toHome").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    Process.Start("http://www.codeisbug.com/");
                });
            };

            GlobalObject.AddFunction("toWord").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    Process.Start("http://www.codeisbug.com/Doc/8");
                });
            };

            GlobalObject.AddFunction("showCreateEntity").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    CreateEntity.ShowWindow();
                });
            };

            GlobalObject.AddFunction("showDBMove").Execute += (func, args) =>
            {
                this.RequireUIThread(() =>
                {
                    DBMove.ShowWindow();
                });
            };

            var qq = base.GlobalObject.AddObject("qq");
            var addedQun = qq.AddFunction("addedQun");
            addedQun.Execute += (func, args) =>
            {
                var url = ((args.Arguments.FirstOrDefault(p => p.IsString)?.StringValue) ?? string.Empty).Trim();
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(url);
                }
            };

            base.LoadHandler.OnLoadStart += LoadHandler_OnLoadStart;
        }

        private void LoadHandler_OnLoadStart(object sender, CfxOnLoadStartEventArgs e)
        {
            //base.Chromium.ShowDevTools();
            //var connString = "Host=192.168.152.129;Port=5432;Username=root123456;Password=123456;Database=test;";
            //var s = connString.Substring(connString.IndexOf("Username=") + 9, connString.IndexOf(";Password=")- connString.IndexOf("Username=")-9);
            //MessageBox.Show(s);

            //if (PostgreSqlHelper.TestLink1(connString))
            //{
            //    MessageBox.Show("OK");
            //    var s =PostgreSqlHelper.QueryTableInfo(connString, "SELECT * FROM \"TB_Test\" WHERE 1=2").Result;
            //}
            //else
            //{
            //    MessageBox.Show("Error");
            //}
        }
    }
}