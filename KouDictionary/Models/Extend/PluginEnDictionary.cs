using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol.AutoModel;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;

namespace KouFunctionPlugin.Models
{
    public partial class PluginEnDictionary : KouAutoModel<PluginEnDictionary>
    {
        public override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的单词";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.ContainsReturnCustomOrNull(nameof(Population), $"\n   词频：{Population}")}";
        }
        public override string ToString(FormatType format, object supplement = null)
        {
            switch (format)
            {
                case FormatType.Brief:
                    return $"【{Word}】{Definition}";

                case FormatType.Detail:
                    return $"{Word}\n(UK)/{UkPron}/ (US)/{UsPron}/\n{Definition}\n词频：{Population}";

            }
            return null;
        }

        public override Action<EntityTypeBuilder<PluginEnDictionary>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.Word)
                    .HasName("PRIMARY");
            };
        }



    }
}
