using Koubot.SDK.AutoModel;
using Koubot.SDK.System.Messages;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.String;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;



namespace KouFunctionPlugin;

[AutoTable("list", new []{nameof(KouQuotation)})]
public partial class Quotation : KouFullAutoModel<Quotation>
{
    public override Action<EntityTypeBuilder<Quotation>>? ModelSetup()
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

    public string GetParsedContent()
    {
        if(KouTemplate.NeedRender(Content))
        {
            var template = new KouTemplate(Content);
            return Content;
        }
        return Content;
    }
    
    public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
    {
        return formatType switch
        {
            FormatType.Brief => $"{Content}",
            FormatType.Detail => $"{ID}.{Content}\n{Type.GetKouEnumName()}{Contributor?.Nickname.Be("\n贡献人:{0}", true)}"
        };
    }
}