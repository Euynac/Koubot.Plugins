using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using KouMessage = Koubot.Shared.Protocol.KouMessage;

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
