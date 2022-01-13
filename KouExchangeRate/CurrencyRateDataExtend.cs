using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace KouFunctionPlugin.Currency.Models;

public partial class CurrencyRateData
{
    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        if (obj is CurrencyRateData other)
        {
            return other.Code == Code;
        }

        return false;
    }

    public double GetCurRateValue(CurrencyRateData otherRateData, double otherValue) =>
        (otherValue / otherRateData.Rate) * Rate;


    public override Action<EntityTypeBuilder<CurrencyRateData>>? ModelSetup()
    {
        return builder =>
        {
            builder.Property(p => p.Code).HasConversion<EnumToStringConverter<CurrencyCode>>();
        };
    }

    public override string? ToString(FormatType formatType, object? supplement = null, KouCommand? command = null)
    {
        return formatType switch
        {
            FormatType.Brief => $"{Code}({CountryZhName})——{Rate:0.#####}",
            FormatType.Detail => $"{Code}" +
                                 $"\n汇率（人民币）：{Rate:0.#####}" +
                                 $"\n国家：{Country}({CountryZhName})" +
                                 $"\n货币名：{CurrencyName}({CurrencyZhName})",
            _ => throw new ArgumentOutOfRangeException(nameof(formatType), formatType, null)
        };
    }
}