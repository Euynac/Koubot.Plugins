using System;

namespace KouGamePlugin.Maimai
{
    public static class DxCalculator
    {
        public static int CalSongRating(double rate, double songConstant)
        {
            if (rate <= 1.01) rate *= 100.0;
            if (rate < 0) return 0;
            var l = rate switch
            {
                < 50 => 0,
                < 60 => 5,
                < 70 => 6,
                < 75 => 7,
                < 80 => 7.5,
                < 90 => 8,
                < 94 => 9,
                < 97 => 10.5,
                < 98 => 12.5,
                < 99 => 12.75,
                < 99.5 => 13,
                < 100 => 13.25,
                < 100.5 => 13.5,
                _ => 14.0
            };
            return (int)Math.Floor(songConstant * (Math.Min(rate, 100.5) / 100) * l);
        }
    }
}