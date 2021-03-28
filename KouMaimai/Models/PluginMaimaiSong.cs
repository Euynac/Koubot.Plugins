using Koubot.SDK.Protocol.AutoModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_song")]
    [KouAutoModelTable("song", new[] { nameof(KouMaimai) }, Name = "Maimai歌曲", Help = "当前表版本1.0.1 C + 萌娘wiki + 日服数据")]
    public partial class PluginMaimaiSong
    {
        [Key]
        [Column("song_id")]
        [KouAutoModelField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
        public int SongId { get; set; }

        [Column("song_title")]
        [StringLength(500)]
        [KouAutoModelField(ActivateKeyword = "曲名")]
        public string SongTitle { get; set; }
        [Column("song_artist")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "曲师")]
        public string SongArtist { get; set; }
        [Column("song_bpm")]
        [StringLength(20)]
        public string SongBpm { get; set; }

        [Column("song_genre")]
        [KouAutoModelField(ActivateKeyword = "分类")]
        public string SongGenre { get; set; }
        [Column("song_type")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "类型")]
        public string SongType { get; set; }
        [Column("jacket_url")]
        [StringLength(255)]
        public string JacketUrl { get; set; }
        [Column("song_length")]
        public TimeSpan? SongLength { get; set; }

        [Column("chart_basic_rating")]
        public string ChartBasicRating { get; set; }
        [Column("chart_basic_constant")]
        public double? ChartBasicConstant { get; set; }
        [Column("chart_advanced_rating")]
        public string ChartAdvancedRating { get; set; }
        [Column("chart_advanced_constant")]
        public double? ChartAdvancedConstant { get; set; }
        [Column("chart_expert_rating")]
        public string ChartExpertRating { get; set; }
        [Column("chart_expert_constant")]
        public double? ChartExpertConstant { get; set; }
        [Column("chart_master_rating")]
        public string ChartMasterRating { get; set; }
        [Column("chart_master_constant")]
        public double? ChartMasterConstant { get; set; }
        [Column("chart_remaster_rating")]
        public string ChartRemasterRating { get; set; }
        [Column("chart_remaster_constant")]
        public double? ChartRemasterConstant { get; set; }

        [Column("version")]
        public string Version { get; set; }
        [Column("date")]
        [KouAutoModelField(ActivateKeyword = "日期")]
        public int? Date { get; set; }
        [Column("remark")]
        [StringLength(2000)]
        [KouAutoModelField(ActivateKeyword = "注|评论")]
        public string Remark { get; set; }

        #region Splash版本 （暂时隐藏）

        [Column("splash_chart_easy_rating")]
        public string SplashChartEasyRating { get; set; }
        [Column("splash_chart_easy_constant")]
        public double? SplashChartEasyConstant { get; set; }
        [Column("splash_chart_basic_rating")]
        public string SplashChartBasicRating { get; set; }
        [Column("splash_chart_basic_constant")]
        public double? SplashChartBasicConstant { get; set; }
        [Column("splash_chart_advanced_rating")]
        public string SplashChartAdvancedRating { get; set; }
        [Column("splash_chart_advanced_constant")]
        public double? SplashChartAdvancedConstant { get; set; }
        [Column("splash_chart_expert_rating")]
        public string SplashChartExpertRating { get; set; }
        [Column("splash_chart_expert_constant")]
        public double? SplashChartExpertConstant { get; set; }
        [Column("splash_chart_master_rating")]
        public string SplashChartMasterRating { get; set; }
        [Column("splash_chart_master_constant")]
        public double? SplashChartMasterConstant { get; set; }
        [Column("splash_chart_remaster_rating")]
        public string SplashChartRemasterRating { get; set; }
        [Column("splash_chart_remaster_constant")]
        public double? SplashChartRemasterConstant { get; set; }
        [Column("song_genre_splash")]
        public string SongGenreSplash { get; set; }
        #endregion
    }
}
