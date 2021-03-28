using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Tool;
using Koubot.Tool.String;
using KouGamePlugin.Arcaea.Models;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [KouPluginClass("arc", "Arcaea助手",
        Introduction = "提供随机歌曲、计算ptt等功能",
        Author = "7zou",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouArcaea : KouPlugin, IWantKouMessage
    {
        public KouMessage Message { get; set; }

        [KouPluginFunction(Name = "获取当前用户最近一次成绩（施工中）", Help = "默认功能，需要先绑定")]
        public override object Default(string str = null)
        {
            return null;
            return Message.ToString(FormatType.Detail);
        }
        [KouPluginFunction(ActivateKeyword = "info", Help = "<歌曲名/别名> 查询歌曲信息，更先进的功能要使用arcinfo")]
        public object KouArcInfo(string name = null)
        {
            return null;
            KouArcaeaInfo arcaeaInfo = new KouArcaeaInfo();
            return arcaeaInfo.Default(name);
        }
        [KouPluginFunction(ActivateKeyword = "bind|绑定", Help = "绑定arc账号(暂时只支持ID不支持名字)")]
        public string KouBindArc(string arcID = null)
        {
            return null;
        }
        [KouPluginFunction(ActivateKeyword = "cal|计算", Name = "计算单曲ptt", Help = "计算出那个分数的ptt")]
        public string KouCalConstant([KouPluginArgument(Name = "定数/歌曲名[+难度类型]")] string nameOrConstant,
            [KouPluginArgument(Name = "分数")] int score,
            [KouPluginArgument(Name = "歌曲ptt反推定数")] double? ptt = null)
        {
            if (score < 0 || score > 11000000) return "这个分数怎么有点奇怪呢";
            if (ptt != null)
            {
                return $"{score}分的谱面ptt={ptt}时，谱面定数约为{ArcaeaData.CalSongChartConstant(ptt.Value, score):F3}";
            }
            if (KouStringTool.TryToDouble(nameOrConstant, out double constant))
            {
                return $"定数{constant}时，{score}分的ptt为{ArcaeaData.CalSongScorePtt(constant, score):F3}";
            }
            KouArcaeaInfo kouArcaeaInfo = new KouArcaeaInfo
            {
                SongName = nameOrConstant
            };
            var songs = kouArcaeaInfo.GetSatisfiedSong(ratingClass: PluginArcaeaSong.RatingClass.Future);
            string result = "";
            if (songs.Count == 0) return $"找不到是哪个歌";
            else if (songs.Count > 4)
            {
                result = $"具体是指下面哪个歌呢？\n{songs.ToSetString()}";
            }
            else
            {
                foreach (var song in songs)
                {
                    if (song.ChartConstant == null) continue;
                    double con = song.ChartConstant.Value;
                    result += $"{song.ToString(FormatType.Brief)} {score}分的ptt为{ArcaeaData.CalSongScorePtt(con, score):F3}\n";
                }
            }
            return result.Trim();
        }
    }
}
