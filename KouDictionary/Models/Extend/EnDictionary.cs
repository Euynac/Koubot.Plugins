using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.Shared.Interface;

namespace KouFunctionPlugin.Models
{
    public partial class EnDictionary : KouFullAutoModel<EnDictionary>
    {
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的单词";
        }
        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return $"{citedFieldNames.BeIfContains(nameof(Population), $"\n   词频：{Population}")}";
        }

        public override Action<EntityTypeBuilder<EnDictionary>> ModelSetup()
        {
            return builder => builder.HasKey(p => p.Word);
        }

        public override string ToString(FormatType format, object supplement = null, KouCommand command = null)
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
    }
}
