using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol.AutoModel;

namespace KouGamePlugin.Arcaea.Models
{
    [KouAutoModelTable("alias", new[] { nameof(KouArcaea) }, Name = "Arcaea歌曲别名")]
    [Table("plugin_arcaea_song_alias")]
    public partial class SongAlias
    {
        [Key]
        [Column("alias_id")]
        public int AliasID { get; set; }
        [Column("alias")]
        [StringLength(30)]
        public string Alias { get; set; }
        [Column("song_en_id")]
        [StringLength(100)]
        public string SongEnId { get; set; }
        /// <summary>
        /// 该alias对应的曲子
        /// </summary>
        public virtual Song CorrespondingSong { get; set; }
        public int? SourceUserID { get; set; }
        /// <summary>
        /// 内容贡献用户
        /// </summary>
        public virtual PlatformUser SourceUser { get; set; }
    }
}
