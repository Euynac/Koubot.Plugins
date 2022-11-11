using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using KouMarketingGenerator;

namespace KouFunctionPlugin;

public class EssayGenerator
{
    private static readonly JsonNode _data;
    static EssayGenerator()
    {
        _data = JsonNode.Parse(data.essay.ConvertToString());
    }

    

    public string Generate(string subject, int num)
    {
        var verbList = _data["verb"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var titleList = _data["title"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var nounList = _data["noun"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var adverb1List = _data["adverb_1"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var adverb2List = _data["adverb_2"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var phraseList = _data["phrase"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var sentenceList = _data["sentence"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var parallelSentenceList = _data["parallel_sentence"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var beginningList = _data["beginning"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var bodyList = _data["body"]!.AsArray().Select(p => p.GetValue<string>()).ToList();
        var endingList = _data["ending"]!.AsArray().Select(p => p.GetValue<string>()).ToList();

        var beginNum = num * 0.15; //开头字数
        var bodyNum = num* 0.7; // 主题
        var endNum = beginNum; //结尾字数相同
        
        string ReplaceAll(string origin, string theme)
        {
            origin = origin.Replace("v", verbList.RandomGetOne());
            origin = origin.Replace("n", nounList.RandomGetOne());
            origin = origin.Replace("ss", sentenceList.RandomGetOne());
            origin = origin.Replace("sp", parallelSentenceList.RandomGetOne());
            origin = origin.Replace("p", phraseList.RandomGetOne());
            origin = origin.Replace("xx", theme);
            return origin;
        }



        var title = ReplaceAll(titleList.RandomGetOne(), subject);
        var begin = "";
        var body = "";
        var end = "";

        while (begin.Length < beginNum)
        {
            begin += ReplaceAll(beginningList.RandomGetOne(), subject);
        }

        while (body.Length < bodyNum)
        {
            body += ReplaceAll(bodyList.RandomGetOne(), subject);
        }

        while (end.Length < endNum)
        {
            end += ReplaceAll(endingList.RandomGetOne(), subject);
        }
        return $"\t{title}\n\t{begin}\n\t{body}\n\t{end}";
    }
}