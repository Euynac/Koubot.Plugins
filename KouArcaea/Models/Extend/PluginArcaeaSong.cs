using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.Expand;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace KouGamePlugin.Arcaea.Models
{
    public partial class PluginArcaeaSong : KouAutoModel<PluginArcaeaSong>
    {
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "别名", Name = "曲名俗称")]
        public string SongAlias { get; set; }

        static PluginArcaeaSong()
        {
            AddCustomFunc(nameof(SongAlias), (song, o) =>
            {
                if (o is string input)
                {
                    return song.PluginArcaeaSong2anothername.Any(n => n.AnotherName.AnotherName.Equals(input));
                }

                return false;
            });
        }
        public override List<PluginArcaeaSong> ModelCacheIncludedList(DbSet<PluginArcaeaSong> set)
        {
            return set.Include(p => p.PluginArcaeaSong2anothername)
                .ThenInclude(p => p.AnotherName).ToList();
        }

        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => SongId == id;
        public override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(SongArtist), $"\n   曲师：{SongArtist}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(ChartDesigner), $"\n   谱师：{ChartDesigner}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(SongBpm), $"\n   BPM：{SongBpm}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(SongLength), $"\n   长度：{SongLength}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(JacketDesigner), $"\n   画师：{JacketDesigner}")}";
        }

        public override string ToString(FormatType format, object supplement = null)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongId}.{SongTitle} [{ChartRatingClass} {ChartRating}({ChartConstant})]";

                case FormatType.Detail:
                    //获取所有歌曲别名
                    string allAnotherName = "";
                    foreach (var item in PluginArcaeaSong2anothername.Where(x => x.SongId == SongId))
                    {
                        allAnotherName += item.AnotherName.AnotherName + "，";
                    }
                    allAnotherName = allAnotherName.TrimEnd('，');
                    if (allAnotherName.IsNullOrWhiteSpace()) allAnotherName = null;
                    return $"{SongId}.{SongTitle} [{ChartRatingClass} {ChartConstant}]\n" +
                        allAnotherName?.Be($"别名：{allAnotherName}\n") +
                        SongArtist?.Be($"曲师：{SongArtist}\n") +
                        JacketDesigner?.Be($"画师：{JacketDesigner}\n") +
                        SongBpm?.Be($"BPM：{SongBpm}\n") +
                        SongLength?.Be($"歌曲长度：{SongLength}\n") +
                        SongPack?.Be($"曲包：{SongPack}\n") +
                        ChartDesigner?.Be($"谱师：{ChartDesigner}\n") +
                        ChartAllNotes?.Be($"note总数：{ChartAllNotes}\n地键：{ChartFloorNotes}\n天键：{ChartSkyNotes}\n蛇：{ChartArcNotes}\n长条：{ChartHoldNotes}");
            }
            return null;
        }

        public override Action<EntityTypeBuilder<PluginArcaeaSong>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.SongId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.SongEnId)
                    .HasName("plugin_Arcaea_song_index_1");

                entity.HasIndex(e => e.SongId)
                    .HasName("plugin_Arcaea_song_index_0");

                entity.HasIndex(e => e.SongTitle)
                    .HasName("plugin_Arcaea_song_index_2");

                entity.Property(e => e.ChartDesigner).IsUnicode(false);

                entity.Property(e => e.ChartRating).IsUnicode(false);

                entity.Property(e => e.JacketDesigner).IsUnicode(false);

                entity.Property(e => e.JacketUrl).IsUnicode(false);

                entity.Property(e => e.Remark).IsUnicode(false);

                entity.Property(e => e.SongArtist).IsUnicode(false);

                entity.Property(e => e.SongBgUrl).IsUnicode(false);

                entity.Property(e => e.SongBpm).IsUnicode(false);

                entity.Property(e => e.SongEnId).IsUnicode(false);

                entity.Property(e => e.SongPack)
                    .IsUnicode(false)
                    .HasComment("songs collection");

                entity.Property(e => e.SongTitle).IsUnicode(false);

                entity.Property(e => e.UnlockInWorldMode).IsUnicode(false);

                entity.Property(e => e.Version).IsUnicode(false);
            };
        }

        /// <summary>
        /// 难度别名
        /// </summary>
        public static Dictionary<string, RatingClass> RatingClassNameList = new Dictionary<string, RatingClass>()
        {
            { "pst", RatingClass.Past},
            { "prs", RatingClass.Present},
            { "ftr", RatingClass.Future},
            { "byd", RatingClass.Beyond },
            { "byn", RatingClass.Beyond },
            { "all", RatingClass.Random }
        };
        /// <summary>
        /// 歌曲阵营
        /// </summary>
        public enum Side
        {
            Light,
            Conflict
        }
        /// <summary>
        /// 歌曲难度类型
        /// </summary>
        public enum RatingClass
        {
            [KouEnumName("pst")]
            Past,
            [KouEnumName("prs")]
            Present,
            [KouEnumName("ftr")]
            Future,
            [KouEnumName("byd", "byn")]
            Beyond,
            [KouEnumName("all")]
            Random
        }

        /// <summary>
        /// 谱面难度过滤器
        /// </summary>
        public static readonly Func<PluginArcaeaSong, List<string>, bool> RatingNumFilter = (song, ratingNumList) =>
            ratingNumList.IsNullOrEmptySet() || ratingNumList.Contains(song.ChartRating);
    }
}
