using System;
using System.Collections.Generic;
using System.Linq;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using KouFunctionPlugin.Models;
using Microsoft.EntityFrameworkCore;
using Mysqlx.Expr;

namespace KouFunctionPlugin;

public class EnglishSolitaireAchievement : IGameRoomLeaderBoardInfo<EnglishSolitaireAchievement>
{
    public int TryTimes { get; set; }
    public int SuccessTimes { get; set; }
    public TimeSpan TotalConsumeTime { get; set; }
    public double AverageCorrectRate => (double)SuccessTimes / (TryTimes.BeNullIfDefault() ?? 1);
    public TimeSpan CorrectAverageTime => new(TotalConsumeTime.Ticks / (SuccessTimes.BeNullIfDefault() ?? 1));
    public int CompareTo(EnglishSolitaireAchievement other)
    {
        return this.CompareToObjDesc(SuccessTimes, other.SuccessTimes, out var result)?
            .CompareToObjAsc(TryTimes, other.TryTimes, out result)?
            .CompareToObjAsc(TotalConsumeTime, other.TotalConsumeTime, out result) == null ? result : 0;
    }

    public KouMessageTableElement GetUserLeaderBoardInfo(PlatformUser user)
    {
        var element = new KouMessageTableElement("参赛人员", "回答数", "正确数", "正确率", "正确平均耗时");
        element.AddRow(user.Name ?? "??", TryTimes.ToString(), SuccessTimes.ToString(), AverageCorrectRate.ToString("P"),CorrectAverageTime.ToZhFormatString());
        return element;
    }
}

public class WordCache
{
    public string Word { get; set; }
}

public class EnglishWordSolitaireRoom : KouGameRoom<EnglishSolitaireAchievement>
{
    public const string Help = "单词接龙。每十轮奖池增益10%，开始游戏后，默认10分钟后结算成绩，玩家不可重复使用已用过的词。";
    public WordCache? CurrentWord { get; set; }
    private readonly object _lock = new();
    public HashSet<string> UserPreviousUseWord { get; set; } = new();

    private static readonly Lazy<List<WordCache>> _wordDictionary = new(() =>
    {
        using var context = new KouContext();
        return context.Set<EnDictionary>().AsNoTracking()
            .Select(p => new WordCache() {Word = p.Word}).ToList();
    }, true);
    public EnglishWordSolitaireRoom(string roomName, PlatformUser ownerUser, PlatformGroup roomGroup, int? fee = 10) : base(roomName, ownerUser, roomGroup, fee)
    {
        RoomHelp = Help;
        UserTakePartInEvent += achievement =>
        {
            achievement.TryTimes++;
        };
        UserCorrectEvent += achievement =>
        {
            achievement.SuccessTimes++;
            achievement.TotalConsumeTime = achievement.TotalConsumeTime.Add(DateTime.Now - CurRoundStartTime);
        };
    }

    private EnDictionary GetInfoFromDb(WordCache cache,KouContext context)
    {
        return EnDictionary.SingleOrDefault(p => p.Word == cache.Word, context)!;
    }

    private WordCache? RandomGetMatchWord(WordCache word)
    {
        var lastPinyin =word.Word.Last();
        return _wordDictionary.Value.Where(p =>
            p.Word.First() == lastPinyin).ToList().RandomGetOne();
    }
    private WordCache? RandomGetMatchWord(EnDictionary word)
    {
        return RandomGetMatchWord(new WordCache()
        {
            Word = word.Word,
        });
    }
    private void RandomNewWord()
    {
        CurrentWord = _wordDictionary.Value.RandomGetOne();
    }

    private string GetCurWordDesc(KouContext context)
    {
        if (CurrentWord == null) return "【出错，当前无单词】";
        var exp = EnDictionary.SingleOrDefault(p => p.Word == CurrentWord.Word, context)?.Definition;
        return $"【{CurrentWord.Word}】{exp?.Be($"，意思是：{exp}")}";
    }

    private string RoundReword()
    {
        if (RoundCount % 10 == 0)
        {
            RewordPool = (RewordPool * 1.1).Ceiling();
            return $"\n另外，当前已经第{RoundCount}轮，奖池增益10%，目前：{CurKouGlobalConfig?.CoinFormat(RewordPool)}";
        }

        return "";
    }


    public override RoomReaction PromptGetPrompt(PlatformUser speaker, string content)
    {
        if (CurrentWord != null)
        {
            var s = RandomGetMatchWord(CurrentWord);
            if(s == null) return "无法获取到提示";
            using var context = new KouContext();
            var info = GetInfoFromDb(s, context);
            RewordPool = (RewordPool*0.99).Ceiling();
            return
                $"提示：{info.Word.First()}开头的，意思是{info.Definition}\n当前奖池已减少1%，目前：{CurKouGlobalConfig?.CoinFormat(RewordPool)}";
        }

        return "还未开始游戏";
    }

    public override RoomReaction DescHasStartedGame() => $"单词接龙已经开始啦，现在请接【{CurrentWord?.Word}】";

    public override RoomReaction PromptStartGame(PlatformUser speaker, string content)
    {
        RandomNewWord();
        if (CurrentWord == null) return "单词数据缺失";
        RecordNewRound();
        StartAutoClose();
        using var context = new KouContext();
        return $"单词接龙开始啦，{(RewordPool != 0).BeIfTrue($"当前奖池{RewordPool}枚硬币，")}我先来一个：{GetCurWordDesc(context)}";
    }

    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        line = line.ToLower();
        if (CurrentWord == null)
        {
            return DescNotStartGame();
        }
        if (CurrentWord != null)
        {
            RecordUserTakePartIn(speaker);
            if (UserPreviousUseWord.Contains(line)) return $"之前已经使用过这个单词咯，换一个把！当前【{CurrentWord.Word}】";
            lock (_lock)
            {
                using var context = new KouContext();
                var word = EnDictionary.SingleOrDefault(p => p.Word == line, context);
                if (word == null)
                {
                    return $"恕我孤陋寡闻，不认识这个单词。当前【{CurrentWord.Word}】";
                }

                UserPreviousUseWord.Add(line);
                if(word.Word.First() == CurrentWord.Word.Last())
                {
                    RecordLastRoundWinner(speaker);
                    CurrentWord =
                        RandomGetMatchWord(word);
                    if (CurrentWord == null)
                    {
                        RewordPool += 1000;
                        RandomNewWord();
                        return $"我居然接不上，你太厉害了，奖池已增加1000个硬币！我再出一个新的：{GetCurWordDesc(context)}{RoundReword()}";
                    }
                    RecordNewRound();
                    return $"{line}{word.Definition?.Be($"，意思是{word.Definition}")}\n嗯...不错，那我接{GetCurWordDesc(context)}{RoundReword()}";
                }
                return $"虽然是单词，但是开头需要是{CurrentWord.Word.Last()}呢。当前【{CurrentWord.Word}】";
            }
        }

     

        return false;
    }
}