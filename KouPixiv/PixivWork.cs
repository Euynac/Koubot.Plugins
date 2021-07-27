using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Pixiv
{
    [KouAutoModelTable("list", new[] {nameof(KouSetu)}, Name = "作品列表")]
    [Table("plugin_pixiv_works")]
    public class PixivWork : KouFullAutoModel<PixivWork>
    {
        /// <summary>
        /// 库中id
        /// </summary>
        [Key]
        public int ID { get; set; }
        /// <summary>
        /// 作品PID
        /// </summary>
        [Required]
        public long Pid { get; set; }
        /// <summary>
        /// 作品所在页（多张作品时的页数）
        /// </summary>
        public int P { get; set; }
        /// <summary>
        /// 作者 uid
        /// </summary>
        public long Uid { get; set; }
        /// <summary>
        /// 作者名（入库时，并过滤掉 @ 及其后内容）
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// 作品标题
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 是否 R18（在库中的分类，不等同于作品本身的 R18 标识）
        /// </summary>
        public bool R18 { get; set; }
        /// <summary>
        /// 原图宽度 px
        /// </summary>
        public int Width { get; set; }
        /// <summary>
        /// 原图高度 px
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// 图片扩展名
        /// </summary>
        public string Ext { get; set; }
        /// <summary>
        /// 作品上传日期；时间戳，单位为毫秒
        /// </summary>
        public long UploadDateTimestamp { get; set; }
        /// <summary>
        /// 作品标签，包含标签的中文翻译（有的话）
        /// </summary>
        [KouAutoModelField(true)]
        [InverseProperty(nameof(PixivTag.Works))]
        public virtual ICollection<PixivTag> Tags { get; set; }
        /// <summary>
        /// 作品上传日期
        /// </summary>
        [NotMapped]
        public DateTime UploadDate => UploadDateTimestamp.ToDateTime();
        public override Action<EntityTypeBuilder<PixivWork>> ModelSetup()
        {
            return builder =>
            {
                builder.HasKey(p => p.ID);
            };
        }

        public override string ToString(FormatType formatType, object supplement = null)
        {
            return formatType switch
            {
                FormatType.Brief => $"{ID}.{Title}「{Pid}」 —— {Author}",
                FormatType.Detail => $"{ID}.{Title}" +
                                     $"\nPID:{Pid}" +
                                     $"\nAuthor:{Author}「{Uid}」" +
                                     $"\nSize:{Width}x{Height}" +
                                     Tags?.ToStringJoin(",")?.BeIfNotEmpty("\nTags:{0}", true) +
                                     $"\nTime:{UploadDate}",
                _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
            };
        }
    }
}