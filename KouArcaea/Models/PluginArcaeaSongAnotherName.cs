using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_song_another_name")]
    public partial class PluginArcaeaSongAnotherName
    {
        public PluginArcaeaSongAnotherName()
        {
            PluginArcaeaSong2anothername = new HashSet<PluginArcaeaSong2anothername>();
        }

        [Key]
        [Column("another_name_id")]
        public int AnotherNameId { get; set; }
        [Column("another_name")]
        [StringLength(30)]
        public string AnotherName { get; set; }
        [Column("song_en_id")]
        [StringLength(100)]
        public string SongEnId { get; set; }
        /// <summary>
        /// 可能一个别名对应多个难度的歌曲
        /// </summary>
        [InverseProperty("AnotherName")]
        public virtual ICollection<PluginArcaeaSong2anothername> PluginArcaeaSong2anothername { get; set; }
    }
}
