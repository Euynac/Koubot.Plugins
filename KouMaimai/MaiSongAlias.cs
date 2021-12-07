using Koubot.Shared.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.Attribute;
using System.Collections.Generic;

namespace KouGamePlugin.Maimai.Models
{
    [KouAutoModelTable("alias", new[] { nameof(KouMaimai) }, Name = "Mai歌曲别名")]
    [Table("plugin_mai_song_alias")]
    public partial class MaiSongAlias
    {
        [Key]
        [Column("alias_id")]
        public int AliasID { get; set; }
        [Column("alias")]
        [StringLength(30)]
        public string Alias { get; set; }
        [Column("song_en_id")]
        [StringLength(100)]
        public string SongKanaId { get; set; }
        /// <summary>
        /// 该alias对应的曲子
        /// </summary>
        public virtual ICollection<MaiSong> CorrespondingSong { get; set; }
        public int? SourceUserID { get; set; }
        /// <summary>
        /// 内容贡献用户
        /// </summary>
        public virtual PlatformUser SourceUser { get; set; }
    }
}
