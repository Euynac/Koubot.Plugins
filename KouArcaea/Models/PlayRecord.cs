using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.Shared.Interface;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_play_record")]
    public partial class PlayRecord : KouFullAutoModel<PlayRecord>
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        [Column("user_id")]
        [StringLength(50)]
        public string UserId { get; set; }
        [Column("play_at")]
        public DateTime? PlayAt { get; set; }
        [Column("song_id")]
        public int? SongId { get; set; }
        [Column("score")]
        public int? Score { get; set; }
        [Column("play_ptt")]
        public double? PlayPtt { get; set; }
        [Column("play_tp")]
        public double? PlayTp { get; set; }

        [ForeignKey(nameof(SongId))]
        [InverseProperty(nameof(Models.Song.PluginArcaeaPlayRecord))]
        public virtual Song Song { get; set; }

        public override string ToString(FormatType formatType, object supplement = null, KouCommand command = null)
        {
            throw new NotImplementedException();
        }

        public override Action<EntityTypeBuilder<PlayRecord>> ModelSetup()
        {
            return entity =>
            {
                entity.HasIndex(e => e.SongId);

                entity.HasOne(d => d.Song)
                    .WithMany(p => p.PluginArcaeaPlayRecord)
                    .HasForeignKey(d => d.SongId);
            };
        }
    }
}
