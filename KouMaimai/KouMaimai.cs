using System.Collections.Generic;
using System.Linq;
using System.Text;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.Services.Interface;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.String;
using KouGamePlugin.Maimai.Models;
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
    public class KouMaimai : KouPlugin<KouMaimai>, IWantKouMessage, IWantKouSession
    {
        public KouMessage Message { get; set; }

        [KouPluginFunction]
        public override object Default(string str = null)
        {
            return ReturnHelp();
        }

        private static List<SongChart> TryGetSongCharts(string aliasName)
        {
            var list = SongChart.Find(p => p.BasicInfo.Aliases.Any(a => a.Alias == aliasName));
            if (list.Count == 0)
            {
                list = SongChart.Find(p => p.BasicInfo.Aliases.Any(a => a.Alias.Contains(aliasName)));
            }

            return list;
        }

        [KouPluginFunction(Name ="使用别名获取歌曲详情", ActivateKeyword = "alias")]
        public object GetSongByAlias(string aliasName)
        {
            var list = TryGetSongCharts(aliasName);
            if (list.IsNullOrEmptySet()) return $"不知道{aliasName}是什么歌呢";
            if (list.Count > 1)
            {
                return list.ToAutoPageSetString("具体是下面哪首歌呢？\n");
            }

            return $"您要找的是不是：\n{list.First().ToString(FormatType.Detail)}";
        }



        private static readonly double[] _rateSeq = {49,50,60,70,75,80,90,94,97,98,99,99.5,100,100.5};
        [KouPluginFunction(Name = "计算单曲rating", ActivateKeyword = "cal", Help = "如果不输入达成率，默认输出跳变阶段的所有rating")]
        public string CalRating([KouPluginArgument(Name = "定数/歌曲名")]string constantOrName, 
            [KouPluginArgument(Name= "达成率", NumberMin = 0, NumberMax = 101)]double? rate = null)
        {
            SongChart song = null;
            if (!double.TryParse(constantOrName, out double constant))
            {
                if (constantOrName != null)
                {
                    if (!constantOrName.StartsWithAny(false, out string difficultStr, "白", "紫", "红", "黄", "绿") ||
                        !difficultStr.TryGetKouEnum(out SongChart.RatingType type))
                    {
                        type = SongChart.RatingType.Master;
                    }
                    else
                    {
                        constantOrName = constantOrName[1..];
                    }
                    
                    var list = TryGetSongCharts(constantOrName);
                    if (list.Count == 1)
                    {
                        song = list[0];
                    }
                    else
                    {
                        list.AddRange(SongChart.Find(p => p.BasicInfo.SongTitle.Contains(constantOrName)));
                        list = list.Distinct().ToList();
                        if (list.Count == 1)
                        {
                            song = list[0];
                        }
                        else if(list.Count > 1)
                        {
                            using (SessionService)
                            {
                                var id = SessionService.Ask<int>($"具体是下面哪首歌呢？输入id：\n{list.ToSetString(endAt:5)}");
                                song = SongChart.Find(p => p.ChartId == id).FirstOrDefault();
                            }
                        }
                    }

                    if (song == null) return "不知道是什么歌呢";
                    constant = song.GetSpecificConstant(type) ?? 0;
                    if (constant == 0) return $"Kou还不知道{type.GetKouEnumFirstName()}{song.BasicInfo.SongTitle}的定数呢";
                }
                else
                {
                    return "需要提供正确的歌曲名或者定数哦";
                }
            }

            var songFormat = song?.Be(song.ToString(FormatType.Brief) + "\n");
            if (rate != null)
            {
                if (rate > 1.01)
                {
                    rate /= 100.0;
                }
                var rating = DxCalculator.CalSongRating(rate.Value, constant);
                return $"{songFormat}定数{constant:F1}，达成率{rate:P4}时，Rating为{rating}";
            }

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append($"{songFormat}定数{constant}时：\n");
            for (int i = _rateSeq.Length - 1; i >= 0; i--)
            {
                var r = _rateSeq[i];
                var rating = DxCalculator.CalSongRating(r, constant);
                stringBuilder.Append($"{r}% ———— {rating}\n");
            }

            return stringBuilder.ToString().TrimEnd();
        }

        public IKouSessionService SessionService { get; set; }
    }
}
