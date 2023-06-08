using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_song_basic_info")]
    public partial class SongInfo
    {
        [Key]
        [Column("song_id")]
        public int SongId { get; set; }
        [Column("song_title_kana")]
        public string SongTitleKaNa { get; set; }
        [AutoField(true)]
        public virtual ICollection<SongAlias> Aliases { get; set; }
        public virtual ICollection<SongChart> ChartInfo { get; set; }
        [Column("song_title")]
        [StringLength(500)]
        [AutoField(ActivateKeyword = "曲名", Features = AutoModelFieldFeatures.IsDefaultField)]
        public string SongTitle { get; set; }
        [Column("song_artist")]
        [StringLength(200)]
        [AutoField(ActivateKeyword = "曲师")]
        public string SongArtist { get; set; }
        [Column("song_bpm")]
        [StringLength(20)]
        [AutoField(ActivateKeyword = "bpm")]
        public string SongBpm { get; set; }
        [Column("jacket_url")]
        [StringLength(255)]
        public string JacketUrl { get; set; }
        [Column("song_length")]
        public TimeSpan? SongLength { get; set; }
        [AutoField(ActivateKeyword = "版本")]
        [Column("version")]
        public SongVersion? Version { get; set; }
        [Column("dx_version")]
        public SongVersion? DxVersion { get; set; }
        [Column("remark")]
        [StringLength(2000)]
        [AutoField(ActivateKeyword = "注|评论")]
        public string Remark { get; set; }
        [AutoField(ActivateKeyword = "分类")]
        [Column("song_genre")]
        public string SongGenre { get; set; }
        [Column("song_genre_old")]
        [AutoField(ActivateKeyword = "旧分类")]
        public string SongGenreOld { get; set; }
        /// <summary>
        /// 判断是否是新曲，用于计算DX b15
        /// </summary>
        [AutoField(ActivateKeyword = "新曲")]
        [Column("is_new")]
        public bool? IsNew { get; set; }
    }
}
