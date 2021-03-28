using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.Expand;
using Koubot.Tool.General;
using Koubot.Tool.Math;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Maimai.Models
{
    public partial class PluginMaimaiSong : KouAutoModel<PluginMaimaiSong>
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
        [KouAutoModelField(ActivateKeyword = "定数")]
        public IntervalDoublePair ChartConstant { get; set; }
        static PluginMaimaiSong()
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
            AddCustomFunc(nameof(ChartConstant), (song, input) =>
            {
                if (input is IntervalDoublePair pair)
                {
                    return SystemExpand.SatisfyAny(pair.IsInInterval, song.ChartBasicConstant,
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
        }
        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => SongId == id;
        public override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的歌曲";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(SongGenre), $"\n   分类：{SongGenre}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(SongArtist), $"\n   曲师：{SongArtist}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Date), $"\n   日期：20{Date}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Remark), $"\n   注：{Remark}")}";
        }

        private string ToConstantString()
        {
            if (ChartExpertConstant == null && ChartAdvancedConstant == null) return null;
            return $"B{ChartBasicConstant}/A{ChartAdvancedConstant}/E{ChartExpertConstant}/M{ChartMasterConstant}{ChartRemasterConstant?.Be($"/R{ChartRemasterConstant}")}";
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

        public override string ToString(FormatType format, object supplement = null)
        {

            bool onlySplashData = SongGenre == null;
            //string splashOrDxData = !onlySplashData ? $"\n分类：{SongGenre}\n难度：{ToRatingString()}\n定数：{ToConstantString()}" :
            //        $"{SongGenreSplash?.Be($"\nSplash分类：{SongGenreSplash}")}\nSplash难度：{ToSplashRatingString()}";
            string splashAndDxData = $"{SongGenre?.Be($"\n分类：{SongGenre}")}{ToRatingString()?.Be("\n难度：$0", true)}{ToConstantString()?.Be("\n定数：$0", true)}" +
                $"{ToSplashRatingString()?.Be("\nSplash难度：$0", true)}";

            switch (format)
            {
                case FormatType.Brief:
                    return $"{SongId}.{SongTitle}({SongType}) {(onlySplashData ? $"*[{ToSplashRatingString()}]" : $"[{ToConstantString()}]")}";

                case FormatType.Detail:
                    return $"{JacketUrl?.Be(new KouImage(JacketUrl, this).ToKouResourceString())}" + //BUG 需要解决翻页可能会使得图片资源字符串裂开的问题
                           $"{SongId}.{SongTitle} [{SongType}]" +
                           splashAndDxData +
                           SongArtist?.Be($"\n曲师：{SongArtist}") +
                           SongBpm?.Be($"\nBPM：{SongBpm}") +
                           SongLength?.Be($"\n歌曲长度：{SongLength}") +
                           Remark?.Be($"\n注：{Remark}");

            }
            return null;
        }

        public override Action<EntityTypeBuilder<PluginMaimaiSong>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.SongId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.SongId)
                    .HasName("plugin_maimai_song_index_0");

                entity.HasIndex(e => e.SongTitle)
                    .HasName("plugin_maimai_song_index_1");

                //entity.Property(e => e.Remark).IsUnicode(false);

                //entity.Property(e => e.SongArtist).IsUnicode(false);

                //entity.Property(e => e.SongBgUrl).IsUnicode(false);

                //entity.Property(e => e.SongBpm).IsUnicode(false);

                //entity.Property(e => e.SongTitle).IsUnicode(false);

                //entity.Property(e => e.Version).IsUnicode(false);
            };
        }



    }
}
