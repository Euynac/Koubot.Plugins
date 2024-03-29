﻿using Koubot.SDK.Models.Entities;
using Koubot.SDK.Services;
using Koubot.SDK.System;
using Koubot.Tool.Extensions;
using Koubot.Tool.Web;
using KouFunctionPlugin.Currency.Models;

namespace KouExchangeRate;

/// <summary>
/// https://www.exchangerate-api.com/
/// </summary>
public class ExchangeRateApi
{
    private static string Key { get; } = StartupConfig.Singleton.RateApiKey;
    public class RateDataDto
    {
        public string result { get; set; }
        public Dictionary<CurrencyCode, double> conversion_rates { get; set; }
    }

    public static bool UpdateDataToDb()
    {
        var response = KouHttp.Create($"https://v6.exchangerate-api.com/v6/{Key}/latest/CNY")
            .SendRequest(HttpMethods.GET).Body;
        var data = response.DeserializeJson<RateDataDto>();
        if (data == null) return false;
        using var context = new KouContext();
        var list = context.Set<CurrencyRateData>().ToHashSet();
        foreach (var (code, rate) in data.conversion_rates)
        {
            if (!list.TryGetValue(new CurrencyRateData
                {
                    Code = code
                }, out var dbValue)) continue;
            dbValue.Rate = rate;
        }
        context.SaveChanges();
        CurrencyRateData.UpdateCache();
        return true;
    }
}