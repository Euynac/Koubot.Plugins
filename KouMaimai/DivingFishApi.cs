using System.Collections.Generic;
using Koubot.Shared.Models;
using Koubot.Tool.Interfaces;
using Koubot.Tool.Web;
using KouGamePlugin.Maimai.Models;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Koubot.Tool.Extensions;

namespace KouGamePlugin.Maimai;

public class DivingFishApi : IKouError<DivingFishApi.ErrorCodes>
{
    private const string _tokenKey = "jwt_token";
    public enum ErrorCodes
    {
        None,
        [Description("登录失败，用户名或密码错误")]
        LoginFailed,
    }
    public string? Username { get; }
    public string? Password { get; }
    public string? TokenValue { get; private set; }
    public string? BindQQ { get; }
    public DivingFishApi(string username, string password, string? tokenValue, string? bindQQ)
    {
        Username = username;
        Password = password;
        TokenValue = tokenValue;
        BindQQ = bindQQ;
    }

    public DivingFishApi(string qq)
    {
        BindQQ = qq;
    }
    public DivingFishApi(MaiUserConfig config)
    {
        Username = config.Username;
        Password = config.Password;
        TokenValue = config.LoginTokenValue;
        
    }

    public static DivingFishChartStatusDto? GetChartStatusList()
    {
        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/chart_stats").SetBody().SendRequest(HttpMethods.GET);
        var body = response.Body;
        return JsonSerializer.Deserialize<DivingFishChartStatusDto>(body);
    }
    public static DivingFishChartInfoDto.Root? GetChartInfoList()
    {
        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/music_data").SetBody().SendRequest(HttpMethods.GET);
        var body = response.Body;
        return new DivingFishChartInfoDto.Root()
            { Infos = JsonSerializer.Deserialize<List<DivingFishChartInfoDto.ChartInfo>>(body) };
    }
    //public static bool

    /// <summary>
    /// 登录并获取token
    /// </summary>
    /// <returns></returns>
    public bool Login()
    {
        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/login").SetQPS(1).SetJsonBody(new
        {
            username = Username,
            password = Password,
        }, o => o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull).SendRequest(HttpMethods.POST);
        if (response.HasError)
        {
            ErrorMsg = $"{response.ExceptionStatus}";
            return this.ReturnFalseWithError("获取Token失败" + ErrorMsg);
        }
        var node = JsonNode.Parse(response.Body)!;
        if (node["errcode"] is not null)
        {
            return this.ReturnError(node["message"]!.GetValue<string>());
        }

        if (response.Cookies?[_tokenKey]?.Value is { } value)
        {
            TokenValue = value;
        }
        else
        {
            return this.ReturnError("获取Token失败");
        }

        return true;
    }
    /// <summary>
    /// 获取当前用户资料
    /// </summary>
    /// <returns></returns>
    public DetailProfile? GetProfile()
    {
        if (BindQQ == null)
        {
            return this.ReturnNullWithError("未绑定QQ");
        }
        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/query/player").SetQPS(1).SetJsonBody(new
        {
            qq = BindQQ,
        }, o => o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull).SendRequest(HttpMethods.POST);
        return response.HasError ? this.ReturnNullWithError(response.Body) : response.Body.DeserializeJson<DetailProfile>(new JsonSerializerOptions(){ NumberHandling = JsonNumberHandling.AllowReadingFromString |
            JsonNumberHandling.WriteAsString});

        //if (TokenValue is null)
        //{
        //    if (!Login()) return null;
        //}

        //var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/player/profile")
        //    .AddCookie(_tokenKey, TokenValue).SetQPS(1).SetBody().SendRequest(HttpMethods.GET);

        //var profile = response.Body.DeserializeJson<UserProfile>();
        //if (profile == null)
        //{
        //    var node = JsonNode.Parse(response.Body)!;
        //    return this.ReturnNullWithError(node["message"]!.GetValue<string>());
        //}
        //return profile;
    }
    //public class UserProfile
    //{
    //    public int additional_rating { get; set; }
    //    public string bind_qq { get; set; }
    //    public string nickname { get; set; }
    //    public string plate { get; set; }
    //    public bool privacy { get; set; }
    //    public string username { get; set; }
    //}
    /// <summary>
    /// 获取用户所有成绩
    /// </summary>
    public bool FetchUserRecords(UserAccount user)
    {
        if (TokenValue is null)
        {
            if (!Login()) return false;
        }
        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/player/records")
            .AddCookie(_tokenKey, TokenValue).SetQPS(1).SetBody().SendRequest(HttpMethods.GET);

        if (response.HasError)
        {
            ErrorMsg = $"{response.ExceptionStatus}";
            return false;
        }
        var records = response.Body.DeserializeJson<DivingFishRecordResponseDto>();
        if (records == null)
        {
            var node = JsonNode.Parse(response.Body)!;
            return this.ReturnFalseWithError(node["message"]!.GetValue<string>());
        }

        records.SaveToDb(user);
        return true;
    }

    


  

    public string? ErrorMsg { get; set; }
    public ErrorCodes ErrorCode { get; set; }
}