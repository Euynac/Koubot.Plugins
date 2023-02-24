using Koubot.SDK.PluginInterface;
using Koubot.Tool.Extensions;
using Koubot.Tool.String;
using KouGamePlugin.Arcaea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using KouArcaea.Room;
using Koubot.SDK.PluginExtension;
using Koubot.SDK.System;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    [PluginClass("arc", "Arcaea助手",
        Author = "7zou",
        PluginType = PluginType.Game,
        CanUseProxy = true)]
    public class KouArcaea : KouPlugin<KouArcaea>
    {

        static KouArcaea()
        {
            PluginEventList.FetchGroupGameInfo += sender =>
            {
                if (sender.CurGroup?.HasInstallPlugin(GetPluginMetadataStatic().Info) is true)
                {
                    var info = GetPluginMetadataStatic().Info;
                    var func = info.GetFunctionInfo(nameof(GuessImage));
                    return new PluginEventList.GameInfo()
                    {
                        GameCommand =
                            $"{KouCommand.GetPluginRoute(sender.CurKouGlobalConfig, info, nameof(GuessImage))} --help",
                        Introduce = func?.FunctionHelp ?? "??",
                        GameName = func?.FunctionName ?? "??",
                        IsSessionRoomGame = true
                    };
                }

                return null;
            };
        }
        [PluginFunction(Name = "获取当前用户最近一次成绩（施工中）", Help = "默认功能，需要先绑定")]
        public override object? Default(string? str = null)
        {
            return null;
        }
        [PluginFunction(ActivateKeyword = "bind|绑定", Help = "绑定arc账号(暂时只支持ID不支持名字)")]
        public string KouBindArc(string arcID = null)
        {
            return null;
        }
        [PluginFunction(ActivateKeyword = "cal|计算", Name = "计算单曲ptt", Help = "计算出那个分数的ptt")]
        public string KouCalConstant([PluginArgument(Name = "定数/歌曲名[+难度类型]")] string nameOrConstant,
            [PluginArgument(Name = "分数")] int score,
            [PluginArgument(Name = "歌曲ptt反推定数")] double? ptt = null)
        {
            if (score < 0 || score > 11000000) return "这个分数怎么有点奇怪呢";
            var songName = nameOrConstant;
            if (ptt != null)
            {
                return $"{score}分的谱面ptt={ptt}时，谱面定数约为{ArcaeaData.CalSongChartConstant(ptt.Value, score):F3}";
            }

            var ratingClass = Song.RatingClass.Future;
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
            Song song = null;
            if (satisfiedSongs.Count > 1)
            {
                song = SessionService.AskWhichOne(satisfiedSongs);
                if (song == null) return null;
            }
            if (satisfiedSongs.Count == 0)
            {
                if (KouStringTool.TryToDouble(nameOrConstant, out var constant))
                {
                    return $"定数{constant}时，{score}分的ptt为{ArcaeaData.CalSongScorePtt(constant, score):F3}";
                }
                return $"找不到哪个歌叫{songName}哦...";
            }
            song ??= satisfiedSongs[0];
            var songConstant = song.MoreInfo.FirstOrDefault(p => p.ChartRatingClass == ratingClass)?.ChartConstant;
            if (songConstant == null) return $"{song.SongTitle}还没有{ratingClass}的定数信息呢...";
            return $"{song.SongTitle}[{ratingClass}{songConstant.Value:0.#}]{score}分的ptt为{ArcaeaData.CalSongScorePtt(songConstant.Value, score):F3}";
        }

        #region 游戏房间

        [PluginFunction(Name = "Arcaea猜图游戏", ActivateKeyword = "guess image|猜图",Help = ArcaeaImageGuessGameRoom.Help
            , OnlyUsefulInGroup = true, NeedCoin = 10, CanEarnCoin = true)]
        public object? GuessImage([PluginArgument(Name = "入场费(最低10)", Min = 10)] int? fee = null)
        {
            fee ??= 10;
            if (!CurKouUser.ConsumeCoinFree(fee.Value)) return FormatNotEnoughCoin(fee.Value, CurUserName);
            var room = new ArcaeaImageGuessGameRoom("Arcaea猜图", CurUser, CurGroup, fee)
            {
                LastTime = CurCommand.CustomTimeSpan ?? new TimeSpan(0,10,0)
            };
            ConnectRoom(
                $"{CurUserName}消耗{CurKouGlobalConfig.CoinFormat(fee.Value)}创建了游戏房间：{room.RoomName}，后续收到的入场费({CurKouGlobalConfig.CoinFormat(fee.Value)})将累计在奖池中，按排名发放奖励",
                room);
            return null;
        }

        [PluginFunction(Name = "检查图片情况", Authority = Authority.BotMaster)]
        public object? CheckImage()
        {
            var list = new List<string>();
            foreach (var (url, p) in Song.GetCache()!.Select(p=>(p.JacketUrl, p)))
            {
                if(url.IsNullOrWhiteSpace()) continue;
                var u = new KouImage(url, new Song());
                if (!u.LocalExists())
                {
                    list.Add(p.ToString(FormatType.Brief));
                }
            }

            return $"共找到{list.Count}个缺失：\n{list.StringJoin("\n")}";
        }

        #endregion
    }
}
