namespace SqlSugar.Tools.Tools
{
    internal static class StringPlugins
    {
        /// <summary>
        /// 判断指定的字符串是 null、空还是仅由空白字符组成。
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <returns>true或false</returns>
        public static bool IsNullOrWhiteSpace(this string str) => string.IsNullOrWhiteSpace(str);

        /// <summary>
        /// 首字母转大写
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <returns>转换后的字符串</returns>
        public static string FristToUpper(this string str) => str.IsNullOrWhiteSpace() ? string.Empty : str[0].ToString().ToUpper() + str.Substring(1);

        /// <summary>
        /// 指定长度首字母转大写, 默认为1
        /// </summary>
        /// <param name="str"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static string SetLengthToUpperByStart(this string str, int length = 1)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return string.Empty;
            }
            str = str.Trim();
            if (length >= str.Length)
            {
                return str.ToUpper();
            }
            var itemString = string.Empty;
            for (int i = 0; i < length; i++)
            {
                itemString += str[i].ToString().ToUpper();
            }
            return itemString + str.Substring(length);
        }
    }
}
