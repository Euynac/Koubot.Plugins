using Koubot.SDK.Protocol.AutoModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_song")]
    [KouAutoModelTable("song", new[] { nameof(KouArcaea) }, Name = "Arcaea歌曲")]
    public partial class PluginArcaeaSong
    {
        [Key]
        [Column("song_id")]
        public int SongId { get; set; }
        [Column("song_en_id")]
        [StringLength(100)]
        public string SongEnId { get; set; }
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
        [KouAutoModelField(ActivateKeyword = "bpm", FilterSetting = FilterType.NumericInterval)]
        public string SongBpm { get; set; }
        [Column("song_bpm_base")]
        public double? SongBpmBase { get; set; }
        [Column("song_pack")]
        [StringLength(200)]
        public string SongPack { get; set; }
        [Column("song_bg_url")]
        [StringLength(255)]
        public string SongBgUrl { get; set; }
        [Column("song_length")]
        [KouAutoModelField(ActivateKeyword = "长度")]
        public TimeSpan? SongLength { get; set; }
        [Column("song_side")]
        public Side? SongSide { get; set; }
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
        [Column("chart_rating_class")]
        [KouAutoModelField(ActivateKeyword = "难度类别")]
        public RatingClass? ChartRatingClass { get; set; }
        [Column("chart_rating")]
        [StringLength(10)]
        [KouAutoModelField(ActivateKeyword = "难度")]
        public string ChartRating { get; set; }
        [Column("chart_constant")]
        [KouAutoModelField(ActivateKeyword = "定数", FilterSetting = FilterType.NumericInterval)]
        public double? ChartConstant { get; set; }
        [Column("chart_designer")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "谱师")]
        public string ChartDesigner { get; set; }
        [Column("plus_fingers", TypeName = "tinyint(1)")]
        public bool? PlusFingers { get; set; }
        [Column("jacket_designer")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "画师")]
        public string JacketDesigner { get; set; }
        [Column("jacket_url")]
        [StringLength(255)]
        public string JacketUrl { get; set; }
        [Column("jacket_override", TypeName = "tinyint(1)")]
        public bool? JacketOverride { get; set; }
        [Column("hidden_until_unlocked", TypeName = "tinyint(1)")]
        public bool? HiddenUntilUnlocked { get; set; }
        [Column("unlock_in_world_mode")]
        [StringLength(200)]
        public string UnlockInWorldMode { get; set; }
        [Column("version")]
        [StringLength(100)]
        public string Version { get; set; }
        [Column("date")]
        public int? Date { get; set; }
        [Column("remark")]
        [StringLength(2000)]
        public string Remark { get; set; }

        [InverseProperty("Song")]
        public virtual ICollection<PluginArcaeaPlayRecord> PluginArcaeaPlayRecord { get; set; }
        [InverseProperty("Song")]
        public virtual ICollection<PluginArcaeaSong2anothername> PluginArcaeaSong2anothername { get; set; }
        public PluginArcaeaSong()
        {
            PluginArcaeaPlayRecord = new HashSet<PluginArcaeaPlayRecord>();
            PluginArcaeaSong2anothername = new HashSet<PluginArcaeaSong2anothername>();
        }


    }
}
