using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using Koubot.Tool.Extensions;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Koubot.Tool.String;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_song_chart")]
    [AutoTable("song",
        new[] { nameof(KouMaimai) },
        Name = "Maimai歌曲",
        Help = "萌娘wiki + 日服数据")]
    public partial class SongChart
    {
        [Key]
        [Column("id")]
        [AutoField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
        public int ChartId { get; set; }
        [Column("official_id")]
        public int? OfficialId { get; set; }
        [AutoField(true)]
        public virtual SongInfo BasicInfo { get; set; }
        [Column("song_title_kana")]
        public string SongTitleKaNa { get; set; }
        [Column("song_type")]
        [StringLength(20)]
        [AutoField(ActivateKeyword = "类型")]
        public ChartType? SongChartType { get; set; }
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
        [AutoField(ActivateKeyword = "日期")]
        public int? Date { get; set; }

        #region 谱面数据
        [AutoField(true)]
        public List<ChartData>? ChartDataList { get; set; }
        [AutoField(true)]
        public List<ChartStatus>? ChartStatusList { get; set; }
        public class ChartStatus
        {
            public enum Tag
            {
                [KouEnumName("Very Easy","超简单","非常简单", "very easy")]
                VeryEasy,
                [KouEnumName("简单")]
                Easy,
                [KouEnumName("中等","一般")]
                Medium,
                [KouEnumName("困难","难")]
                Hard,
                [KouEnumName("Very Hard","非常困难","超困难", "very hard")]
                VeryHard,
                None,
            }
            public Tag DifficultTag { get; set; }
            /// <summary>
            /// 平均达成率
            /// </summary>
            public double AverageRate { get; set; }
            /// <summary>
            /// 记录总人数
            /// </summary>
            public int TotalCount { get; set; }
            /// <summary>
            /// 记录中，SSS以上成绩的人数
            /// </summary>
            public int SSSCount { get; set; }
            [JsonIgnore]
            public double SSSPeopleRatio => SSSCount/(double)TotalCount;
            /// <summary>
            /// 相同难度歌曲数
            /// </summary>
            public int SameDifficultCount { get; set; }
            /// <summary>
            /// 相同难度SSS比例排名
            /// </summary>
            public int SSSRankOfSameDifficult { get; set; }
            [JsonIgnore]
            public string SSSRankString => $"{SSSRankOfSameDifficult + 1}/{SameDifficultCount}";
            public override string ToString()
            {
                return
                    $"Tag：{DifficultTag}\n"+
                    $"SSS比例排名：{SSSRankString}\n" +
                       $"SSS人数：{SSSCount}/{TotalCount}({SSSPeopleRatio:P}) \n" +
                       $"平均达成率：{AverageRate:0.##}%";
            }
        }


        public class ChartData
        {
            public List<int> Notes { get; set; }
            [AutoField(ActivateKeyword = "谱师")]
            public string Charter { get; set; }
            [AutoField]
            public int Tap => Notes[0];
            [AutoField]
            public int Hold => Notes[1];
            [AutoField]
            public int Slide => Notes[2];
            [AutoField]
            public int Touch => Notes.Count == 5 ? Notes[3] : 0;
            [AutoField]
            public int Break => Notes[^1];
            public override string ToString()
            {
                if (Notes.IsNullOrEmptySet()) return "";
                return Charter.BeIfNotEmpty($"谱师：{Charter}\n") +
                       Tap.BeIfNotDefault($"TAP:{Tap}\n") +
                       Hold.BeIfNotDefault($"HOLD:{Hold}\n") +
                       Slide.BeIfNotDefault($"SLIDE:{Slide}\n") +
                       Touch.BeIfNotDefault($"TOUCH:{Touch}\n") +
                       Break.BeIfNotDefault($"BREAK:{Break}");
            }
        }

        #endregion

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
