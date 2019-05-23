namespace SqlSugar.Tools.Model
{
    internal class DBTypeMappingModel
    {
        /// <summary>
        /// SQL Server类型
        /// </summary>
        public string MSSQL { get; set; }

        /// <summary>
        /// mysql类型
        /// </summary>
        public string MySql { get; set; }

        /// <summary>
        /// SQLite类型
        /// </summary>
        public string SQLite { get; set; }

        /// <summary>
        /// Oracle类型
        /// </summary>
        public string Oracle { get; set; }

        /// <summary>
        /// PostregSQL类型
        /// </summary>
        public string PostregSQL { get; set; }

        /// <summary>
        /// 映射关系描述
        /// </summary>
        public string Desc { get; set; }
    }
}