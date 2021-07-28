using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Koubot.SDK.Models.Entities;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin.Pixiv
{
    public class ResponseDto
    {
        public class DataItem
        {
            /// <summary>
            /// 作品 pid
            /// </summary>
            public long Pid { get; set; }
            /// <summary>
            /// 作品所在页
            /// </summary>
            public int P { get; set; }
            /// <summary>
            /// 作者 uid
            /// </summary>
            public long Uid { get; set; }
            /// <summary>
            /// 作品标题
            /// </summary>
            public string Title { get; set; }
            /// <summary>
            /// 作者名（入库时，并过滤掉 @ 及其后内容）
            /// </summary>
            public string Author { get; set; }
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
            /// 作品标签，包含标签的中文翻译（有的话）
            /// </summary>
            public string[]? Tags { get; set; }
            /// <summary>
            /// 图片扩展名
            /// </summary>
            public string Ext { get; set; }
            /// <summary>
            /// 作品上传日期；时间戳，单位为毫秒
            /// </summary>
            public long UploadDate { get; set; }
            /// <summary>
            /// 包含了所有指定size的图片地址
            /// </summary>
            public Dictionary<string, string> Urls { get; set; }

            public PixivWork ToModel(KouContext context)
            {
                var R18Flag = false;
                List<PixivTag> tags = new();
                if (!Tags.IsNullOrEmptySet())
                {
                    foreach (var tagName in Tags)
                    {
                        if (R18 || tagName.IsMatch("r.*(18|17).*", RegexOptions.IgnoreCase)) R18Flag = true;
                        var newTag = new PixivTag {Name = tagName};
                        var oldTag = newTag.FindThis(context);
                        tags.Add(oldTag ?? newTag);
                    }
                }

                var author = new PixivAuthor() {Name = Author, Uid = Uid};
                var oldAuthor = author.FindThis(context);
                if (oldAuthor != null) author = oldAuthor;

                if (tags.Count == 0) tags = null;
                return new PixivWork()
                {
                    Author = author,
                    Pid = Pid,
                    P = P,
                    Title = Title,
                    R18 = R18Flag,
                    Width = Width,
                    Height = Height,
                    Ext = Ext,
                    UploadDateTimestamp = UploadDate,
                    Tags = tags
                };
            }
        }

        public class Root
        {
            public string Error { get; set; }
            public List<DataItem> Data { get; set; }
        }
        
    }
}