using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Math;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using KouCommand = Koubot.Shared.Protocol.KouCommand;

namespace KouGamePlugin.Maimai.Models
{
    public partial class SongChart : KouFullAutoModel<SongChart>
    {
        /// <summary>
        /// 谱面类型
        /// </summary>
        [Flags]
        public enum ChartType
        {
            None,
            DX = 1 << 0,
            SD = 1 << 1,
        }

        /// <summary>
        /// 难度类型
        /// </summary>
        public enum RatingColor
        {
            [KouEnumName("绿")]
            Basic,
            [KouEnumName("黄")]
            Advanced,
            [KouEnumName("红")]
            Expert,
            [KouEnumName("紫")]
            Master,
            [KouEnumName("白")]
            ReMaster
        }

        [NotMapped] //临时虚拟字段
        [KouAutoModelField(ActivateKeyword = "颜色", IgnoreOrIncludeWhenFrom = new[] { nameof(SongRecord) })]
        public static ThreadLocal<RatingColor> SongRatingColor { get; set; } =
            new(() => RatingColor.Master);
        [NotMapped]//桥梁字段无法支持自动排序
        [KouAutoModelField(ActivateKeyword = "难度")]
        public string ChartRating { get; set; }

        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "定数")]
        public IntervalDoublePair ChartConstant { get; set; }
        //[NotMapped]
        //[KouAutoModelField(ActivateKeyword = "旧定数")]
        //public IntervalDoublePair OldChartConstant { get; set; }
        //[NotMapped]
        //[KouAutoModelField(ActivateKeyword = "旧难度")]
        //public string OldChartRating { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "别名", Name = "曲名俗称")]
        public string SongAlias { get; set; }

        static SongChart()
        {
            AddCustomFunc(nameof(SongAlias), (song, o) =>
            {
                if (o is string input)
                {
                    return song.BasicInfo.Aliases.Any(n => n.Alias.Contains(input, StringComparison.OrdinalIgnoreCase));
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
            //AddCustomFunc(nameof(OldChartRating), (song, input) =>
            //{
            //    if (input is string inputStr)
            //    {
            //        return inputStr.EqualsAny(song.OldChartBasicRating, song.OldChartAdvancedRating,
            //            song.OldChartExpertRating, song.OldChartMasterRating, song.OldChartRemasterRating);
            //    }
            //    return false;
            //});
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
            //AddCustomFunc(nameof(OldChartConstant), (song, input) =>
            //{
            //    if (input is IntervalDoublePair pair)
            //    {
            //        return DelegateExtensions.SatisfyAny(pair.IsInInterval, song.OldChartBasicConstant,
            //            song.OldChartAdvancedConstant, song.OldChartExpertConstant, song.OldChartMasterConstant,
            //            song.OldChartRemasterConstant);
            //    }

            //    return false;
            //});
            //AddCustomComparison(nameof(OldChartConstant), isDesc =>
            //{
            //    return (song, maimaiSong) => (song.OldChartRemasterConstant ?? song.OldChartMasterConstant).CompareToObj(
            //        maimaiSong.OldChartRemasterConstant ?? maimaiSong.OldChartMasterConstant, isDesc);
            //});
        }

        public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
        {
            ruleDictionary = new Dictionary<string, string>();
            relationStr = $"{{{nameof(SongAlias)}}}||{{{nameof(BasicInfo.SongTitle)}}}";
            if (userInput.IsNullOrEmpty())
            {
                return false;
            }
            if (userInput.MatchOnceThenReplace("(白|紫|红|黄|绿)",
                    out userInput, out var groupResult2))
            {
                var color = groupResult2[1].Value;
                ruleDictionary.Add($"{nameof(SongRatingColor)}", color);
            }
            //处理限定难度类型信息
            if (userInput.MatchOnceThenReplace("[ ](dx|sd)",
                    out userInput, out var groupResult, RegexOptions.IgnoreCase | RegexOptions.RightToLeft))
            {
                var ratingClass = groupResult[1].Value;
                ruleDictionary.Add($"{nameof(SongChartType)}", ratingClass);
            }

            ruleDictionary.Add($"{nameof(BasicInfo)}.{nameof(BasicInfo.SongTitle)}", userInput);
            ruleDictionary.Add(nameof(SongAlias), userInput);
            return true;
        }

        public override int GetHashCode()
        {
            return SongTitleKaNa.GetHashCodeWith(SongChartType);
        }

        public override bool Equals(object? obj)
        {
            if (obj is SongChart another)
            {
                if (another.ChartId != 0 && ChartId != 0)
                {
                    return another.ChartId == ChartId;
                }

                return another.SongTitleKaNa == SongTitleKaNa && another.SongChartType == SongChartType;
            }
            return false;
        }

        public override bool UseItemIDToFormat() => true;
        public override int? GetItemID() => ChartId;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        protected override dynamic SetModelIncludeConfig(IQueryable<SongChart> set)
        {
            return set.Include(p => p.BasicInfo).ThenInclude(p => p.Aliases);
        }

        public override string? GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongGenreOld), $"\n   分类：{BasicInfo.SongGenre}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongArtist), $"\n   曲师：{BasicInfo.SongArtist}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Version), $"\n   版本：{BasicInfo.Version}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Date), $"\n   日期：20{Date}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongBpm), $"\n   BPM：{BasicInfo.SongBpm}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Hold), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}HOLD:{GetChartData(SongRatingColor.Value)?.Hold}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Tap), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}TAP:{GetChartData(SongRatingColor.Value)?.Tap}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Break), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}BREAK:{GetChartData(SongRatingColor.Value)?.Break}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Slide), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}SLIDE:{GetChartData(SongRatingColor.Value)?.Slide}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Touch), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}TOUCH:{GetChartData(SongRatingColor.Value)?.Touch}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Charter), $"\n   {SongRatingColor.Value.GetKouEnumFirstName()}谱师：{GetChartData(SongRatingColor.Value)?.Charter}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Remark), $"\n   注：{BasicInfo.Remark}")}";
        }

        public double? GetSpecificConstant(RatingColor type)
        {
            return type switch
            {
                RatingColor.Basic => ChartBasicConstant,
                RatingColor.Advanced => ChartAdvancedConstant,
                RatingColor.Expert => ChartExpertConstant,
                RatingColor.Master => ChartMasterConstant,
                RatingColor.ReMaster => ChartRemasterConstant,
                _ => null
            };
        }

        public string ToConstantString()
        {
            if (ChartExpertConstant == null && ChartAdvancedConstant == null) return null;
            return $"B{ChartBasicConstant?.BeIfNotDefault("{0}",true)}/A{ChartAdvancedConstant?.BeIfNotDefault("{0:F1}", true)}" +
                   $"/E{ChartExpertConstant?.BeIfNotDefault("{0:F1}", true)}" +
                   $"/M{ChartMasterConstant?.BeIfNotDefault("{0:F1}", true)}" +
                   $"{ChartRemasterConstant?.BeIfNotDefault($"/R{ChartRemasterConstant:F1}")}";
        }
        //public string ToOldConstantString()
        //{
        //    if (OldChartExpertConstant == null && OldChartAdvancedConstant == null) return null;
        //    return $"B{OldChartBasicConstant}/A{OldChartAdvancedConstant}/E{OldChartExpertConstant}/M{OldChartMasterConstant}{OldChartRemasterConstant?.Be($"/R{OldChartRemasterConstant}")}";
        //}

        //public string ToOldRatingString()
        //{
        //    if (OldChartAdvancedRating == null && OldChartExpertRating == null) return null;
        //    return
        //        $"B{OldChartBasicRating}/A{OldChartAdvancedRating}/E{OldChartExpertRating}/M{OldChartMasterRating}{OldChartRemasterRating?.Be($"/R{OldChartRemasterRating}")}";
        //}

        public string ToRatingString()
        {
            if (ChartExpertRating == null && ChartAdvancedRating == null) return null;
            return $"{ChartEasyRating?.BeIfNotEmpty("E{0}/", true)}B{ChartBasicRating?.BeIfNotEmpty("{0}", true)}" +
                   $"/A{ChartAdvancedRating?.BeIfNotEmpty("{0}", true)}/E{ChartExpertRating?.BeIfNotEmpty("{0}", true)}" +
                   $"/M{ChartMasterRating?.BeIfNotEmpty("{0}", true)}" +
                   $"{ChartRemasterRating?.BeIfNotEmpty($"/R{ChartRemasterRating}")}";
        }

        /// <summary>
        /// 获取指定难度格式化
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public string ToSpecificRatingString(RatingColor type)
        {
            return $"{BasicInfo.SongTitle}[{SongChartType}{type.GetKouEnumFirstName()}{GetSpecificConstant(type)?.Be(" {0:F1}", true)}]";
        }

        /// <summary>
        /// 计算单曲rating
        /// </summary>
        /// <param name="type"></param>
        /// <param name="achievement"></param>
        /// <returns>不存在相关定数时返回null</returns>
        public int? CalRating(RatingColor type, double achievement) =>
            GetSpecificConstant(type) is { } constant ? DxCalculator.CalSongRating(achievement, constant) : null;

        /// <summary>
        /// 获取指定难度的谱面数据
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ChartData? GetChartData(RatingColor type)
        {
            var typeInt = (int)type;
            if (!ChartDataList.IsNullOrEmptySet() && ChartDataList.Count > typeInt)
            {
                return ChartDataList[typeInt];
            }

            return null;
        }


        public override string? ToString(FormatType format, object? supplement = null, KouCommand? command = null)
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

                entity.Property(p => p.ChartDataList)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<ChartData>>(v, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }));

                entity.HasIndex(e => e.ChartId);

                entity.Property(p => p.SongChartType).HasConversion<EnumToStringConverter<ChartType>>();

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
