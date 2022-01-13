using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using KouCommand = Koubot.Shared.Protocol.KouCommand;

namespace KouFunctionPlugin.Models
{
    public partial class IdiomDictionary : KouFullAutoModel<IdiomDictionary>
    {
        public override int? GetItemID() => Id;
        public override bool UseAutoCache() => false;
        public override bool UseItemIDToFormat()
        {
            return true;
        }

        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的成语";
        }

        public override string? GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(Derivation), $"\n   来源：{Derivation}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Pinyin), $"\n   拼音：{Pinyin}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Explanation), $"\n   解释：{Explanation}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(Abbreviation), $"\n   缩写：{Abbreviation}")}";
        }
        public override string? ToString(FormatType format, object? supplement = null, KouCommand? command = null)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"{Word} [{Abbreviation}]";

                case FormatType.Detail:
                    return $"{Id}.{Word} [{Abbreviation}]{Pinyin?.BeIfNotWhiteSpace($"\n拼音：{Pinyin}")}{Explanation?.BeIfNotWhiteSpace($"\n解释：{Explanation}")}{Derivation?.BeIfNotWhiteSpace($"\n来源：{Derivation}")}{Example?.BeIfNotWhiteSpace($"\n例子：{Example}")}";

            }
            return null;
        }

        public override Action<EntityTypeBuilder<IdiomDictionary>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.Id)
                    .HasName("PRIMARY");
                entity.HasKey(e => e.Word);
            };
        }



    }
}
