using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Maths;
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
        [AutoField(ActivateKeyword = "颜色", IgnoreOrIncludeWhenFrom = new[] { nameof(SongRecord) })]
        public static ThreadLocal<RatingColor> SongRatingColor { get; set; } =
            new(() => RatingColor.Master);
        [NotMapped]//桥梁字段无法支持自动排序
        [AutoField(ActivateKeyword = "难度")]
        public string ChartRating { get; set; }

        [NotMapped]
        [AutoField(ActivateKeyword = "定数")]
        public IntervalDoublePair ChartConstant { get; set; }
        //[NotMapped]
        //[KouAutoModelField(ActivateKeyword = "旧定数")]
        //public IntervalDoublePair OldChartConstant { get; set; }
        //[NotMapped]
        //[KouAutoModelField(ActivateKeyword = "旧难度")]
        //public string OldChartRating { get; set; }
        [NotMapped]
        [AutoField(ActivateKeyword = "别名", Name = "曲名俗称")]
        public string SongAlias { get; set; }

        [AutoField(ActivateKeyword = "tag", Name = "难易度标签")]
        public ChartStatus.Tag? DifficultTag =>
            ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.DifficultTag;

        /// <summary>
        /// 平均达成率
        /// </summary>
        [AutoField(ActivateKeyword = "平均达成率")]
        public double? AverageRate => ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.AverageRate;
        [AutoField(ActivateKeyword = "鸟比例")]
        public double? SSSPeopleRatio => ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSPeopleRatio;

        /// <summary>
        /// 相同难度SSS比例排名
        /// </summary>
        [AutoField(ActivateKeyword = "同难度排名|rank")]
        public int? SSSRankOfSameDifficult =>
            ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSRankOfSameDifficult;

        public static string GetCssColorClass(RatingColor color)
        {
            return color switch
            {
                RatingColor.Basic => "basic_color",
                RatingColor.Advanced => "advanced_color",
                RatingColor.Expert => "expert_color",
                RatingColor.Master => "master_color",
                RatingColor.ReMaster => "remaster_color",
                _ => ""
            };
        }
        static SongChart()
        {
            //AddCustomFunc(nameof(ChartStatus.DifficultTag), (song, o) =>
            //    song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data
            //    && BaseCompare(data.DifficultTag, o));
            //AddCustomFunc(nameof(ChartStatus.AverageRate), (song, o) =>
            //    song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data
            //    && BaseCompare(data.AverageRate, o));
            //AddCustomFunc(nameof(ChartStatus.SSSRankOfSameDifficult), (song, o) =>
            //    song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data
            //    && BaseCompare(data.SSSRankOfSameDifficult, o));
            //AddCustomFunc(nameof(ChartStatus.SSSPeopleRatio), (song, o) =>
            //    song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data
            //    && BaseCompare(data.SSSPeopleRatio, o));
            //AddCustomComparison(nameof(ChartStatus.DifficultTag), isDesc => (song, maimaiSong) =>
            //    (song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.DifficultTag).CompareToObj(
            //        maimaiSong.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.DifficultTag, isDesc));
            //AddCustomComparison(nameof(ChartStatus.AverageRate), isDesc => (song, maimaiSong) =>
            //    (song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.AverageRate).CompareToObj(
            //        maimaiSong.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.AverageRate, isDesc));
            //AddCustomComparison(nameof(ChartStatus.SSSRankOfSameDifficult), isDesc => (song, maimaiSong) =>
            //    (song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSRankOfSameDifficult).CompareToObj(
            //        maimaiSong.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSRankOfSameDifficult, isDesc));
            //AddCustomComparison(nameof(ChartStatus.SSSPeopleRatio), isDesc => (song, maimaiSong) =>
            //    (song.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSPeopleRatio).CompareToObj(
            //        maimaiSong.ChartStatusList?.ElementAtOrDefault((int)SongRatingColor.Value)?.SSSPeopleRatio, isDesc));


            AddCustomFunc(nameof(ChartData.Break),
                (song, o) => song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                             BaseCompare(data.Break, o));
            AddCustomFunc(nameof(ChartData.Hold), (song, o) =>
                song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                BaseCompare(data.Hold, o));
            AddCustomFunc(nameof(ChartData.Slide),
                (song, o) => song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                             BaseCompare(data.Slide, o));
            AddCustomFunc(nameof(ChartData.Tap), (song, o) =>
                song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                BaseCompare(data.Tap, o));
            AddCustomFunc(nameof(ChartData.Touch), (song, o) =>
                song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                BaseCompare(data.Touch, o));
            AddCustomFunc(nameof(ChartData.Charter), (song, o) =>
                song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value) is { } data &&
                BaseCompare(data.Charter, o));
            AddCustomComparison(nameof(ChartData.Touch), isDesc => (song, maimaiSong) =>
                (song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Touch).CompareToObj(
                    maimaiSong.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Touch, isDesc));
            AddCustomComparison(nameof(ChartData.Break), isDesc => (song, maimaiSong) =>
                (song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Break).CompareToObj(
                    maimaiSong.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Break, isDesc));
            AddCustomComparison(nameof(ChartData.Hold), isDesc => (song, maimaiSong) =>
                (song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Hold).CompareToObj(
                    maimaiSong.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Hold, isDesc));
            AddCustomComparison(nameof(ChartData.Slide), isDesc => (song, maimaiSong) =>
                (song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Slide).CompareToObj(
                    maimaiSong.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Slide, isDesc));
            AddCustomComparison(nameof(ChartData.Tap), isDesc => (song, maimaiSong) =>
                (song.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Tap).CompareToObj(
                    maimaiSong.ChartDataList?.ElementAtOrDefault((int)SongRatingColor.Value)?.Tap, isDesc));
            AddCustomFunc(nameof(SongAlias), (song, o) => o.ConvertedValue is string input &&
                                                          song.BasicInfo.Aliases.Any(n =>
                                                              n.Alias.Contains(input,
                                                                  StringComparison.OrdinalIgnoreCase)));
            AddCustomFunc(nameof(ChartRating), (song, input) => input.ConvertedValue is string inputStr &&
                                                                inputStr.EqualsAny(song.ChartBasicRating,
                                                                    song.ChartAdvancedRating,
                                                                    song.ChartExpertRating, song.ChartMasterRating,
                                                                    song.ChartRemasterRating));

            //AddCustomFunc(nameof(OldChartRating), (song, input) =>
            //{
            //    if (input is string inputStr)
            //    {
            //        return inputStr.EqualsAny(song.OldChartBasicRating, song.OldChartAdvancedRating,
            //            song.OldChartExpertRating, song.OldChartMasterRating, song.OldChartRemasterRating);
            //    }
            //    return false;
            //});
            AddCustomFunc(nameof(ChartConstant), (song, input) => input.ConvertedValue is IntervalDoublePair pair &&
                                                                  DelegateExtensions.SatisfyAny(pair.IsInInterval,
                                                                      song.ChartBasicConstant,
                                                                      song.ChartAdvancedConstant,
                                                                      song.ChartExpertConstant,
                                                                      song.ChartMasterConstant,
                                                                      song.ChartRemasterConstant));
            AddCustomComparison(nameof(ChartConstant), isDesc => (song, maimaiSong) =>
                (song.ChartRemasterConstant ?? song.ChartMasterConstant).CompareToObj(
                    maimaiSong.ChartRemasterConstant ?? maimaiSong.ChartMasterConstant, isDesc));
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
      
        public double? GetChartConstantOfSpecificColor(RatingColor color)
        {
            return color switch
            {
                RatingColor.Basic => ChartBasicConstant,
                RatingColor.Advanced => ChartAdvancedConstant,
                RatingColor.Expert => ChartExpertConstant,
                RatingColor.Master => ChartMasterConstant,
                RatingColor.ReMaster => ChartRemasterConstant,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }

        public string GetChartRatingOfSpecificColor(RatingColor color)
        {
            return color switch
            {
                RatingColor.Basic => ChartBasicRating,
                RatingColor.Advanced => ChartAdvancedRating,
                RatingColor.Expert => ChartExpertRating,
                RatingColor.Master => ChartMasterRating,
                RatingColor.ReMaster => ChartRemasterRating,
                _ => throw new ArgumentOutOfRangeException(nameof(color), color, null)
            };
        }
        public override int GetHashCode() => HashCode.Combine(SongTitleKaNa,SongChartType);

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

        public override FormatConfig ConfigFormat() => new() {UseItemIdToFormat = true};
        public override int? GetItemID() => ChartId;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        protected override dynamic ConfigModelInclude(IQueryable<SongChart> set)
        {
            return set.Include(p => p.BasicInfo).ThenInclude(p => p.Aliases);
        }

        public override string? GetAutoCitedSupplement(HashSet<string> citedFieldNames)
        {
            return
                $"{citedFieldNames.BeIfContains(nameof(ChartStatus.DifficultTag), $"\n   {SongRatingColor.Value.GetKouEnumName()}标签：{GetChartStatus(SongRatingColor.Value)?.DifficultTag}")}" +
                $"{citedFieldNames.BeIfContains(nameof(ChartStatus.AverageRate), $"\n   {SongRatingColor.Value.GetKouEnumName()}平均达成率：{GetChartStatus(SongRatingColor.Value)?.AverageRate:0.##}%")}" +
                $"{citedFieldNames.BeIfContains(nameof(ChartStatus.SSSRankOfSameDifficult), $"\n   {SongRatingColor.Value.GetKouEnumName()}同难度排名：{GetChartStatus(SongRatingColor.Value)?.SSSRankString}")}" +
                $"{citedFieldNames.BeIfContains(nameof(ChartStatus.SSSPeopleRatio), $"\n   {SongRatingColor.Value.GetKouEnumName()}SSS人数比例：{GetChartStatus(SongRatingColor.Value)?.SSSPeopleRatio:P}")}" +
                $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongGenreOld), $"\n   分类：{BasicInfo.SongGenre}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongArtist), $"\n   曲师：{BasicInfo.SongArtist}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Version), $"\n   版本：{BasicInfo.Version}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Date), $"\n   日期：20{Date}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.SongBpm), $"\n   BPM：{BasicInfo.SongBpm}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Hold), $"\n   {SongRatingColor.Value.GetKouEnumName()}HOLD:{GetChartData(SongRatingColor.Value)?.Hold}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Tap), $"\n   {SongRatingColor.Value.GetKouEnumName()}TAP:{GetChartData(SongRatingColor.Value)?.Tap}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Break), $"\n   {SongRatingColor.Value.GetKouEnumName()}BREAK:{GetChartData(SongRatingColor.Value)?.Break}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Slide), $"\n   {SongRatingColor.Value.GetKouEnumName()}SLIDE:{GetChartData(SongRatingColor.Value)?.Slide}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Touch), $"\n   {SongRatingColor.Value.GetKouEnumName()}TOUCH:{GetChartData(SongRatingColor.Value)?.Touch}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(ChartData.Charter), $"\n   {SongRatingColor.Value.GetKouEnumName()}谱师：{GetChartData(SongRatingColor.Value)?.Charter}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(BasicInfo.Remark), $"\n   注：{BasicInfo.Remark}")}";
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
        /// <param name="color"></param>
        /// <returns></returns>
        public string ToSpecificRatingString(RatingColor color)
        {
            return $"{BasicInfo.SongTitle}[{SongChartType}{color.GetKouEnumName()}{GetChartConstantOfSpecificColor(color)?.Be(" {0:F1}", true)}{GetChartStatus(color)?.DifficultTag.Be(" {0}",true)}]";
        }

        /// <summary>
        /// 计算单曲rating
        /// </summary>
        /// <param name="type"></param>
        /// <param name="achievement"></param>
        /// <returns>不存在相关定数时返回null</returns>
        public int? CalRating(RatingColor type, double achievement) =>
            GetChartConstantOfSpecificColor(type) is { } constant ? DxCalculator.CalSongRating(achievement, constant) : null;

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

        /// <summary>
        /// 获取指定难度的谱面状态
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public ChartStatus? GetChartStatus(RatingColor type)
        {
            var typeInt = (int)type;
            if (!ChartStatusList.IsNullOrEmptySet() && ChartStatusList.Count > typeInt)
            {
                return ChartStatusList[typeInt];
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
                entity.Property(p=>p.ChartStatusList)
                    .HasConversion( v=> JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                v => JsonSerializer.Deserialize<List<ChartStatus>>(v, new JsonSerializerOptions
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
