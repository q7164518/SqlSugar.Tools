using Chromium;
using NetDimension.NanUI;
using System;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //指定CEF架构和文件目录结构，并初始化CEF
            if (Bootstrap.Load(Settings(), CommandLine()))
            {
                LoadResources();
                Application.Run(new Main());
            }
        }
        /// <summary>
        /// 加载资源
        /// </summary>
        static void LoadResources()
        {
            //注册嵌入资源，默认资源指定假的域名res.app.local
            Bootstrap.RegisterAssemblyResources(System.Reflection.Assembly.GetExecutingAssembly());
            //注册嵌入资源，并为指定资源指定一个假的域名my.resource.local
            Bootstrap.RegisterAssemblyResources(System.Reflection.Assembly.GetExecutingAssembly(), "wwwroot", "my.resource.local");
            //加载分离式(外部)的资源
            //var separateAssembly = System.Reflection.Assembly.LoadFile(System.IO.Path.Combine(Application.StartupPath, "EmbeddedResourcesInSplitAssembly.dll"));
            //注册外部的嵌入资源，并为指定资源指定一个假的域名separate.resource.local
            //Bootstrap.RegisterAssemblyResources(separateAssembly, "separate.resource.local");
        }
        static Action<CfxSettings> Settings()
        {
            return settings =>
            {
                settings.LogSeverity = CfxLogSeverity.Disable;//禁用日志

                //指定中文为当前CEF环境的默认语言
                settings.AcceptLanguageList = "zh-CN";
                settings.Locale = "zh-CN";
            };
        }
        static Action<CfxCommandLine> CommandLine()
        {
            return commandLine =>
            {
                //在启动参数中添加disable-web-security开关，禁用跨域安全检测
                commandLine.AppendSwitch("disable-web-security");
            };
        }
    }
}
