using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Math;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using Koubot.Shared.Protocol.Attribute;
using KouCommand = Koubot.Shared.Protocol.KouCommand;

namespace KouGamePlugin.Maimai.Models
{
    public partial class SongChart : KouFullAutoModel<SongChart>
    {
        ///// <summary>
        ///// 难度类型
        ///// </summary>
        //public enum RatingType
        //{
        //    [KouEnumName("b")]
        //    Basic,
        //    [KouEnumName("a")]
        //    Advanced,
        //    [KouEnumName("e","ex")]
        //    Expert,
        //    [KouEnumName("m")]
        //    Master,
        //    [KouEnumName("r","rm")]
        //    ReMaster
        //}
        //[NotMapped]
        //[KouAutoModelField(ActivateKeyword = "难度类型")]（临时虚拟字段）
        //public RatingType SongRatingType { get; set; }
        [NotMapped]//桥梁字段无法支持自动排序
        [KouAutoModelField(ActivateKeyword = "难度")]
        public string ChartRating { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "旧难度")]
        public string OldChartRating { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "定数")]
        public IntervalDoublePair ChartConstant { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "旧定数")]
        public IntervalDoublePair OldChartConstant { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "别名", Name = "曲名俗称")]
        public string SongAlias { get; set; }

        static SongChart()
        {
            AddCustomFunc(nameof(SongAlias), (song, o) =>
            {
                if (o is string input)
                {
                    return song.BasicInfo.Aliases.Any(n => n.Alias.Contains(input));
                }
                return false;
            });
            AddCustomFunc(nameof(ChartRating), (song, input) =>
            {
                if (input is string inputStr)
                {
                    return inputStr.EqualsAny(song.ChartBasicRating, song.ChartAdvancedRating,
                        song.ChartExpertRating, song.ChartMasterRating, song.ChartRemasterRating);
                }
                return false;
            });
            AddCustomFunc(nameof(OldChartRating), (song, input) =>
            {
                if (input is string inputStr)
                {
                    return inputStr.EqualsAny(song.OldChartBasicRating, song.OldChartAdvancedRating,
                        song.OldChartExpertRating, song.OldChartMasterRating, song.OldChartRemasterRating);
                }
                return false;
            });
            AddCustomFunc(nameof(ChartConstant), (song, input) =>
            {
                if (input is IntervalDoublePair pair)
                {
                    return DelegateExtensions.SatisfyAny(pair.IsInInterval, song.ChartBasicConstant,
                        song.ChartAdvancedConstant, song.ChartExpertConstant, song.ChartMasterConstant,
                        song.ChartRemasterConstant);
                }

                return false;
            });
            AddCustomComparison(nameof(ChartConstant), isDesc =>
            {
                return (song, maimaiSong) => (song.ChartRemasterConstant ?? song.ChartMasterConstant).CompareToObj(
                    maimaiSong.ChartRemasterConstant ?? maimaiSong.ChartMasterConstant, isDesc);
            });
            AddCustomFunc(nameof(OldChartConstant), (song, input) =>
            {
                if (input is IntervalDoublePair pair)
                {
                    return DelegateExtensions.SatisfyAny(pair.IsInInterval, song.OldChartBasicConstant,
                        song.OldChartAdvancedConstant, song.OldChartExpertConstant, song.OldChartMasterConstant,
                        song.OldChartRemasterConstant);
                }

                return false;
            });
            AddCustomComparison(nameof(OldChartConstant), isDesc =>
            {
                return (song, maimaiSong) => (song.OldChartRemasterConstant ?? song.OldChartMasterConstant).CompareToObj(
                    maimaiSong.OldChartRemasterConstant ?? maimaiSong.OldChartMasterConstant, isDesc);
            });
        }

        public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
        {
            ruleDictionary = new Dictionary<string, string>();
            relationStr = $"{{{nameof(SongAlias)}}}||{{{nameof(BasicInfo.SongTitle)}}}";
            if (userInput.IsNullOrEmpty())
            {
                return false;
            }
            //处理限定难度类型信息
            if (userInput.MatchOnceThenReplace("[,，](dx|sd|标准)",
                    out userInput, out var groupResult, RegexOptions.IgnoreCase | RegexOptions.RightToLeft))
            {
                var ratingClass = groupResult[1].Value;
                ruleDictionary.Add($"{nameof(SongType)}", ratingClass);
            }
            ruleDictionary.Add($"{nameof(BasicInfo)}.{nameof(BasicInfo.SongTitle)}", userInput);
            ruleDictionary.Add(nameof(SongAlias), userInput);
            return true;
        }

        public override int GetHashCode()
        {
            return SongTitleKaNa.GetHashCodeWith(SongType);
        }

        public override bool Equals(object? obj)
        {
            if (obj is SongChart another)
            {
                if (another.ChartId != 0 && ChartId != 0)
                {
                    return another.ChartId == ChartId;
                }

                return another.SongTitleKaNa == SongTitleKaNa && another.SongType == SongType;
            }
            return false;
        }

        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => ChartId == id;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        protected override dynamic SetModelIncludeConfig(IQueryable<SongChart> set)
        {
            return set.Include(p => p.BasicInfo).ThenInclude(p => p.Aliases);
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongGenreOld), $"\n   分类：{BasicInfo.SongGenre}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongArtist), $"\n   曲师：{BasicInfo.SongArtist}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Version), $"\n   版本：{BasicInfo.Version}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Date), $"\n   日期：20{Date}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongBpm), $"\n   BPM：{BasicInfo.SongBpm}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Remark), $"\n   注：{BasicInfo.Remark}")}";
        }

        public string ToConstantString()
        {
            if (ChartExpertConstant == null && ChartAdvancedConstant == null) return null;
            return $"E{ChartEasyConstant?.ToString() ?? ChartEasyRating}/B{ChartBasicConstant}/A{ChartAdvancedConstant:F1}/E{ChartExpertConstant:F1}/M{ChartMasterConstant:F1}{ChartRemasterConstant?.Be($"/R{ChartRemasterConstant:F1}")}";
        }
        public string ToOldConstantString()
        {
            if (OldChartExpertConstant == null && OldChartAdvancedConstant == null) return null;
            return $"B{OldChartBasicConstant}/A{OldChartAdvancedConstant}/E{OldChartExpertConstant}/M{OldChartMasterConstant}{OldChartRemasterConstant?.Be($"/R{OldChartRemasterConstant}")}";
        }

        public string ToOldRatingString()
        {
            if (OldChartAdvancedRating == null && OldChartExpertRating == null) return null;
            return
                $"B{OldChartBasicRating}/A{OldChartAdvancedRating}/E{OldChartExpertRating}/M{OldChartMasterRating}{OldChartRemasterRating?.Be($"/R{OldChartRemasterRating}")}";
        }

        public string ToRatingString()
        {
            if (ChartExpertRating == null && ChartAdvancedRating == null) return null;
            return $"E{ChartEasyRating}/B{ChartBasicRating}/A{ChartAdvancedRating}/E{ChartExpertRating}/M{ChartMasterRating}{ChartRemasterRating?.Be($"/R{ChartRemasterRating}")}";
        }

        public override string ToString(FormatType format, object supplement = null, KouCommand command = null)
        {
            return format switch
            {
                FormatType.Brief => $"{BasicInfo.ToString(format, this)}",
                FormatType.Detail => $"{BasicInfo.ToString(format, this)}",
                _ => null
            };
        }

        public override Action<EntityTypeBuilder<SongChart>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.ChartId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.ChartId);

                entity
                    .HasOne(e => e.BasicInfo)
                    .WithMany(p => p.ChartInfo)
                    .IsRequired()
                    .HasForeignKey(p => p.SongTitleKaNa)
                    .HasPrincipalKey(p => p.SongTitleKaNa);
            };
        }
    }
}
