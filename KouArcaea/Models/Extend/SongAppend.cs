using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace KouGamePlugin.Arcaea.Models
{
    public partial class SongAppend : KouFullAutoModel<SongAppend>
    {
        public override string ToString(FormatType formatType, object supplement = null, KouCommand command = null)
        {
            return $"[{ChartRatingClass} {ChartConstant}]" + ChartDesigner?.Be($"谱师：{ChartDesigner}\n") +
                   ChartAllNotes?.Be(
                       $"note总数：{ChartAllNotes}\n地键：{ChartFloorNotes}\n天键：{ChartSkyNotes}\n蛇：{ChartArcNotes}\n长条：{ChartHoldNotes}");
        }

        public override Action<EntityTypeBuilder<SongAppend>> ModelSetup()
        {
            return builder =>
            {
                builder.HasKey(p => new { p.SongEnId, p.ChartRatingClass });
                builder.HasIndex(p => p.SongEnId);
                builder.HasIndex(p => p.ChartDesigner);
            };
        }
    }
}