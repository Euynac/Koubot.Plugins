using Koubot.Shared.Models;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin;

public class QuotationConfig : PluginGlobalConfig
{
    public bool IsEnable { get; set; } = true;
    public double TriggerRate { get; set; } = 0.1;
    public Quotation.QuotationType QuotationType { get; set; } =
           Quotation.QuotationType.Love | Quotation.QuotationType.Praise;

}