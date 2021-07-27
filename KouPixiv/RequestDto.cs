namespace KouFunctionPlugin.Pixiv
{
    /// <summary>
    /// https://api.lolicon.app/
    /// </summary>
    public class RequestDto
    {
        /// <summary>
        /// 0为非 R18，1为 R18，2为混合（在库中的分类，不等同于作品本身的 R18 标识）
        /// </summary>
        public int? R18 { get; set; }

        /// <summary>
        /// 一次返回的结果数量，范围为1到100；在指定关键字或标签的情况下，结果数量可能会不足指定的数量
        /// </summary>
        public int? Num { get; set; }

        /// <summary>
        /// 返回指定uid作者的作品，最多20个
        /// </summary>
        public int[]? Uid { get; set; }

        /// <summary>
        /// 返回从标题、作者、标签中按指定关键字模糊匹配的结果，大小写不敏感，性能和准度较差且功能单一，建议使用tag代替
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 返回匹配指定标签的作品，详见下文
        /// </summary>
        public string[]? Tag { get; set; }

        /// <summary>
        /// ["original"]	返回指定图片规格的地址，详见下文
        /// </summary>
        public string[]? Size { get; set; }

        /// <summary>
        /// i.pixiv.cat	设置图片地址所使用的在线反代服务，详见下文
        /// </summary>
        public string? Proxy { get; set; }

        /// <summary>
        /// 设置为任意真值以禁用对某些缩写keyword和tag的自动转换，详见下文
        /// </summary>
        public bool? Dsc { get; set; }

    }
}