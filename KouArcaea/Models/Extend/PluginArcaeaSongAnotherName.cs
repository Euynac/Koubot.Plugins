using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

namespace KouGamePlugin.Arcaea.Models
{
    /// <summary>
    /// Arcaea歌曲别名
    /// </summary>
    public partial class PluginArcaeaSongAnotherName : KouAutoModel<PluginArcaeaSongAnotherName>
    {
        public override string ToString(FormatType format)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"{this.AnotherNameId}.{this.AnotherName}【{this.PluginArcaeaSong2anothername.First().Song.SongTitle}】";
                case FormatType.Detail:
                    return $"{this.AnotherNameId}.{this.AnotherName}【{this.PluginArcaeaSong2anothername.First().Song.SongTitle}】";
            }
            return null;
        }

        public override Action<EntityTypeBuilder<PluginArcaeaSongAnotherName>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.AnotherNameId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.AnotherName)
                    .HasName("plugin_Arcaea_song_another_name_index_3");

                entity.Property(e => e.AnotherName).IsUnicode(false);

                entity.Property(e => e.SongEnId).IsUnicode(false);
            };
        }
    }
}
