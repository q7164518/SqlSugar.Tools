using NetDimension.NanUI;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SqlSugar.Tools
{
    public partial class DBTypeSetting : Formium
    {
        public DBTypeSetting()
            : base("http://my.resource.local/pages/DBTypeSetting.html")
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
        }
    }
}