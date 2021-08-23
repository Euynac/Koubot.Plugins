using Koubot.SDK.System;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Plugin;

namespace KouGamePlugin.Maimai
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [KouPluginClass("mai", "Maimai",
        Introduction = "Maimai",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Game,
        CanUseProxy = true)]
    public class KouMaimai : KouPlugin<KouMaimai>, IWantKouMessage
    {
        public KouMessage Message { get; set; }
        [KouPluginFunction]
        public override object Default(string str = null)
        {
            return "施工中，目前只有/mai.song";
        }
    }
}
