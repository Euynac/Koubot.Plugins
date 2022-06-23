using System.Text.Json;
using System.Text.Json.Serialization;
using Koubot.Shared.Interface;
using Koubot.Tool.Web;
using Koubot.Tool.Web.RateLimiter;


namespace KouFunctionPlugin.Pixiv
{
    public class SetuAPI : IKouError<SetuAPI.Error>
    {
        public enum Error
        {
            None
        }
        public Error ErrorCode { get; set; }
        public string? ErrorMsg { get; set; }

        public ResponseDto.Root? Call(int num = 10)
        {
            var body = new RequestDto()
            {
                Num = num,
                R18 = 2
            };
            var option = new JsonSerializerOptions()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string responseStr = null;
            using (var limiter = new LeakyBucketRateLimiter(nameof(SetuAPI), 1))
            {
                if (limiter.CanRequest())
                {
                    responseStr = WebHelper.HttpPost("https://api.lolicon.app/setu/v2", JsonSerializer.Serialize(body, option),
                        WebContentType.Json);
                }
            }

            return responseStr == null ? null : JsonSerializer.Deserialize<ResponseDto.Root>(responseStr, option);
        }
    }
}