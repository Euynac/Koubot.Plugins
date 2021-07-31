using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Koubot.Tool.Extensions;

namespace KouGamePlugin
{
    public class TwentyFour
    {
    
        private List<int> curCalArr = new() {0,0,0,0};
        private const double Loss = 1e-7;
        private List<Num> _answers = new();
        private int _target;
        private bool _onlyNeedOneAnswer;
        public int CalCount { get; private set; }

        public bool TryTest(int target, List<int> arr, out string answer)
        {
            curCalArr = arr;
            _target = target;
            answer = null;
            if (TryCalTo(_target, out List<string> answerList))
            {
                answer = answerList[0];
                return true;
            }
            return false;
        }
        public bool TryCalUse(int target, List<int> arr, out List<string> answer)
        {
            curCalArr = arr;
            _target = target;
            return TryCalTo(_target, out answer);
        }

        public bool TryCalTo(int target, out List<string> answer)
        {
            _target = target;
            answer = null;
            _answers = new();
            CalCount = 0;
            if (ToTargetUse(curCalArr.Select(n=>new Num(n)).ToList()))
            {
                answer = _answers.Select(n => n.ToString()).Distinct().ToList();
                return true;
            }

            return false;
        }

        public class Num
        {
            private readonly Num _x;
            private readonly Num _y;
            private readonly Operator _way;
            private bool _notComeFromCal;
            public enum Operator
            {
                [Description("+")]
                Add,
                [Description("-")]
                Sub,
                [Description("*")]
                Multiply,
                [Description("/")]
                Divide
            }

            public Num(Num x, Num y, Operator way, double value)
            {
                _x = x;
                _y = y;
                Value = value;
                _way = way;
            }

            public Num(int num)
            {
                Value = num;
                _notComeFromCal = true;
            }

            public double Value { get; }

            public override string ToString()
            {
                if (_notComeFromCal) return ((int)Value).ToString();
                string result = $"{_x}{_way.GetDescription()}{_y}";
                //if (_way.EqualsAny(Operator.Add, Operator.Sub))
                    return $"({result})";
                return result;
            }
        }



        public bool ToTargetUse(List<Num> numList)
        {
            if (numList.Count == 1)
            {
                CalCount++;
                if (Math.Abs(numList[0].Value - _target) < Loss)
                {
                    _answers.Add(numList[0]);
                    return true;
                }

                return false;
            }
            for (int i = 0; i < numList.Count-1; i++)
            {
                for (int j = i + 1; j < numList.Count; j++)
                {
                    Num x = numList[i];
                    Num y = numList[j];
                    List<Num> newList = new List<Num>();
                    for (int k = 0; k < numList.Count; k++)
                    {
                        if(k == i || k == j) continue;
                        newList.Add(numList[k]);
                    }

                    for (int o = 0; o < 6; o++)
                    {
                        Num combinedNum = null;
                        switch (o)
                        {
                            case 0:
                                combinedNum = new Num(x, y, Num.Operator.Add, x.Value + y.Value);
                                break;
                            case 1:
                                combinedNum = new Num(x, y, Num.Operator.Sub, x.Value - y.Value);
                                break;
                            case 2:
                                combinedNum = new Num(y, x, Num.Operator.Sub, y.Value - x.Value);
                                break;
                            case 3:
                                combinedNum = new Num(x, y, Num.Operator.Multiply, x.Value * y.Value);
                                break;
                            case 4:
                                if (y.Value == 0) continue;
                                combinedNum = new Num(x, y, Num.Operator.Divide, x.Value / y.Value);
                                break;
                            case 5:
                                if (x.Value == 0) continue;
                                combinedNum = new Num(y, x, Num.Operator.Divide, y.Value / x.Value);
                                break;
                        }

                        newList.Add(combinedNum);
                        if (ToTargetUse(newList) && _onlyNeedOneAnswer) return true;
                        newList.RemoveAt(newList.Count - 1);
                    }
                }
            }

            return _answers.Count != 0;
        }
        //private bool CanGetWanted(int at, double wanted)
        //{
        //    if (at >= curCalArr.Length) return false;
        //    if (at == curCalArr.Length - 1)
        //    {
        //        if (Math.Abs(curCalArr[at] - wanted) < loss)
        //        {
        //            sequence = curCalArr[at].ToString();
        //            return true;
        //        }

        //        return false;

        //    }

        //    if (CanGetWanted(at + 1, curCalArr[at] + wanted))
        //    {
        //        sequence = $"({sequence}-{curCalArr[at]})";
        //        return true;
        //    }

        //    if (CanGetWanted(at + 1, curCalArr[at] - wanted))
        //    {
        //        sequence = $"({curCalArr[at]}-{sequence})";
        //        return true;
        //    }

        //    if (CanGetWanted(at + 1, wanted - curCalArr[at]))
        //    {
        //        sequence = $"({curCalArr[at]}+{sequence})";
        //        return true;
        //    }
     
        //    if (CanGetWanted(at + 1, curCalArr[at] * wanted))
        //    {
        //        sequence = $"{sequence}/{curCalArr[at]}";
        //        return true;
        //    }

        //    if (CanGetWanted(at + 1, wanted/curCalArr[at]))
        //    {
        //        sequence = $"{curCalArr[at]}*{sequence}";
        //        return true;
        //    }

        //    if (wanted == 0) return false;
        //    if (CanGetWanted(at + 1, curCalArr[at]/wanted))
        //    {
        //        sequence = $"{curCalArr[at]}*{sequence}";
        //        return true;
        //    }
        //    return false;
        //}
    }
}