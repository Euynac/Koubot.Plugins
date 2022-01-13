using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.String;
using KouGamePlugin.Arcaea.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using KouMessage = Koubot.Shared.Protocol.KouMessage;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [KouPluginClass("arc", "Arcaea助手",
        Author = "7zou",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouArcaea : KouPlugin<KouArcaea>
    {
        [KouPluginFunction(Name = "获取当前用户最近一次成绩（施工中）", Help = "默认功能，需要先绑定")]
        public override object? Default(string? str = null)
        {
            return null;
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
            string songName = nameOrConstant;
            if (ptt != null)
            {
                return $"{score}分的谱面ptt={ptt}时，谱面定数约为{ArcaeaData.CalSongChartConstant(ptt.Value, score):F3}";
            }

            Song.RatingClass ratingClass = Song.RatingClass.Future;
            if (songName.MatchOnceThenReplace(@"[,，]?(ftr|pst|prs|byd|byn|future|past|present|beyond)",
                out songName, out var matched, RegexOptions.IgnoreCase | RegexOptions.RightToLeft))
            {
                KouStringTool.TryToEnum(matched[1].Value, out ratingClass);
            }

            if (songName.IsNullOrWhiteSpace())
            {
                return "你在说哪首歌呢";
            }

            var satisfiedSongs = Song.Find(s =>
                s.SongTitle.Contains(songName,
                    StringComparison.OrdinalIgnoreCase)
                || s.Aliases?.Any(alias => alias.Alias == songName) ==
                true);
            if (satisfiedSongs.Count > 1) return $"具体是以下哪一首歌呢（暂时不支持选择id）：\n{satisfiedSongs.ToSetString()}";
            if (satisfiedSongs.Count == 0)
            {
                if (KouStringTool.TryToDouble(nameOrConstant, out double constant))
                {
                    return $"定数{constant}时，{score}分的ptt为{ArcaeaData.CalSongScorePtt(constant, score):F3}";
                }
                return $"找不到哪个歌叫{songName}哦...";
            }
            var song = satisfiedSongs[0];
            var songConstant = song.MoreInfo.FirstOrDefault(p => p.ChartRatingClass == ratingClass)?.ChartConstant;
            if (songConstant == null) return $"{song.SongTitle}还没有{ratingClass}的定数信息呢...";
            return $"{song.SongTitle}[{ratingClass}{songConstant.Value:0.#}]{score}分的ptt为{ArcaeaData.CalSongScorePtt(songConstant.Value, score):F3}";
        }
    }
}
