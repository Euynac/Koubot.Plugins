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
    public const string Help = "手速竞技，每轮不定时放出图片，玩家需要输入图片中的内容，最先输入的计分。开始游戏后，默认5分钟后结算成绩。\n" +
                               "此游戏基于Kou会话房间，消耗房间入场券入场，入场券全部投入奖池。当前房间信息及帮助通过【/room】查看。\n" +
                               "成功创建房间后，通过房间钥匙（前缀）加入以及后续交互，默认钥匙是空格，开始后直接空格+答案参与游戏\n" +
                               "【 开始】开始游戏\n" +
                               "【 排行榜】查看战况\n" +
                               "【 结束】房主可提前结算游戏，按排名分配奖池硬币\n";
    private readonly object _lock = new ();

    public class RecordContent
    {
        public string Word { get; set; }
        public string Explanation { get; set; }
    }

    public KouMessage? CurImage { get; set; }
    public RecordContent? CurAnswer { get; set; }
    public DateTime CurRoundStartTime { get; set; }
    public bool HasFinishCurRound { get; set; }
    public int RoundCount { get; set; }
    public SpeedTyperGameRoom(string roomName, PlatformUser ownerUser, PlatformGroup? roomGroup, int? fee) : base(roomName, ownerUser, roomGroup, fee)
    {
        RoomHelp = Help;
        UserCorrectEvent += achievement =>
        {
            HasFinishCurRound = true;
            achievement.SuccessTimes++;
            achievement.TotalConsumeTime = achievement.TotalConsumeTime.Add(DateTime.Now - CurRoundStartTime);
        };

        NextRoundEvent += (sender, args) =>
        {
            HasFinishCurRound = false;
            RoundCount++;
            CurRoundStartTime = DateTime.Now;
        };
    }

    public override RoomReaction PromptStartGame(PlatformUser speaker, string content)
    {
        if(CurAnswer != null) return DescHasStartedGame();
        NewRound();
        if (CurAnswer == null) return "数据库缺失，开始失败";
        return "游戏开始啦，最先输入图片中的文字的计一分，将随机一小段时间内放出图片";
    }
    public void NewRound()
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
            RecordNextRound();
            KouCommand.ResponseThoughPipe(CurImage, CurKouGlobalConfig?.BotPlatformUser, OwnerUser, OwnerGroup);
        });
    }

    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        if (CurImage == null || CurAnswer == null)
        {
            return DescNotStartGame();
        }
        if (!HasFinishCurRound)
        {
            lock (_lock)
            {
                if (HasFinishCurRound) return false;
                if (line == CurAnswer.Word)
                {
                    RecordLastRoundWinner(speaker);
                    NewRound();
                    var cost = DateTime.Now - CurRoundStartTime;
                    return $"{speaker.Name}{(cost.TotalSeconds < 4).IIf("仅花", "耗费")}{cost.ToZhFormatString()}拿下一分{(cost.TotalSeconds < 2).BeIfTrue((cost.TotalSeconds < 1).IIf("，一定是开了挂吧？？", "，什么神仙"))}";
                }
            }
        }

        return false;
    }
}