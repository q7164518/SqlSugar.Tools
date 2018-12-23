using Newtonsoft.Json;

namespace SqlSugar.Tools.Model
{
    /// <summary>
    /// 连接信息
    /// </summary>
    [System.Serializable]
    internal class LinkModel
    {
        /// <summary>
        /// 连接字符串
        /// </summary>
        [JsonProperty(PropertyName = "linkString")]
        public string LinkString { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        [JsonProperty(PropertyName = "type")]
        public DataBaseType Type { get; set; }

        /// <summary>
        /// 连接名称
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string LinkName { get; set; }

        /// <summary>
        /// 数据库帐号
        /// </summary>
        [JsonProperty(PropertyName = "account")]
        public string Account { get; set; }

        /// <summary>
        /// 数据库密码
        /// </summary>
        [JsonProperty(PropertyName = "pwd")]
        public string Password { get; set; }

        /// <summary>
        /// MySQL端口号
        /// </summary>
        [JsonProperty(PropertyName = "port")]
        public ushort Port { get; set; }

        /// <summary>
        /// 主机地址
        /// </summary>
        [JsonProperty(PropertyName = "host")]
        public string Host { get; set; }
    }

    /// <summary>
    /// 数据库类型
    /// </summary>
    internal enum DataBaseType
    {
        SQLServer = 1,
        MySQL = 2,
        Oracler = 3,
        SQLite = 4,
        PostgreSQL = 5
    }
}