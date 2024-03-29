using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.Attribute;

namespace KouFunctionPlugin.Pixiv
{

    /// <summary>
    /// Pixiv中的Tag
    /// </summary>
    [AutoTable("tag", new[] { nameof(KouPixiv) }, Name = "标签列表")]
    [Table("plugin_pixiv_tags")]
    public class PixivTag : KouFullAutoModel<PixivTag>
    {
        [Key]
        public int ID { get; set; }
        /// <summary>
        /// 标签名
        /// </summary>
        [Required]
        [AutoField(ActivateKeyword = "tag|标签")]
        public string Name { get; set; }
        [InverseProperty(nameof(PixivWork.Tags))]
        public virtual ICollection<PixivWork> Works { get; set; }
        public override Action<EntityTypeBuilder<PixivTag>> ModelSetup()
        {
            return builder =>
            {
                builder.HasKey(p => p.ID);
                builder.HasIndex(p => p.Name);
            };
        }
        /// <summary>
        /// 获取标签内容
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PixivTag tag)
            {
                return Name == tag.Name;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
        {
            return $"{ID}.{Name}";
        }
    }
}