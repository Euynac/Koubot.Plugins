using System;

namespace KouFunctionPlugin
{
    internal class LotteryGenerator
    {
        private readonly int _lotteryMaxValue; //彩票从什么号码结束
        private int _lotteryAmount; //彩票数
        private readonly int _lotteryMinValue; //彩票从什么号码开始
        private int[] _bingoArray; //中奖的号码 从1开始
        private readonly Random _ran;

        public LotteryGenerator(int lotteryAmount, int minValue, int maxValue)
        {
            _lotteryMaxValue = maxValue;
            _lotteryMinValue = minValue;
            _lotteryAmount = lotteryAmount;
            _ran = new Random();
        }

        public int[] DrawLottery()
        {
            int total = _lotteryMaxValue - _lotteryMinValue + 1;
            if (_lotteryAmount > total) _lotteryAmount = total;
            _bingoArray = new int[_lotteryAmount];
            for (int i = 0; i < _lotteryAmount; i++)
            {
                int bingo;
                do
                {
                    bingo = _ran.Next(_lotteryMinValue, _lotteryMaxValue + 1);
                } while (!IsContain(bingo));
                _bingoArray[i] = bingo;
            }
            return _bingoArray;
        }

        private bool IsContain(int value)//判断是否重复
        {
            foreach (int bingo in _bingoArray)
            {
                if (bingo == 0) break;//0说明还没生成
                if (bingo == value) return false;
            }
            return true;
        }
    }
}