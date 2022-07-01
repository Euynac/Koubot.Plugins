using Koubot.Shared.Protocol.Attribute;
using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Aeronautics;


[KouAutoModelTable("airline", new[] { nameof(KouAeronautics) })]
public partial class Airline : KouFullAutoModel<Airline>
{
    public override bool UseAutoCache()
    {
        return false;
    }

    public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
    {
        ruleDictionary = new Dictionary<string, string>
        {
            {nameof(CompanyName), userInput},
            {nameof(EnglishName), userInput},
            {nameof(Code3), userInput},
            {nameof(Code2),userInput},
            {nameof(Surname),userInput}
        };
        relationStr = $"{{{nameof(CompanyName)}}}||{{{nameof(EnglishName)}}}||{{{nameof(Surname)}}}||{{{nameof(Code3)}}}||{{{nameof(Code2)}}}";
        return true;
    }

    public override Action<EntityTypeBuilder<Airline>>? ModelSetup()
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
            FormatType.Brief => $"{Code3} {CompanyName?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}{EnglishName?.BeIfNotEmpty("({0})",true)}",
            FormatType.Detail => $"{CompanyID}.{Code3} {CompanyName?.BeNullIfEmpty() ?? Surname?.FirstOrDefault()}{EnglishName?.BeIfNotEmpty("({0})", true)}" +
                                 Surname?.BeIfNotEmptySet($"\n别名：{Surname.ToStringJoin(',')}") +
                                 SITAAddress?.BeIfNotEmpty("\nSITA:{0}",true) +
                                 AFTNAddress?.BeIfNotEmpty("\nAFTN:{0}",true)
        };
    }

}