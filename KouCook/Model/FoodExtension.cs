
using Koubot.SDK.AutoModel;
using Koubot.SDK.System;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KouFunctionPlugin.Cook;

[KouAutoModelTable("list", new []{nameof(KouFood)}, Name = "美食图鉴")]
public partial class Food : KouFullAutoModel<Food>
{
    public static KouImage? SaveFoodImage(string foodName, KouImage foodImage)
    {
        foodName = foodName.FilterForFileName().LimitLength(100);
        var fileName =
            $"{foodName}-{DateTime.Now.ToTimeStamp(TimeExtensions.TimeStampType.Javascript).ToString()}";
        foodImage.SaveToLocalPath(Path.Combine(DataDirectory(), fileName), out var image);
        return image;
    }
    /// <summary>
    /// 删除记录并同时删除该图片
    /// </summary>
    /// <returns></returns>
    public override bool DeleteThis()
    {
        return FileTool.Delete(Path.Combine(DataDirectory(), ImageUrl)) && base.DeleteThis();
    }

    public override Action<EntityTypeBuilder<Food>>? ModelSetup()
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
            FormatType.Brief => Name,
            FormatType.Customize1 => $"{new KouImage(ImageUrl, this).ToKouResourceString()}\n{ID}.{Name}",
            FormatType.Detail => $"{new KouImage(ImageUrl, this).ToKouResourceString()}\n{ID}.{Name}{Contributor?.Nickname.Be("\n贡献人:{0}", true)}"
        };
    }
}