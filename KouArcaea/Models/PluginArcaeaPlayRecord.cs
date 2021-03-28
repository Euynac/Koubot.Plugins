using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_play_record")]
    public partial class PluginArcaeaPlayRecord : KouAutoModel<PluginArcaeaPlayRecord>
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
        [InverseProperty(nameof(PluginArcaeaSong.PluginArcaeaPlayRecord))]
        public virtual PluginArcaeaSong Song { get; set; }

        public override string ToString(FormatType formatType, object supplement = null)
        {
            throw new NotImplementedException();
        }

        public override Action<EntityTypeBuilder<PluginArcaeaPlayRecord>> ModelSetup()
        {
            return entity =>
            {
                entity.HasIndex(e => e.SongId)
                    .HasName("song_id");

                entity.Property(e => e.UserId).IsUnicode(false);

                entity.HasOne(d => d.Song)
                    .WithMany(p => p.PluginArcaeaPlayRecord)
                    .HasForeignKey(d => d.SongId)
                    .HasConstraintName("plugin_arcaea_play_record_ibfk_1");
            };
        }
    }
}
