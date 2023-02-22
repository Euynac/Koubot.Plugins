using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using KouExchangeRate;
using KouFunctionPlugin.Currency.Models;

namespace KouFunctionPlugin.Currency
{
    [PluginClass("rate",
        "汇率转换", 
        PluginType = PluginType.Function)]
    public class KouExchangeRate : KouPlugin<KouExchangeRate>, IWantPluginGlobalConfig<RateConfig>
    {
        static KouExchangeRate()
        {
            AddCronTab(new TimeSpan(1,0,0,0), () =>
            {
                var success = ExchangeRateApi.UpdateDataToDb();
                var config = GetSingleton().GlobalConfig();
                config.LastUpdateTime = DateTime.Now;
                KouLog.QuickAdd($"尝试刷新汇率：{success.IIf("成功","失败")}");
                if (success)
                {
                    config.LastSuccessUpdateTime = config.LastUpdateTime;
                    config.LastUpdateSuccess = true;
                }
                config.SaveChanges();
            });
        }

        [PluginFunction(ActivateKeyword = "status", Name = "更新状态")]
        public object UpdateStatus()
        {
            var config = this.GlobalConfig();
            if (!config.LastUpdateSuccess)
            {
                return $"汇率上次尝试刷新时间：{config.LastUpdateTime}\n" +
                       $"汇率上次成功刷新时间：{config.LastSuccessUpdateTime}";
            }

            return $"汇率上次成功刷新时间：{config.LastSuccessUpdateTime}";
        }

        [PluginFunction(Name = "是多少RMB")]
        public override object? Default([PluginArgument(Name = "数字+国家/货币名/货币代号")]string? str = null)
        {
            
            if (!str.MatchOnceThenReplace(@"[\d.零壹一贰两二叁三肆四伍五陆六柒七捌八玖九拾十佰百仟千亿万wk]+", out var name, out var valueGroup))
                return "请输入诸如“100日元”之类的格式";
            if (!TypeService.TryConvert(valueGroup[0].Value, out double value))
            {
                return "请输入正确的数字";
            }

            TypeService.TryConvert(name, out CurrencyCode? code);
            var list = CurrencyRateData.Find(p =>
                (code != null && p.Code == code) ||
                p.CountryZhName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                p.Country.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                p.CurrencyZhName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
                p.CurrencyName.Contains(name, StringComparison.OrdinalIgnoreCase));
            if (list.IsNullOrEmptySet()) return $"不知道{name}是什么货币呢";
            CurrencyRateData? otherRateData;
            if (list.Count > 1)
            {
                otherRateData = SessionService.AskWhichOne(list);
                if (otherRateData == null) return null;
            }
            else
            {
                otherRateData = list.First();
            }

            var result = CurrencyRateData.SingleOrDefault(p => p.Code == CurrencyCode.CNY)!.GetCurRateValue(
                otherRateData,
                value);
            return $"{value:0.####}{otherRateData.Code}({otherRateData.CurrencyZhName}) = {result:0.####}RMB";
        }
    }
}