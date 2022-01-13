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
        [KouAutoModelField(true)]
        public virtual ICollection<SongAlias> Aliases { get; set; }
        public virtual ICollection<SongChart> ChartInfo { get; set; }
        [Column("song_title")]
        [StringLength(500)]
        [KouAutoModelField(ActivateKeyword = "曲名", Features = AutoModelFieldFeatures.IsDefaultField)]
        public string SongTitle { get; set; }
        [Column("song_artist")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "曲师")]
        public string SongArtist { get; set; }
        [Column("song_bpm")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "bpm")]
        public string SongBpm { get; set; }
        [Column("jacket_url")]
        [StringLength(255)]
        public string JacketUrl { get; set; }
        [Column("song_length")]
        public TimeSpan? SongLength { get; set; }
        [KouAutoModelField(ActivateKeyword = "版本")]
        [Column("version")]
        public SongVersion? Version { get; set; }
        [Column("remark")]
        [StringLength(2000)]
        [KouAutoModelField(ActivateKeyword = "注|评论")]
        public string Remark { get; set; }
        [KouAutoModelField(ActivateKeyword = "分类")]
        [Column("song_genre")]
        public string SongGenre { get; set; }
        [Column("song_genre_old")]
        [KouAutoModelField(ActivateKeyword = "旧分类")]
        public string SongGenreOld { get; set; }
        /// <summary>
        /// 判断是否是新曲，用于计算DX b15
        /// </summary>
        [KouAutoModelField(ActivateKeyword = "新DX")]
        [Column("is_new")]
        public bool? IsNew { get; set; }
    }
}
