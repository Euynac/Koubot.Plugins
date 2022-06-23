using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.RegularExpressions;
using Koubot.SDK.System;

namespace KouFunctionPlugin.Aeronautics;

[KouAutoModelTable("airport", new[] { nameof(KouAeronautics) })]
public partial class AirPort : KouFullAutoModel<AirPort>
{
    public override bool UseAutoCache()
    {
        return false;
    }
    public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
    {
        ruleDictionary = new Dictionary<string, string>
        {
            {nameof(Code4), userInput},
            {nameof(City), userInput},
            {nameof(Code3), userInput},
            {nameof(Country),userInput},
            {nameof(Surname),userInput}
        };
        relationStr = $"{{{nameof(Code4)}}}||{{{nameof(City)}}}||{{{nameof(Surname)}}}||{{{nameof(Code3)}}}||{{{nameof(Country)}}}";
        return true;
    }
    public override Action<EntityTypeBuilder<AirPort>>? ModelSetup()
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
            FormatType.Brief => $"{Code4} {Name?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}",
            FormatType.Detail => $"{ID}.{Code4} {Name?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}" +
                                 Surname?.BeIfNotEmptySet($"\n别名：{Surname.ToStringJoin('，')}") +
                                 Country?.BeIfNotEmpty($"\n国家：{Country}") +
                                 Longitude.BeIfNotDefault($"\n经纬度：({Longitude}, {Latitude})")
        };
    }
}