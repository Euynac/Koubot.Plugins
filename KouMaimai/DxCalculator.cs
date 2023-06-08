using System;

namespace KouGamePlugin.Maimai
{
    public static class DxCalculator
    {
        public static int CalSongRating(double achievement, double ds, bool? b50 = null)
        {
            if (achievement <= 1.01) achievement *= 100.0;
            if (achievement < 0) return 0;
            var baseRa = b50 switch
            {
                true => achievement switch
                {
                    < 50 => 7.0,
                    < 60 => 8.0,
                    < 70 => 9.6,
                    < 75 => 11.2,
                    < 80 => 12.0,
                    < 90 => 13.6,
                    < 94 => 15.2,
                    < 97 => 16.8,
                    < 98 => 20.0,
                    < 99 => 20.3,
                    < 99.5 => 20.8,
                    < 100 => 21.1,
                    < 100.5 => 21.6,
                    _ => 22.4
                },
                _ => achievement switch
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
                }
            };

            return (int)Math.Floor(ds * (Math.Min(achievement, 100.5) / 100) * baseRa);
        }
    }
}