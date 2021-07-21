using Koubot.SDK.Protocol.Plugin;
using System;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Cytus
{
    [KouPluginClass("cy", "Cytus助手",
        Introduction = "提供随机歌曲、计算小p等功能",
        Author = "7zou",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouCytus : KouPlugin<KouCytus>
    {
        [KouPluginFunction(ActivateKeyword = "cal", Name = "计算小P")]
        public string CalTP(double tp, int perfect, int good = 0, int bad = 0, int miss = 0)
        {
            if (tp > 100 || tp <= 0) return "TP是不是有点奇怪...";
            if (perfect <= 0 || good < 0 || bad < 0 || miss < 0) return "我不会算欸";
            double total = perfect + good + bad + miss;
            var tp_perfect_score = tp - good * 30.0 / total;
            var nm_perfect = (perfect / total * 100 - tp_perfect_score) / 30.0 * total;
            nm_perfect = Math.Round(nm_perfect);
            var tp_perfect = perfect - nm_perfect;
            var real_tp = (tp_perfect * 100 + nm_perfect * 70 + good * 30) / total;
            var tp_error = real_tp - tp;
            return $"彩P：{tp_perfect}\n黑P：{nm_perfect}\n真实TP：{real_tp:F5}";
        }
        [KouPluginFunction]
        public override object Default(string str = null) => ReturnHelp();
    }
}
