using System.Collections.Generic;
using System.Text;
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
            return ReturnHelp();
        }


        private static readonly double[] _rateSeq = {49,50,60,70,75,80,90,94,97,98,99,99.5,100,100.5};
        [KouPluginFunction(Name = "计算单曲rating", ActivateKeyword = "cal", Help = "如果不输入达成率，默认输出跳变阶段的所有rating")]
        public string CalRating([KouPluginArgument(Name = "定数", NumberMin = 1)]double constant, 
            [KouPluginArgument(Name= "达成率", NumberMin = 0, NumberMax = 101)]double? rate = null)
        {
            if (rate != null)
            {
                if (rate > 1.01)
                {
                    rate /= 100.0;
                }
                var rating = DxCalculator.CalSongRating(rate.Value, constant);
                return $"定数{constant:F1}，达成率{rate:P4}时，Rating为{rating}";
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"定数{constant}时：\n");
            for (int i = _rateSeq.Length - 1; i >= 0; i--)
            {
                var r = _rateSeq[i];
                var rating = DxCalculator.CalSongRating(r, constant);
                stringBuilder.Append($"{r}% ———— {rating}\n");
            }

            return stringBuilder.ToString().TrimEnd();
        }
    }
}
