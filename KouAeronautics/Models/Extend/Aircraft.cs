using Koubot.Shared.Protocol.Attribute;
using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Aeronautics;


[AutoTable("aircraft", new[] { nameof(KouAeronautics) })]
public partial class Aircraft : KouFullAutoModel<Aircraft>
{
    public override bool UseAutoCache()
    {
        return false;
    }

    public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
    {
        ruleDictionary = new Dictionary<string, string>
        {
            {nameof(AircraftType), userInput},
            {nameof(ICAOCode), userInput},
            {nameof(Surname), userInput}
        };
        relationStr = $"{{{nameof(AircraftType)}}}||{{{nameof(ICAOCode)}}}||{{{nameof(Surname)}}}";
        return true;
    }

    public override Action<EntityTypeBuilder<Aircraft>>? ModelSetup()
    {
        return builder =>
        {
            builder.Property(p => p.Surname).HasConversion((o) => o == null ? "" : string.Join('/', o),
                s => s.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList());
        };
    }

    public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
    {
        return formatType switch
        {
            FormatType.Brief => $"{AircraftType?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}",
            FormatType.Detail => $"{AircraftID}.{AircraftType?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}" +
                                 Surname?.BeIfNotEmptySet($"\n别名：{Surname.StringJoin('，')}") +
                                 ICAOCode?.BeIfNotEmpty("\nICAO：{0}",true) +
                                 Manufacturer?.BeIfNotEmpty("\n制造商：{0}", true) +
                                 WakeTurbulance?.Be("\n尾流等级：{0}",true) +
                                 FuelCapacity.BeIfNotDefault("\n油箱容量：{0}",true) +
                                 FueledWeight.BeIfNotDefault("\n满油重量：{0}",true) +
                                 Range.BeIfNotDefault("\nRange:{0}", true) +
                                 PassengerSize.BeIfNotDefault("\n载客量：{0}", true) +
                                 CruiseAltitude.BeIfNotDefault("\n巡航高度：{0}",true) +
                                 MinSpeed.BeIfNotDefault("\n最小速度：{0}",true) +
                                 MaxSpeed.BeIfNotDefault("\n最大速度：{0}", true)
        };
    }
}