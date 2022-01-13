using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Tool.Interfaces;
using Koubot.Tool.Web;
using KouGamePlugin.Maimai.Models;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

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
    public string Username { get; }
    public string Password { get; }
    public string TokenValue { get; private set; }
    public string BindQQ { get; }
    public DivingFishApi(string username, string password, string? tokenValue, string? bindQQ)
    {
        Username = username;
        Password = password;
        TokenValue = tokenValue;
        BindQQ = bindQQ;
    }

    public DivingFishApi(MaiUserConfig config)
    {
        Username = config.Username;
        Password = config.Password;
        TokenValue = config.LoginTokenValue;
    }
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
    public UserProfile GetProfile()
    {
        if (TokenValue is null)
        {
            if (!Login()) return null;
        }

        var response = KouHttp.Create("https://www.diving-fish.com/api/maimaidxprober/player/profile")
            .AddCookie(_tokenKey, TokenValue).SetQPS(1).SetBody().SendRequest(HttpMethods.GET);

        var profile = JsonSerializer.Deserialize<UserProfile>(response.Body);
        if (profile == null)
        {
            var node = JsonNode.Parse(response.Body)!;
            return this.ReturnNullWithError(node["message"]!.GetValue<string>());
        }

        return profile;
    }
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

        var records = JsonSerializer.Deserialize<DivingFishRecordResponseDto>(response.Body);
        if (records == null)
        {
            var node = JsonNode.Parse(response.Body)!;
            return this.ReturnFalseWithError(node["message"]!.GetValue<string>());
        }

        records.SaveToDb(user);
        return true;
    }

    public class UserProfile
    {
        public int additional_rating { get; set; }
        public string bind_qq { get; set; }
        public string nickname { get; set; }
        public string plate { get; set; }
        public bool privacy { get; set; }
        public string username { get; set; }
    }

    public string? ErrorMsg { get; set; }
    public ErrorCodes ErrorCode { get; set; }
}