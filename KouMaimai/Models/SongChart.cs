using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.SDK.AutoModel;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_song_chart")]
    [KouAutoModelTable("song",
        new[] { nameof(KouMaimai) },
        Name = "Maimai歌曲",
        Help = "萌娘wiki + 日服数据")]
    public partial class SongChart
    {
        [Key]
        [Column("id")]
        [KouAutoModelField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
        public int ChartId { get; set; }
        [KouAutoModelField(true)]
        public virtual SongInfo BasicInfo { get; set; }
        [Column("song_title_kana")]
        public string SongTitleKaNa { get; set; }
        [Column("song_type")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "类型")]
        public string SongType { get; set; }
        [StringLength(10)]
        [Column("chart_easy_rating")]
        public string ChartEasyRating { get; set; }
        [Column("chart_easy_constant")]
        public double? ChartEasyConstant { get; set; }
        [StringLength(10)]
        [Column("chart_basic_rating")]
        public string ChartBasicRating { get; set; }
        [Column("chart_basic_constant")]
        public double? ChartBasicConstant { get; set; }
        [StringLength(10)]
        [Column("chart_advanced_rating")]
        public string ChartAdvancedRating { get; set; }
        [Column("chart_advanced_constant")]
        public double? ChartAdvancedConstant { get; set; }
        [StringLength(10)]
        [Column("chart_expert_rating")]
        public string ChartExpertRating { get; set; }
        [Column("chart_expert_constant")]
        public double? ChartExpertConstant { get; set; }
        [Column("chart_master_rating")]
        [StringLength(10)]
        public string ChartMasterRating { get; set; }
        [Column("chart_master_constant")]
        public double? ChartMasterConstant { get; set; }
        [Column("chart_remaster_rating")]
        [StringLength(10)]
        public string ChartRemasterRating { get; set; }
        [Column("chart_remaster_constant")]
        public double? ChartRemasterConstant { get; set; }
        [Column("date")]
        [KouAutoModelField(ActivateKeyword = "日期")]
        public int? Date { get; set; }
        #region 旧版本谱面难度
        [Column("old_chart_basic_rating")]
        [StringLength(10)]
        public string OldChartBasicRating { get; set; }
        [Column("old_chart_basic_constant")]
        public double? OldChartBasicConstant { get; set; }
        [StringLength(10)]
        [Column("old_chart_advanced_rating")]
        public string OldChartAdvancedRating { get; set; }
        [Column("old_chart_advanced_constant")]
        public double? OldChartAdvancedConstant { get; set; }
        [StringLength(10)]
        [Column("old_chart_expert_rating")]
        public string OldChartExpertRating { get; set; }
        [Column("old_chart_expert_constant")]
        public double? OldChartExpertConstant { get; set; }
        [StringLength(10)]
        [Column("old_chart_master_rating")]
        public string OldChartMasterRating { get; set; }
        [Column("old_chart_master_constant")]
        public double? OldChartMasterConstant { get; set; }
        [StringLength(10)]
        [Column("old_chart_remaster_rating")]
        public string OldChartRemasterRating { get; set; }
        [Column("old_chart_remaster_constant")]
        public double? OldChartRemasterConstant { get; set; }
        #endregion
    }
}
