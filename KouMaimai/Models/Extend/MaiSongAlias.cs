using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Linq;

namespace KouGamePlugin.Maimai.Models
{
    /// <summary>
    /// Maimai歌曲别名
    /// </summary>
    public partial class MaiSongAlias : KouFullAutoModel<MaiSongAlias>
    {
        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => AliasID == id;
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (obj is MaiSongAlias alias)
            {
                return this.TryEqual(AliasID, alias.AliasID)
                       || Alias == alias.Alias && SongKanaId == alias.SongKanaId;
            }
            return base.Equals(obj);
        }

        protected override dynamic SetModelIncludeConfig(IQueryable<MaiSongAlias> set)
        {
            return set.Include(p => p.CorrespondingSong)
                .Include(p => p.SourceUser);
        }

        public override string ToString(FormatType format, object supplement = null, KouCommand command = null)
        {
            return format switch
            {
                FormatType.Brief => $"{AliasID}.{Alias}——{CorrespondingSong?.ElementAt(0).SongTitle}",
                FormatType.Detail => $"{AliasID}.{Alias}——{CorrespondingSong?.ElementAt(0).SongTitle}" +
                                     $"{SourceUser?.Be($"\n贡献者：{SourceUser.Name}")}",
                FormatType.Customize1 => $"{Alias}",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        public override Action<EntityTypeBuilder<MaiSongAlias>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.AliasID);

                entity.HasIndex(e => e.Alias);

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
