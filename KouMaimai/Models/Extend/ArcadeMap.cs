using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.Shared.Interface;

namespace KouGamePlugin.Maimai.Models
{
    public partial class ArcadeMap : KouFullAutoModel<ArcadeMap>
    {
        public override bool IsAutoItemIDEnabled() => true;
        public override bool IsTheItemID(int id) => LocationId == id;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的洗衣机店";
        }

        public override string GetAutoCitedSupplement(List<string> citedFieldNames)
        {
            return
                   $"{citedFieldNames.BeIfContains(nameof(ArcadeName), $"\n   电玩城名：{ArcadeName}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(MallName), $"\n   商城名：{MallName}")}";
        }


        public override string ToString(FormatType format, object supplement = null, KouCommand command = null)
        {

            switch (format)
            {
                case FormatType.Brief:
                    return $"{LocationId}.[{MachineCount}]{Address}";

                case FormatType.Detail:
                    return
                        $"{LocationId}.{ArcadeName}" +
                        $"\n机台数量：{MachineCount}" +
                        $"\n电玩城名：{ArcadeName}" +
                        $"\n商场名：{MallName}+" +
                        $"\n地址：{Address}";

            }
            return null;
        }

        public override Action<EntityTypeBuilder<ArcadeMap>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.LocationId)
                    .HasName("PRIMARY");
            };
        }



    }
}
