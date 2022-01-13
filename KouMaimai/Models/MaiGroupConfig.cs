using Koubot.Shared.Models;

namespace KouGamePlugin.Maimai.Models;

public class MaiGroupConfig : PluginGroupConfig
{
    /// <summary>
    /// 设定的默认地区，用于快捷查看几卡，避免别名冲突
    /// </summary>
    public string MapDefaultArea { get; set; }
}