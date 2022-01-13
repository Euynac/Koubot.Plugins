using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.Tool;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using KouExchangeRate;
using KouFunctionPlugin.Currency.Models;

namespace KouFunctionPlugin.Currency
{
    [KouPluginClass("rate",
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
                if (success)
                {
                    config.LastSuccessUpdateTime = config.LastUpdateTime;
                    config.LastUpdateSuccess = true;
                }
                config.SaveChanges();
            });
        }

        [KouPluginFunction(ActivateKeyword = "status", Name = "更新状态")]
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

        [KouPluginFunction(Name = "是多少RMB")]
        public override object? Default([KouPluginArgument(Name = "数字+国家/货币名/货币代号")]string? str = null)
        {
            
            if (!str.MatchOnceThenReplace(@"\d+(\.\d+)?", out string name, out var valueGroup))
                return "请输入诸如“100日元”之类的格式";
            if (!double.TryParse(valueGroup[0].Value, out double value))
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
                var id = SessionService.Ask<int>($"是下面哪个？输入ID：{list.ToSetStringWithID(5)}");
                otherRateData = list.ElementAtOrDefault(id - 1);
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