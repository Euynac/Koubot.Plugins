using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_song2anothername")]
    public partial class PluginArcaeaSong2anothername : KouAutoModel<PluginArcaeaSong2anothername>
    {
        [Key]
        [Column("another_name_id")]
        public int AnotherNameId { get; set; }
        [Key]
        [Column("song_id")]
        public int SongId { get; set; }

        [ForeignKey(nameof(AnotherNameId))]
        [InverseProperty(nameof(PluginArcaeaSongAnotherName.PluginArcaeaSong2anothername))]
        public virtual PluginArcaeaSongAnotherName AnotherName { get; set; }
        [ForeignKey(nameof(SongId))]
        [InverseProperty(nameof(PluginArcaeaSong.PluginArcaeaSong2anothername))]
        public virtual PluginArcaeaSong Song { get; set; }

        public override string ToString(FormatType formatType, object supplement = null)
        {
            throw new NotImplementedException();
        }

        public override Action<EntityTypeBuilder<PluginArcaeaSong2anothername>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => new { e.AnotherNameId, e.SongId })
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.SongId)
                    .HasName("song_id");

                entity.Property(e => e.AnotherNameId).HasComment("for different ratingClass");

                entity.HasOne(d => d.AnotherName)
                    .WithMany(p => p.PluginArcaeaSong2anothername)
                    .HasForeignKey(d => d.AnotherNameId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("plugin_arcaea_song2anothername_ibfk_1");

                entity.HasOne(d => d.Song)
                    .WithMany(p => p.PluginArcaeaSong2anothername)
                    .HasForeignKey(d => d.SongId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("plugin_arcaea_song2anothername_ibfk_2");
            };
        }
    }
}
