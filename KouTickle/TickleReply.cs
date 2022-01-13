using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Koubot.Shared.Protocol.Attribute;

namespace KouFunctionPlugin
{
    [KouAutoModelTable("list", new[] { nameof(KouTickle) }, Name = "戳一戳反馈列表")]
    [Table("plugin_tickle_reply")]
    public class TickleReply : KouFullAutoModel<TickleReply>
    {
        [Key]
        public int ID { get; set; }
        public int SourceUserID { get; set; }
        /// <summary>
        /// 内容贡献用户
        /// </summary>
        public virtual PlatformUser SourceUser { get; set; }
        /// <summary>
        /// 详细内容
        /// </summary>
        public string Reply { get; set; }

        public override int? GetItemID() => ID;
        public override bool UseItemIDToFormat() => true;

        protected override dynamic SetModelIncludeConfig(IQueryable<TickleReply> set)
        {
            return set.Include(p => p.SourceUser);
        }

        public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
        {
            return formatType switch
            {
                FormatType.Brief => $"{ID}.{Reply}",
                FormatType.Detail => $"{ID}.{Reply}" +
                                     SourceUser?.Be($"\n贡献人：{SourceUser.Name}"),

                _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
            };
        }

        public override Action<EntityTypeBuilder<TickleReply>> ModelSetup()
        {
            return entity =>
            {
                entity
                    .HasOne(p => p.SourceUser)
                    .WithMany()
                    .HasPrincipalKey(p => p.Id)
                    .HasForeignKey(p => p.SourceUserID)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            };
        }
    }
}