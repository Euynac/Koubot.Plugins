using Koubot.SDK.Protocol.AutoModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_songs")]
    [KouAutoModelTable("song", 
        new[] { nameof(KouArcaea) },
        Name = "Arcaea歌曲",
        Help = "当前表数据版本：3.6.1")]
    public partial class Song
    {
        [Key]
        [Column("id")]
        public int SongId { get; set; }
        [Column("en_id")]
        [StringLength(100)]
        public string SongEnId { get; set; }
        [Column("title")]
        [StringLength(500)]
        [KouAutoModelField(ActivateKeyword = "曲名", Features = AutoModelFieldFeatures.IsDefaultField)]
        public string SongTitle { get; set; }
        [Column("artist")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "曲师")]
        public string SongArtist { get; set; }
        [Column("bpm")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "bpm", FilterSetting = FilterType.NumericInterval)]
        public string SongBpm { get; set; }
        [Column("song_url")]
        public string SongUrl { get; set; }
        [Column("bpm_base")]
        public double? SongBpmBase { get; set; }
        [Column("pack")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "曲包")]
        public SongPackType? SongPack { get; set; }
        [Column("bg_url")]
        [StringLength(255)]
        public string SongBgUrl { get; set; }
        [Column("length")]
        [KouAutoModelField(ActivateKeyword = "长度")]
        public TimeSpan? SongLength { get; set; }
        [Column("side")]
        public Side? SongSide { get; set; }
        
        [Column("jacket_designer")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "画师")]
        public string JacketDesigner { get; set; }
        [Column("jacket_url")]
        [StringLength(255)]
        public string JacketUrl { get; set; }
        [Column("unlock_in_world_mode")]
        [StringLength(200)]
        public string UnlockInWorldMode { get; set; }
        [Column("version")]
        [StringLength(100)]
        public string Version { get; set; }
        [Column("date")]
        public DateTime? Date { get; set; }
        [Column("remark")]
        [StringLength(2000)]
        public string Remark { get; set; }

        [InverseProperty(nameof(PlayRecord.Song))]
        public virtual ICollection<PlayRecord> PluginArcaeaPlayRecord { get; set; }
        [KouAutoModelField(true)]
        public virtual ICollection<SongAppend> MoreInfo { get; set; }
        [KouAutoModelField(true)]
        public virtual ICollection<SongAlias> Aliases { get; set; }

        public Song()
        {
            PluginArcaeaPlayRecord = new HashSet<PlayRecord>();
        }


    }
}
