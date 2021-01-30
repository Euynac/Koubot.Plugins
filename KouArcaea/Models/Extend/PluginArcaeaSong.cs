using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.Expand;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KouGamePlugin.Arcaea.Models
{
    public partial class PluginArcaeaSong : KouAutoModel<PluginArcaeaSong>
    {

        public override string ToString(FormatType format)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongTitle} [{ChartRatingClass} {ChartRating}({ChartConstant})]";

                case FormatType.Detail:
                    //获取所有歌曲别名
                    string allAnotherName = "";
                    foreach (var item in PluginArcaeaSong2anothername.Where(x => x.SongId == SongId))
                    {
                        allAnotherName += item.AnotherName.AnotherName + "，";
                    }
                    allAnotherName = allAnotherName.TrimEnd('，');
                    if (allAnotherName.IsNullOrWhiteSpace()) allAnotherName = null;
                    return $"{SongTitle} [{ChartRatingClass} {ChartConstant}]\n" +
                        allAnotherName.BeNullOr($"别名：{allAnotherName}\n") +
                        SongArtist.BeNullOr($"曲师：{SongArtist}\n") +
                        JacketDesigner.BeNullOr($"画师：{JacketDesigner}\n") +
                        SongBpm.BeNullOr($"BPM：{SongBpm}\n") +
                        SongLength.BeNullOr($"歌曲长度：{SongLength}\n") +
                        SongPack.BeNullOr($"曲包：{SongPack}\n") +
                        ChartDesigner.BeNullOr($"谱师：{ChartDesigner}\n") +
                        ChartAllNotes.BeNullOr($"note总数：{ChartAllNotes}\n地键：{ChartFloorNotes}\n天键：{ChartSkyNotes}\n蛇：{ChartArcNotes}\n长条：{ChartHoldNotes}");
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
            Past,
            Present,
            Future,
            Beyond,
            Random
        }

        /// <summary>
        /// 谱面难度过滤器
        /// </summary>
        public static readonly Func<PluginArcaeaSong, List<string>, bool> RatingNumFilter = (song, ratingNumList) =>
            ratingNumList.IsNullOrEmptySet() || ratingNumList.Contains(song.ChartRating);
    }
}
