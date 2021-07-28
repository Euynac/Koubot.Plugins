using System;
using Koubot.SDK.Interface;
using Koubot.Tool.Extensions;
using Koubot.Tool.Web;
using Koubot.Tool.Web.RateLimiter;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace KouFunctionPlugin.Pixiv
{
    public class SetuAPI : IKouError<SetuAPI.Error>
    {
        public enum Error
        {
            None
        }
        public Error ErrorCode { get; set; }
        public string ErrorMsg { get; set; }

        public ResponseDto.Root? Call(int num = 10)
        {
            var body = new RequestDto()
            {
                Num = num,
                R18 = 2
            };
            var jsonSerializeSetting = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            string responseStr = null;
            using (var limiter = new LeakyBucketRateLimiter(nameof(SetuAPI), 1))
            {
                if (limiter.CanRequest())
                {
                    responseStr = WebHelper.HttpPost("https://api.lolicon.app/setu/v2", JsonConvert.SerializeObject(body, jsonSerializeSetting),
                        WebHelper.WebContentType.Json);
                }
            }

            return responseStr == null ? null : JsonConvert.DeserializeObject<ResponseDto.Root>(responseStr, jsonSerializeSetting);
        }
    }
}