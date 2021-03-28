using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_userinfo")]
    public partial class PluginArcaeaUserinfo : KouAutoModel<PluginArcaeaUserinfo>
    {
        [Column("user_id")]
        [StringLength(50)]
        public string UserId { get; set; }
        [Key]
        [Column("arcaea_id")]
        public int ArcaeaId { get; set; }
        [Column("arcaea_official_ptt")]
        public double? ArcaeaOfficialPtt { get; set; }
        [Column("arcaea_ptt")]
        public double? ArcaeaPtt { get; set; }
        [Column("arceaa_avg_tp")]
        public double? ArceaaAvgTp { get; set; }
        [Column("arcaea_username")]
        [StringLength(20)]
        public string ArcaeaUsername { get; set; }

        public override string ToString(FormatType formatType, object supplement = null)
        {
            throw new NotImplementedException();
        }

        public override Action<EntityTypeBuilder<PluginArcaeaUserinfo>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.ArcaeaId)
                    .HasName("PRIMARY");

                entity.HasIndex(e => e.ArcaeaUsername)
                    .HasName("plugin_Arcaea_userinfo_index_4");

                entity.Property(e => e.ArcaeaUsername).IsUnicode(false);

                entity.Property(e => e.UserId).IsUnicode(false);
            };
        }
    }
}
