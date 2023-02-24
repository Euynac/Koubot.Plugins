using System;
using Koubot.SDK.System;
using Koubot.SDK.System.Image;
using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using KouFunctionPlugin.Models;

namespace KouDictionary;

public class SpeedTyperLeaderBoard : IGameRoomLeaderBoardInfo<SpeedTyperLeaderBoard>
{
    public int SuccessTimes { get; set; }
    public TimeSpan TotalConsumeTime { get; set; } = new();
    public TimeSpan CorrectAverageTime => new(TotalConsumeTime.Ticks / (SuccessTimes.BeNullIfDefault() ?? 1));
    public int CompareTo(SpeedTyperLeaderBoard other)
    {
        return this.CompareToObjDesc(SuccessTimes, other.SuccessTimes, out var result)?
            .CompareToObjAsc(TotalConsumeTime, other.TotalConsumeTime, out result) == null ? result : 0;
    }

    public KouMessageTableElement GetUserLeaderBoardInfo(PlatformUser user)
    {
        var element = new KouMessageTableElement("参赛人员", "抢占数", "平均耗时");
        element.AddRow(user.Name ?? "??",  SuccessTimes.ToString(),CorrectAverageTime.ToZhFormatString());
        return element;
    }
}

public class SpeedTyperGameRoom : KouGameRoom<SpeedTyperLeaderBoard>
{
    public const string Help = "手速竞技，每轮不定时放出图片，玩家需要输入图片中的内容，最先输入的计分。开始游戏后，默认5分钟后结算成绩。";
    private readonly object _roundLock = new ();

    public class RecordContent
    {
        public string Word { get; set; }
        public string Explanation { get; set; }
    }

    public KouMessage? CurImage { get; set; }
    public RecordContent? CurAnswer { get; set; }
    public SpeedTyperGameRoom(string roomName, PlatformUser ownerUser, PlatformGroup? roomGroup, int? fee) : base(roomName, ownerUser, roomGroup, fee)
    {
        RoomHelp = Help;
        UserCorrectEvent += achievement =>
        {
            CurRoundHasEnd = true;
            achievement.SuccessTimes++;
            achievement.TotalConsumeTime = achievement.TotalConsumeTime.Add(DateTime.Now - CurRoundStartTime);
        };
    }

    public override RoomReaction PromptStartGame(PlatformUser speaker, string content)
    {
        if(CurAnswer != null) return DescHasStartedGame();
        NewRound();
        if (CurAnswer == null) return "数据库缺失，开始失败";
        return "游戏开始啦，最先输入图片中的文字的计一分，将随机一小段时间内放出图片";
    }
    public override void NewRound(bool isRenew = false)
    {
        var fromEn = 0.5.ProbablyTrue();
        if (fromEn)
        {
            var en = EnDictionary.RandomGetOne(p => p.Population > 0);
            if(en == null) return;
            CurAnswer = new RecordContent(){Explanation = en.Definition, Word = en.Word};
        }
        else
        {
            var zh = IdiomDictionary.RandomGetOne(p => p.Word.Length == 4);
            if(zh == null) return;
            CurAnswer = new RecordContent(){Explanation = zh.Explanation, Word = zh.Word};
        }
        CurImage = new KouMutateImage(CurAnswer.Word).SaveTemporarily();
        KouTaskDelayer.DelayInvoke(RandomTool.GetInt(5000, 15000), () =>
        {
            if(HasClosed) return;
            RecordNewRound(isRenew);
            
            RoomBroadcast(CurImage);
        });
    }

    public override RoomReaction PromptSayWhenRoundStart(PlatformUser speaker, string content)
    {
        if (content == CurAnswer!.Word)
        {
            RecordLastRoundWinner(speaker);
            NewRound();
            var cost = DateTime.Now - CurRoundStartTime;
            return $"{speaker.Name}{(cost.TotalSeconds < 4).IIf("仅花", "耗费")}{cost.ToZhFormatString()}拿下一分{(cost.TotalSeconds < 2).BeIfTrue((cost.TotalSeconds < 1).IIf("，一定是开了挂吧？？", "，什么神仙"))}";
        }

        return false;
    }

    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        return false;
    }
}