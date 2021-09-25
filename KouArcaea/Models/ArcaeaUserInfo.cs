using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KouGamePlugin.Arcaea.Models
{
    [Table("plugin_arcaea_userinfo")]
    public partial class ArcaeaUserInfo : KouFullAutoModel<ArcaeaUserInfo>
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
        public double? ArcaeaKouPtt { get; set; }
        [Column("arcaea_avg_tp")]
        public double? ArcaeaAvgTp { get; set; }
        [Column("arcaea_username")]
        [StringLength(20)]
        public string ArcaeaUsername { get; set; }

        public override string ToString(FormatType formatType, object supplement = null, KouCommand command = null)
        {
            throw new NotImplementedException();
        }

        public override Action<EntityTypeBuilder<ArcaeaUserInfo>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.ArcaeaId);

                entity.HasIndex(e => e.ArcaeaUsername);
            };
        }
    }
}
