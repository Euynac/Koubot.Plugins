using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Tool;
using Koubot.Tool.Expand;
using Koubot.Tool.String;
using KouGamePlugin.Arcaea.Models;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [KouPluginClass(
        Introduction = "提供随机歌曲、计算ptt等功能",
        Author = "7zou",
        ActivateName = "arc",
        Title = "Arcaea助手",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouArcaea : KouPlugin, IWantKouMessage
    {
        public KouMessage Message { get; set; }

        [KouPluginFunction(Name = "获取当前用户最近一次成绩", Help = "默认功能，需要先绑定")]
        public override object Default(string str = null)
        {
            return Message.ToString(FormatType.Detail);
        }
        [KouPluginFunction(ActivateKeyword = "info", Help = "<歌曲名/别名> 查询歌曲信息，更先进的功能要使用arcinfo")]
        public object KouArcInfo(string name)
        {
            KouArcaeaInfo arcaeaInfo = new KouArcaeaInfo();
            return arcaeaInfo.Default(name);
        }
        [KouPluginFunction(ActivateKeyword = "bind|绑定", Help = "绑定arc账号(暂时只支持ID不支持名字)")]
        public string KouBindArc(string arcID)
        {
            return "施工中";
        }
        [KouPluginFunction(ActivateKeyword = "cal|计算", Name = "计算单曲ptt", Help = "<定数/歌曲名[+难度类型]> <(int)分数>，计算出那个分数的ptt")]
        public string KouCalConstant(string nameOrConstant, int score)
        {
            if (score < 0 || score > 11000000) return "这个分数怎么有点奇怪呢";
            if (KouStringTool.TryToDouble(nameOrConstant, out double constant))
            {
                return $"定数{constant}时，{score}分的ptt为{ArcaeaData.CalSongScorePtt(constant, score).RetainDecimal(3)}";
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
                result = $"具体是指下面哪个歌呢？\n{songs.ToPageSetString()}";
            }
            else
            {
                foreach (var song in songs)
                {
                    if (song.ChartConstant == null) continue;
                    double con = song.ChartConstant.Value;
                    result += $"{song.ToString(FormatType.Brief)} {score}分的ptt为{ArcaeaData.CalSongScorePtt(con, score).RetainDecimal(3)}\n";
                }
            }
            return result.Trim();
        }
    }
}
