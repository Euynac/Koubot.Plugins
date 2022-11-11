using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using KouCommand = Koubot.Shared.Protocol.KouCommand;

namespace KouGamePlugin.Maimai.Models
{
    public partial class ArcadeMap : KouFullAutoModel<ArcadeMap>
    {
        public override FormatConfig ConfigFormat() => new() {UseItemIdToFormat = true};
        public override int? GetItemID() => LocationId;
        protected override KouMessage ReplyOnFailingToSearch()
        {
            return "未找到符合条件的洗衣机店";
        }

        public override bool Equals(object? obj)
        {
            if (obj is ArcadeMap map)
            {
                return map.LocationId == LocationId;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return LocationId.GetHashCode();
        }

        public override string? GetAutoCitedSupplement(HashSet<string> citedFieldNames)
        {
            return
                   $"{citedFieldNames.BeIfContains(nameof(ArcadeName), $"\n   电玩城名：{ArcadeName}")}" +
                   $"{citedFieldNames.BeIfContains(nameof(MallName), $"\n   商城名：{MallName}")}";
        }


        public override string? ToString(FormatType format, object? supplement = null, KouCommand? command = null)
        {

            switch (format)
            {
                case FormatType.Brief:
                    return $"[{MachineCount}]{Address}";

                case FormatType.Detail:
                case FormatType.Customize1:
                case FormatType.Customize2:
                case FormatType.Customize3:
                    var arcadeName = ArcadeName;
                    var curPeople = PeopleCount?.Sum(p => p.AlterCount) ?? 0;
                    var modifiedTime = PeopleCount?.MaxBy(p => p.ModifyAt)?.ModifyAt ?? default;
                    switch (format)
                    {
                        case FormatType.Customize1:
                            return $"{LocationId}.{arcadeName}当前人数：{curPeople}\n" +
                                   $"更新时间：{modifiedTime:T}";
                        case FormatType.Customize2 when PeopleCount.IsNullOrEmptySet():
                            return $"{arcadeName}暂时没有人提供这边的消息";
                        case FormatType.Customize2:
                        {
                            var records = PeopleCount.StringJoin('\n');
                            return $"{LocationId}.{arcadeName}历史记录\n{records}";
                        }
                        case FormatType.Customize3 when PeopleCount.IsNullOrEmptySet():
                        {
                            return $"{arcadeName} - 无人提供";
                        }
                        case FormatType.Customize3:
                        {
                            return $"{arcadeName}[{MachineCount}] - {curPeople}人";
                        }
                        default:
                            return
                                $"{LocationId}.{ArcadeName}" +
                                $"\n机台数量：{MachineCount}" +
                                $"\n地址：{Address}" +
                                $"\n商场名：{MallName}" +
                                Aliases?.BeIfNotEmptySet($"\n别名：{Aliases.StringJoin(",")}") +
                                curPeople.BeIfNotDefault($"\n当前人数：{curPeople}\n更新时间：{modifiedTime:T}");
                    }
            }
            return null;
        }

        public override Action<EntityTypeBuilder<ArcadeMap>> ModelSetup()
        {
            return entity =>
            {
                entity.HasKey(e => e.LocationId)
                    .HasName("PRIMARY");
                entity.Property(p => p.Aliases)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
                entity.Property(p => p.Photos)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));
                entity.Property(p => p.PeopleCount)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<CardRecord>>(v, (JsonSerializerOptions)null));
            };
        }



    }
}
