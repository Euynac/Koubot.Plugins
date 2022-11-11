using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.Attribute;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_song_append")]
    public partial class SongAppend
    {
        [Column("chart_rating_class")]
        [AutoField(ActivateKeyword = "难度类别")]
        public Song.RatingClass ChartRatingClass { get; set; }
        [Column("song_en_id")]
        [StringLength(100)]
        public string SongEnId { get; set; }
        public virtual Song Song { get; set; }
        [Column("chart_rating")]
        [StringLength(10)]
        [AutoField(ActivateKeyword = "难度")]
        public string ChartRating { get; set; }
        [Column("chart_constant")]
        [AutoField(ActivateKeyword = "定数", FilterSetting = FilterType.NumericInterval)]
        public double? ChartConstant { get; set; }
        [Column("chart_designer")]
        [StringLength(200)]
        [AutoField(ActivateKeyword = "谱师")]
        public string ChartDesigner { get; set; }
        [AutoField(ActivateKeyword = "总键数", FilterSetting = FilterType.NumericInterval)]
        [Column("chart_all_notes")]
        public int? ChartAllNotes { get; set; }
        [Column("chart_floor_notes")]
        public int? ChartFloorNotes { get; set; }
        [Column("chart_sky_notes")]
        public int? ChartSkyNotes { get; set; }
        [Column("chart_hold_notes")]
        public int? ChartHoldNotes { get; set; }
        [Column("chart_arc_notes")]
        public int? ChartArcNotes { get; set; }
        [Column("notes_per_second")]
        public double? NotesPerSecond { get; set; }
        [Column("jacket_override", TypeName = "tinyint(1)")]
        public bool? JacketOverride { get; set; }
        [Column("jacket_override_url")]
        [StringLength(255)]
        public string JacketOverrideUrl { get; set; }
    }
}