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

namespace KouFunctionPlugin.LuckMachine
{
    /// <summary>
    /// 黄历
    /// </summary>
    [Table("plugin_luck_almanac")]
    [KouAutoModelTable("list", new[] { nameof(KouLuck) }, Name = "黄历")]
    public class Almanac : KouFullAutoModel<Almanac>
    {
        [Key]
        public int ID { get; set; }
        public int SourceUserID { get; set; }
        /// <summary>
        /// 黄历内容贡献用户
        /// </summary>
        public virtual PlatformUser SourceUser { get; set; }
        /// <summary>
        /// 是忌的，否则是宜的
        /// </summary>
        public bool IsOminous { get; set; }
        /// <summary>
        /// 事务名
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 详细内容
        /// </summary>
        public string Content { get; set; }

        public override bool IsTheItemID(int id) => id == ID;
        public override bool IsAutoItemIDEnabled() => true;

        protected override dynamic SetModelIncludeConfig(IQueryable<Almanac> set)
        {
            return set.Include(p => p.SourceUser);
        }

        public override string ToString(FormatType formatType, object supplement = null, KouCommand command = null)
        {
            return formatType switch
            {
                FormatType.Brief => $"{ID}.{(IsOminous ? "忌" : "宜")}{Title}：{Content}",
                FormatType.Detail => $"{ID}.{(IsOminous ? "忌" : "宜")}{Title}\n{Content}" +
                                     SourceUser?.Be($"\n贡献人：{SourceUser.Name}"),
                FormatType.Customize1 => $"{(IsOminous ? "忌" : "宜")}{Title}：{Content}",
                _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
            };
        }

        public override Action<EntityTypeBuilder<Almanac>> ModelSetup()
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