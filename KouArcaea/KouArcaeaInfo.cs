using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Format;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Tool;
using KouGamePlugin.Arcaea.Models;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Microsoft.EntityFrameworkCore;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea歌曲数据类
    /// </summary>
    [KouPluginClass("arcinfo", "Arcaea歌曲数据服务",
        Introduction = "提供歌曲详细信息查询、随机歌曲功能，可限定条件",
        Author = "7zou",
        PluginType = PluginType.Game)]
    public class KouArcaeaInfo : KouPlugin<KouArcaeaInfo>, IWantCommandLifeKouContext, IWantKouUser,IWantKouGlobalConfig
    {
        [KouPluginFunction(Name = "查询歌曲信息", Help = "请使用/arc.song help")]
        public override object Default(string name = null)
        {
            return name == null ? ReturnHelp() : "歌曲信息查询请使用升级版的/arc.song help";
        }

        #region 歌曲别名
        [KouPluginFunction(ActivateKeyword = "add|教教", Name = "学新的歌曲别名", Help = "教kou一个歌曲的别名。")]
        public string KouLearnAnotherName(
            [KouPluginArgument(Name = "歌曲名等")]string songName, 
            [KouPluginArgument(Name = "要学的歌曲别名")]string songAnotherName)
        {
            if (songName.IsNullOrWhiteSpace() || songAnotherName.IsNullOrWhiteSpace()) return "好好教我嘛";
            var haveTheAlias = SongAlias.SingleOrDefault(p => p.Alias == songAnotherName);
            if (haveTheAlias != null)
                return $"可是我之前就知道{haveTheAlias.CorrespondingSong.SongTitle}可以叫做{songAnotherName}了";
            
            var song = SongAlias.SingleOrDefault(p => p.Alias == songName)?.CorrespondingSong;
            if (song == null)
            {
                var satisfiedSongs = Song.Find(s =>
                    s.SongId.ToString() == songName ||
                    s.SongTitle.Contains(songName,
                        StringComparison.OrdinalIgnoreCase)).ToList();
                if (satisfiedSongs.Count > 1) return $"具体是以下哪一首歌呢（暂时不支持选择id）：\n{satisfiedSongs.ToSetString()}";
                if (satisfiedSongs.Count == 0) return $"找不到哪个歌叫{songName}哦...";
                song = satisfiedSongs[0];
            }

            var sourceUser = CurrentPlatformUser.FindThis(KouContext);
            var dbSong = song.FindThis(KouContext);
            var havenHadAliases = dbSong.Aliases?.Select(p => p.Alias).ToStringJoin("、");
            var success = SongAlias.Add(alias =>
            {
                alias.CorrespondingSong = dbSong;
                alias.Alias = songAnotherName;
                alias.SourceUser = sourceUser;
            }, out var added, out var error, KouContext);
            if (success)
            {
                var reward = RandomTool.GenerateRandomInt(1, 2);
                CurrentPlatformUser.KouUser.GainCoinFree(reward);
                return $"学会了，{song.SongTitle}可以叫做{songAnotherName}({added.AliasID})" +
                       $"{havenHadAliases?.Be($"，我知道它还可以叫做{havenHadAliases}！")}（目前暂不会立即同步）\n" +
                       $"[{CurrentUser.FormatGainFreeCoin(CurrentKouGlobalConfig,reward)}!]";
            }
            return $"没学会，就突然：{error}";
        }
        [KouPluginFunction(ActivateKeyword = "del|delete|忘记", Name = "忘记歌曲别名", Help = "叫kou忘掉一个歌曲的别名。")]
        public string KouForgetAnotherName(
            [KouPluginArgument(Name = "别名ID")]List<int> ids)
        {
            if (ids.IsNullOrEmptySet()) return "这是叫我忘掉什么嘛";
            var result = new StringBuilder();
            foreach (var i in ids)
            {
                var alias = SongAlias.SingleOrDefault(a => a.AliasID == i);
                if (alias == null) result.Append($"\n不记得ID{i}");
                else if (alias.SourceUser != null && alias.SourceUser != CurrentPlatformUser &&
                         !CurrentPlatformUser.HasTheAuthority(Authority.BotManager))
                    result.Append($"\nID{i}是别人贡献的，不可以删噢");
                else
                {
                    result.Append($"\n忘记了{alias.ToString(FormatType.Brief)}");
                    alias.DeleteThis();
                };
            }

            return result.ToString().TrimStart();
        }
        #endregion

        public KouContext KouContext { get; set; }
        public PlatformUser CurrentPlatformUser { get; set; }
        public UserAccount CurrentUser { get; set; }
        public KouGlobalConfig CurrentKouGlobalConfig { get; set; }
    }
}
