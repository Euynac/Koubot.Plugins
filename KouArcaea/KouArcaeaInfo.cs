﻿using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using KouGamePlugin.Arcaea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea歌曲数据类
    /// </summary>
    [PluginClass("arcinfo", "Arcaea歌曲数据服务",
        Introduction = "提供歌曲详细信息查询、随机歌曲功能，可限定条件",
        Author = "7zou",
        PluginType = PluginType.Game)]
    public class KouArcaeaInfo : KouPlugin<KouArcaeaInfo>
    {
        [PluginFunction(Name = "查询歌曲信息", Help = "请使用/arc.song help")]
        public override object? Default(string? name = null)
        {
            return name == null ? ReturnHelp() : "歌曲信息查询请使用升级版的/arc.song help";
        }

        #region 歌曲别名
        [PluginFunction(ActivateKeyword = "add|教教", Name = "学新的歌曲别名", Help = "教kou一个歌曲的别名。")]
        public object KouLearnAnotherName(
            [PluginArgument(Name = "歌曲名等")] string songName,
            [PluginArgument(Name = "要学的歌曲别名")] string songAnotherName)
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
                if (satisfiedSongs.Count > 1) return satisfiedSongs.ToAutoPageSetString($"具体是以下哪一首歌呢：\n");
                if (satisfiedSongs.Count == 0) return $"找不到哪个歌叫{songName}哦...";
                song = satisfiedSongs[0];
            }

            var sourceUser = CurUser.FindThis(Context);
            var dbSong = song.FindThis(Context);
            var havenHadAliases = dbSong.Aliases?.Select(p => p.Alias).StringJoin("、");
            var success = SongAlias.Add(alias =>
            {
                alias.CorrespondingSong = dbSong;
                alias.Alias = songAnotherName;
                alias.SourceUser = sourceUser;
            }, out var added, out var error, Context);
            if (success)
            {
                Song.UpdateCache();
                var reward = RandomTool.GetInt(1, 2);
                CurUser.KouUser.GainCoinFree(reward);
                return $"学会了，{song.SongTitle}可以叫做{songAnotherName}({added.AliasID})" +
                       $"{havenHadAliases?.BeIfNotEmpty($"，我知道它还可以叫做{havenHadAliases}！")}\n" +
                       $"[{FormatGainFreeCoin(reward)}]";
            }
            return $"没学会，就突然：{error}";
        }
        [PluginFunction(ActivateKeyword = "del|delete|忘记", Name = "忘记歌曲别名", Help = "叫kou忘掉一个歌曲的别名。")]
        public string KouForgetAnotherName(
            [PluginArgument(Name = "别名ID")] List<int> ids)
        {
            if (ids.IsNullOrEmptySet()) return "这是叫我忘掉什么嘛";
            var result = new StringBuilder();
            foreach (var i in ids)
            {
                var alias = SongAlias.SingleOrDefault(a => a.AliasID == i);
                if (alias == null) result.Append($"\n不记得ID{i}");
                else if (alias.SourceUser != null && alias.SourceUser != CurUser &&
                         !CurUser.HasTheAuthority(Authority.BotManager))
                    result.Append($"\nID{i}是别人贡献的，不可以删噢");
                else
                {
                    result.Append($"\n忘记了{alias.ToString(FormatType.Brief)}");
                    alias.DeleteThis();
                    Song.UpdateCache();
                };
            }

            return result.ToString().TrimStart();
        }
        #endregion
    }
}
