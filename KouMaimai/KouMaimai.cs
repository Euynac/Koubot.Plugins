using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.Plugin;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Maimai
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [KouPluginClass("mai", "Maimai",
        Introduction = "Maimai",
        Author = "7zou",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouMaimai : KouPlugin, IWantKouMessage
    {
        public KouMessage Message { get; set; }
        [KouPluginFunction]
        public override object Default(string str = null)
        {
            return "施工中，目前只有/mai.song";
        }
    }
}
