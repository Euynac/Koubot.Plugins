using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using Koubot.Tool.Expand;

namespace KouFunctionPlugin.Models
{
    public partial class PluginIdiomDictionary : KouAutoModel<PluginIdiomDictionary>
    {
        public override bool IsTheItemID(int id)
        {
            return id == Id;
        }

        public override bool IsAutoItemIDEnabled()
        {
            return true;
        }

        public override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的成语";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Derivation), $"\n   来源：{Derivation}")}" + 
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Pinyin), $"\n   拼音：{Pinyin}")}" + 
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Explanation), $"\n   解释：{Explanation}")}" +
                   $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Abbreviation), $"\n   缩写：{Abbreviation}")}";
        }
        public override string ToString(FormatType format, object supplement = null)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"{Id}.{Word} [{Abbreviation}]";

                case FormatType.Detail:
                    return $"{Id}.{Word} [{Abbreviation}]{Pinyin?.BeIfNotWhiteSpace($"\n拼音：{Pinyin}")}{Explanation?.BeIfNotWhiteSpace($"\n解释：{Explanation}")}{Derivation?.BeIfNotWhiteSpace($"\n来源：{Derivation}")}{Example?.BeIfNotWhiteSpace($"\n例子：{Example}")}";

            }
            return null;
        }

        public override Action<EntityTypeBuilder<PluginIdiomDictionary>> ModelSetup()
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
