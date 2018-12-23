using Newtonsoft.Json;

namespace SqlSugar.Tools.Model
{
    /// <summary>
    /// 设置信息实体类
    /// </summary>
    internal class SettingsModel
    {
        /// <summary>
        /// 导入的命名空间
        /// </summary>
        [JsonProperty(PropertyName = "namespace")]
        public string Namespace { get; set; }

        /// <summary>
        /// 实体类命名空间
        /// </summary>
        [JsonProperty(PropertyName = "entityNamespace")]
        public string EntityNamespace { get; set; }

        /// <summary>
        /// 实体类要继承的父类
        /// </summary>
        [JsonProperty(PropertyName = "baseClassName")]
        public string BaseClassName { get; set; }

        /// <summary>
        /// 实体类名称开头大写个数
        /// </summary>
        [JsonProperty(PropertyName = "classCapsCount")]
        public ushort ClassCapsCount { get; set; }

        /// <summary>
        /// 属性名称开头大写个数
        /// </summary>
        [JsonProperty(PropertyName = "propCapsCount")]
        public ushort PropCapsCount { get; set; }

        /// <summary>
        /// string类型是否去空格
        /// </summary>
        [JsonProperty(PropertyName = "propTrim")]
        public bool PropTrim { get; set; }

        /// <summary>
        /// 值类型默认值
        /// </summary>
        [JsonProperty(PropertyName = "propDefault")]
        public bool PropDefault { get; set; }

        /// <summary>
        /// 是否标识主键
        /// </summary>
        [JsonProperty(PropertyName = "sqlSugarPK")]
        public bool SqlSugarPK { get; set; }

        /// <summary>
        /// 是否标识自增列
        /// </summary>
        [JsonProperty(PropertyName = "sqlSugarBZL")]
        public bool SqlSugarBZL { get; set; }

        /// <summary>
        /// get自定义格式
        /// </summary>
        [JsonProperty(PropertyName = "getCus")]
        public string GetCus { get; set; }

        /// <summary>
        /// set自定义格式
        /// </summary>
        [JsonProperty(PropertyName = "setCus")]
        public string SetCus { get; set; }

        /// <summary>
        /// 实体类自定义特性
        /// </summary>
        [JsonProperty(PropertyName = "cusAttr")]
        public string CusAttr { get; set; }

        /// <summary>
        /// 自定义构造函数
        /// </summary>
        [JsonProperty(PropertyName = "cusGouZao")]
        public string CusGouZao { get; set; }

        /// <summary>
        /// 生成类型
        /// </summary>
        [JsonProperty(PropertyName = "propType")]
        public PropType PropType { get; set; }
    }

    internal enum PropType
    {
        /// <summary>
        /// 简易模式
        /// </summary>
        Easy = 0,

        /// <summary>
        /// 模版模式
        /// </summary>
        Moban = 1
    }
}
