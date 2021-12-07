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
using Koubot.Shared.Protocol.Attribute;
using KouCommand = Koubot.Shared.Protocol.KouCommand;

namespace KouGamePlugin.Maimai.Models
{
    public partial class MaiSong : KouFullAutoModel<MaiSong>
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
        [KouAutoModelField(ActivateKeyword = "旧难度")]
        public string ChartRating { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "难度")]
        public string SplashChartRating { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "旧定数")]
        public IntervalDoublePair ChartConstant { get; set; }
        [NotMapped]
        [KouAutoModelField(ActivateKeyword = "定数")]
        public IntervalDoublePair SplashChartConstant { get; set; }
        static MaiSong()
        {

            AddCustomFunc(nameof(ChartRating), (song, input) =>
            {
                if (input is string inputStr)
                {
                    return inputStr.EqualsAny(song.ChartBasicRating, song.ChartAdvancedRating,
                        song.ChartExpertRating, song.ChartMasterRating, song.ChartRemasterRating);
                }
                return false;
            });
            AddCustomFunc(nameof(SplashChartRating), (song, input) =>
            {
                if (input is string inputStr)
                {
                    return inputStr.EqualsAny(song.SplashChartBasicRating, song.SplashChartAdvancedRating,
                        song.SplashChartExpertRating, song.SplashChartMasterRating, song.SplashChartRemasterRating);
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
            AddCustomFunc(nameof(SplashChartConstant), (song, input) =>
            {
                if (input is IntervalDoublePair pair)
                {
                    return DelegateExtensions.SatisfyAny(pair.IsInInterval, song.SplashChartBasicConstant,
                        song.SplashChartAdvancedConstant, song.SplashChartExpertConstant, song.SplashChartMasterConstant,
                        song.SplashChartRemasterConstant);
                }

                return false;
            });
            AddCustomComparison(nameof(SplashChartConstant), isDesc =>
            {
                return (song, maimaiSong) => (song.SplashChartRemasterConstant ?? song.SplashChartMasterConstant).CompareToObj(
                    maimaiSong.SplashChartRemasterConstant ?? maimaiSong.SplashChartMasterConstant, isDesc);
            });
        }
        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => SongId == id;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(SongGenre), $"\n   分类：{SongGenreSplash}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongArtist), $"\n   曲师：{SongArtist}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Version), $"\n   版本：{Version}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Date), $"\n   日期：20{Date}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(SongBpm), $"\n   BPM：{SongBpm}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Remark), $"\n   注：{Remark}")}";
        }

        private string ToConstantString()
        {
            if (ChartExpertConstant == null && ChartAdvancedConstant == null) return null;
            return $"B{ChartBasicConstant}/A{ChartAdvancedConstant}/E{ChartExpertConstant}/M{ChartMasterConstant}{ChartRemasterConstant?.Be($"/R{ChartRemasterConstant}")}";
        }
        private string ToSplashConstantString()
        {
            if (SplashChartExpertConstant == null && SplashChartAdvancedConstant == null) return null;
            return $"B{SplashChartBasicConstant}/A{SplashChartAdvancedConstant}/E{SplashChartExpertConstant}/M{SplashChartMasterConstant}{SplashChartRemasterConstant?.Be($"/R{SplashChartRemasterConstant}")}";
        }

        private string ToSplashRatingString()
        {
            if (SplashChartAdvancedRating == null && SplashChartExpertRating == null) return null;
            return
                $"E{SplashChartEasyRating}/B{SplashChartBasicRating}/A{SplashChartAdvancedRating}/E{SplashChartExpertRating}/M{SplashChartMasterRating}{SplashChartRemasterRating?.Be($"/R{SplashChartRemasterRating}")}";
        }

        private string ToRatingString()
        {
            if (ChartExpertRating == null && ChartAdvancedRating == null) return null;
            return $"B{ChartBasicRating}/A{ChartAdvancedRating}/E{ChartExpertRating}/M{ChartMasterRating}{ChartRemasterRating?.Be($"/R{ChartRemasterRating}")}";
        }

        public override string ToString(FormatType format, object supplement = null, KouCommand command = null)
        {

            bool withoutConstant = SongGenreSplash == null;
            string splashAndDxData = $"{SongGenreSplash?.Be($"\n分类：{SongGenreSplash}")}" +
                                     $"{ToSplashRatingString()?.Be("\n难度：{0}", true)}" +
                                     $"{ToSplashConstantString()?.Be("\n定数：{0}", true)}" +
                                     $"{ToRatingString()?.Be("\n旧难度：{0}", true)}" +
                                     $"{ToConstantString()?.Be("\n旧定数：{0}", true)}";

            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongId}.{SongTitle}({SongType}) {(withoutConstant ? $"*[{ToSplashRatingString()}]" : $"[{ToSplashConstantString()}]")}";

                case FormatType.Detail:
                    return $"{JacketUrl?.Be(new KouImage(JacketUrl, this).ToKouResourceString())}" + //BUG 需要解决翻页可能会使得图片资源字符串裂开的问题
                           $"{SongId}.{SongTitle} [{SongType}]" +
                           splashAndDxData +
                           Version?.Be($"\n版本：{Version}") +
                           SongArtist?.Be($"\n曲师：{SongArtist}") +
                           SongBpm?.Be($"\nBPM：{SongBpm}") +
                           SongLength?.Be($"\n歌曲长度：{SongLength}") +
                           Remark?.Be($"\n注：{Remark}");

            }
            return null;
        }

        public override Action<EntityTypeBuilder<MaiSong>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.SongId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.SongId);

                entity.HasIndex(e => e.SongTitle);

                entity
                    .HasMany(e => e.Aliases)
                    .WithMany(p => p.CorrespondingSong)
                     ;
            };
        }



    }
}
