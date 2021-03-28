namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// Arcaea的数据模块
    /// </summary>
    public class ArcaeaData
    {
        static ArcaeaData()
        {
        }
        /// <summary>
        /// 根据分数和谱面定数计算ptt
        /// </summary>
        /// <param name="constant"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public static double CalSongScorePtt(double constant, int score)
        {
            if (score >= 10000000) return constant + 2;
            else if (score > 9800000) return constant + 1 + (score - 9800000) / 200000.0;
            double value = constant + (score - 9500000) / 300000.0;
            return value < 0 ? 0 : value;
        }
        /// <summary>
        /// 根据ptt和分数计算谱面定数
        /// </summary>
        /// <returns></returns>
        public static double CalSongChartConstant(double ptt, int score)
        {
            if (score >= 10000000) return ptt - 2;
            else if (score > 9800000) return ptt - (1 + (score - 9800000) / 200000.0);
            double value = ptt - ((score - 9500000) / 300000.0);
            return value < 0 ? 0 : value;
        }
    }
}
