using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Koubot.SDK.Models.Entities;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Microsoft.EntityFrameworkCore.Query;

namespace KouGamePlugin.Arcaea.Models
{
    public partial class Song : KouFullAutoModel<Song>
    {
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "别名", Name = "曲名俗称")]
        public string SongAlias { get; set; }

        public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
        {
            ruleDictionary = new Dictionary<string, string>();
            relationStr = $"{{{nameof(SongAlias)}}}||{{{nameof(SongTitle)}}}";
            if (userInput.IsNullOrEmpty())
            {
                return false;
            }
            //处理限定难度类型信息
            if (userInput.MatchOnceThenReplace("[,，](ftr|pst|prs|byd|byn|future|past|present|beyond|all)",
                out userInput, out var groupResult, RegexOptions.IgnoreCase | RegexOptions.RightToLeft))
            {
                var ratingClass = groupResult[1].Value;
                ruleDictionary.Add(nameof(SongAppend.ChartRatingClass), ratingClass);
            }
            ruleDictionary.Add(nameof(SongTitle), userInput);
            ruleDictionary.Add(nameof(SongAlias), userInput);
            return true;
        }

        static Song()
        {
            AddCustomFunc(nameof(SongAlias), (song, o) =>
            {
                if (o is string input)
                {
                    return song.Aliases.Any(n => n.Alias.Contains(input));
                }
                return false;
            });
        }

        protected override dynamic ModelCacheIncludeConfig(IQueryable<Song> set)
        {
            return set.Include(p=>p.MoreInfo)
                .Include(p => p.Aliases)
                .ThenInclude(p=>p.SourceUser);
        }

        public override int GetHashCode() => SongId.GetHashCodeWith(SongEnId);

        public override bool Equals(object? obj)
        {
            if (obj is Song song)
            {
                return this.TryEqual(SongId, song.SongId)
                       || SongEnId == song.SongEnId;
            }

            return false;
        }

        public override bool IsAutoItemIDEnabled() => true;


        public override bool IsTheItemID(int id) => SongId == id;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(SongArtist), $"\n   曲师：{SongArtist}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongBpm), $"\n   BPM：{SongBpm}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongPack), $"\n   曲包：{SongPack.GetKouEnumFirstName()}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongLength), $"\n   长度：{SongLength}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongAppend.ChartDesigner), $"\n   谱师：{GetBriefChartDesigner()}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongAppend.ChartAllNotes), $"\n   总键数：{GetBriefAllNotes()}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(JacketDesigner), $"\n   画师：{JacketDesigner}")}";
        }

        private string GetBriefChartDesigner() =>
            MoreInfo?.Select(p => p.ChartDesigner).Distinct().ToStringJoin('/').TrimEnd('/');
        private string GetBriefConstant() => 
            MoreInfo?.OrderBy(p => p.ChartRatingClass).Select(p => p.ChartConstant).ToStringJoin('/').TrimEnd('/');
        private string GetBriefAllNotes() =>
            MoreInfo?.OrderBy(p => p.ChartRatingClass).Select(p => p.ChartAllNotes).ToStringJoin('/').TrimEnd('/');
        public override string ToString(FormatType format, object supplement = null)
        {
            string constantDesc = GetBriefConstant();
            string designerDesc = GetBriefChartDesigner();
            string allNotesDesc = GetBriefAllNotes();
            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongId}.{SongTitle}{constantDesc?.Be($" [{constantDesc}]")}";

                case FormatType.Detail:
                    return $"{JacketUrl?.Be(new KouImage(JacketUrl, this).ToKouResourceString())}" +
                           $"{SongId}.{SongTitle}\n" +
                           constantDesc?.Be($"定数：{constantDesc}\n") +
                           Aliases?.Be($"别名：{Aliases.Select(p => p.Alias).ToStringJoin('，')}\n") +
                           SongArtist?.Be($"曲师：{SongArtist}\n") +
                           designerDesc?.Be($"谱师：{designerDesc}\n")+
                           JacketDesigner?.Be($"画师：{JacketDesigner}\n") +
                           SongBpm?.Be($"BPM：{SongBpm}\n") +
                           SongLength?.Be($"歌曲长度：{SongLength}\n") +
                           SongPack?.Be($"曲包：{SongPack.GetKouEnumFirstName().ToTitleCase()}\n") +
                           allNotesDesc?.Be($"总键数：{allNotesDesc}\n");
            }
            return null;
        }

        public override Action<EntityTypeBuilder<Song>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.SongId);

                entity.HasIndex(e => e.SongEnId);

                entity.HasIndex(e => e.SongId);

                entity.HasIndex(e => e.SongTitle);

                entity.Property(p => p.SongPack).HasConversion<string>();

                entity
                    .HasMany(e => e.Aliases)
                    .WithOne(p=>p.CorrespondingSong)
                    .HasForeignKey(p=>p.SongEnId)
                    .HasPrincipalKey(p=>p.SongEnId);
                entity.HasMany(p => p.MoreInfo)
                    .WithOne(p => p.Song)
                    .HasForeignKey(p => p.SongEnId)
                    .HasPrincipalKey(p => p.SongEnId);
            };
        }

        /// <summary>
        /// 歌曲阵营
        /// </summary>
        public enum Side
        {
            [KouEnumName("光","光侧","白")]
            Light,
            [KouEnumName("对立","对立侧","黑")]
            Conflict
        }
        
        /// <summary>
        /// 曲包
        /// </summary>
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        public enum SongPackType
        {
            [KouEnumName("Arcaea","arc")]
            Base,
            [KouEnumName("World Extend","扩展")]
            Extend,
            [KouEnumName("Black Fate")]
            Vs,
            [KouEnumName("Adverse Prelude")]
            Prelude,
            [KouEnumName("Luminous Sky")]
            Rei,
            [KouEnumName("Vicious Labyrinth")]
            Yugamu,
            [KouEnumName("Eternal Core")]
            Core,
            [KouEnumName("Esoteric Order")]
            Observer,
            [KouEnumName("Pale Tapestry")]
            Observer_append_1,
            [KouEnumName("Ephemeral Page")]
            Alice,
            [KouEnumName("The Journey Onwards")]
            Alice_append_1,
            [KouEnumName("Sunset Radiance")]
            Omatsuri,
            [KouEnumName("Absolute Reason")]
            Zettai,
            [KouEnumName("Binary Enfold")]
            Nijuusei,
            [KouEnumName("Ambivalent Vision","av")]
            Mirai,
            [KouEnumName("Crimson Solace","cs")]
            Shiawase,
            [KouEnumName("maimai")]
            Maimai,
            [KouEnumName("O.N.G.E.K.I.")]
            Ongeki,
            [KouEnumName("CHUNITHM","中二")]
            Chunithm,
            [KouEnumName("Collaboration Chapter 2")]
            Chunithm_append_1,
            [KouEnumName("Groove Coaster","gc")]
            Groovecoaster,
            [KouEnumName("Tone Sphere","ts")]
            Tonesphere,
            [KouEnumName("Lanota","la")]
            Lanota,
            [KouEnumName("Dynamix","dy")]
            Dynamix,
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
    }
}
