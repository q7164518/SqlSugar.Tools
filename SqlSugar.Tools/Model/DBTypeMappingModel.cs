namespace SqlSugar.Tools.Model
{
    internal class DBTypeMappingModel
    {
        public int ID { get; set; }

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

        /// <summary>
        /// 根据数据库类型, 获得对应的数据库类型
        /// </summary>
        /// <param name="dataBaseType">数据库类型</param>
        /// <returns></returns>
        public string GetMappingByType(DataBaseType dataBaseType)
        {
            switch (dataBaseType)
            {
                case DataBaseType.SQLServer:
                    return this.MSSQL.Trim();
                case DataBaseType.MySQL:
                    return this.MySql.Trim();
                case DataBaseType.Oracler:
                    return this.Oracle.Trim();
                case DataBaseType.SQLite:
                    return this.SQLite.Trim();
                case DataBaseType.PostgreSQL:
                    return this.PostregSQL.Trim();
                default:
                    throw new System.Exception("不支持该数据库");
            }
        }
    }
}