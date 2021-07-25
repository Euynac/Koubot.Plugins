using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;

namespace KouGamePlugin.Arcaea.Models
{
    /// <summary>
    /// Arcaea歌曲别名
    /// </summary>
    public partial class SongAlias : KouFullAutoModel<SongAlias>
    {
        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => AliasID == id;
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is SongAlias alias)
            {
                return this.TryEqual(AliasID, alias.AliasID)
                       || Alias == alias.Alias && SongEnId == alias.SongEnId;
            }
            return base.Equals(obj);
        }

        protected override dynamic ModelCacheIncludeConfig(IQueryable<SongAlias> set)
        {
            return set.Include(p => p.CorrespondingSong)
                .Include(p => p.SourceUser);
        }

        public override string ToString(FormatType format, object supplement = null)
        {
            return format switch
            {
                FormatType.Brief => $"{AliasID}.{Alias}——{CorrespondingSong?.SongTitle}",
                FormatType.Detail => $"{AliasID}.{Alias}——{CorrespondingSong?.SongTitle}" +
                                     $"{SourceUser?.Be($"\n贡献者：{SourceUser.Name}")}",
                FormatType.Customize1 => $"{Alias}",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        public override Action<EntityTypeBuilder<SongAlias>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.AliasID);

                entity.HasIndex(e => e.Alias);

                entity
                    .HasOne(p => p.SourceUser)
                    .WithMany()
                    .HasPrincipalKey(p =>p.Id)
                    .HasForeignKey(p => p.SourceUserID)
                    .OnDelete(DeleteBehavior.ClientSetNull);
            };
        }
    }
}
