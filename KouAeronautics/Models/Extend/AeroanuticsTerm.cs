using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using Koubot.SDK.AutoModel;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Aeronautics;


[AutoTable("term", new[] { nameof(KouAeronautics) }, SupportedActions = AutoModelActions.All, AlterAuthority = Authority.BotMaster)]
public partial class AeronauticsTerm : KouFullAutoModel<AeronauticsTerm>
{
    public override bool UseAutoCache()
    {
        return false;
    }

    public override bool UseCustomDefaultFieldSplit(string userInput, out Dictionary<string, string> ruleDictionary, out string relationStr)
    {
        ruleDictionary = new Dictionary<string, string>
        {
            {nameof(Abbreviation), userInput},
            {nameof(FullName), userInput},
            {nameof(Title), userInput},
        };
        relationStr = $"{{{nameof(Abbreviation)}}}||{{{nameof(FullName)}}}||{{{nameof(Title)}}}";
        return true;
    }

    public override Action<EntityTypeBuilder<AeronauticsTerm>>? ModelSetup()
    {
        return builder =>
        {
            builder.HasKey(p => p.ID);
            builder
                .HasOne(p => p.Contributor)
                .WithMany()
                .HasPrincipalKey(p => p.Id)
                .OnDelete(DeleteBehavior.ClientSetNull);
        };
    }

    public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
    {
        return formatType switch
        {
            FormatType.Brief => $"{Abbreviation}{FullName?.BeIfNotEmpty("({0})",true)}{Title?.BeIfNotEmpty(" {0}",true)}",
            FormatType.Detail => $"{ID}.{(FullName+Abbreviation?.BeIfNotEmpty(", {0}",true)).TrimStart(',', ' ')}" +
                                 $"{Title?.BeIfNotEmpty("\n名称：{0}",true)}" +
                                 $"{Remark?.BeIfNotEmpty("\n备注：{0}",true)}" +
                                 $"{UpdateTime?.BeIfNotDefault("\n更新时间：{0}",true)}"
        };
    }

}