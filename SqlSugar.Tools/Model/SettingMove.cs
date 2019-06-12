using Newtonsoft.Json;

namespace SqlSugar.Tools.Model
{
    public class SettingMove
    {
        /// <summary>
        /// 是否迁移表数据
        /// </summary>
        [JsonProperty(PropertyName = "tableData")]
        public bool TableData { get; set; }

        /// <summary>
        /// 是否覆盖已有的数据表
        /// </summary>
        [JsonProperty(PropertyName = "tableCover")]
        public bool TableCover { get; set; }

        /// <summary>
        /// 是否同名表自动添加后缀
        /// </summary>
        [JsonProperty(PropertyName = "tableAny")]
        public bool TableAny { get; set; }

        /// <summary>
        /// 每次查询多少行数据进行迁移
        /// </summary>
        [JsonProperty(PropertyName = "dataRows")]
        public int DataRows { get; set; }

        /// <summary>
        /// 是否只生成建表SQL
        /// </summary>
        [JsonProperty(PropertyName = "onlySql")]
        public bool OnlySql { get; set; }
    }
}