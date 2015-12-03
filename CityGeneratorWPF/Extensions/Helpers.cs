using System;
using System.Windows.Media;

namespace CityGeneratorWPF.Extensions
{
    public static class Extensions
    {
        private static Random _rng;

        public static Color RandomColor(bool grayscale = false, int max = 255)
        {
            if(_rng == null)
                _rng = new Random(DateTime.Now.GetHashCode());

           return RandomColor_Simple(max);

        }

        private static Color RandomColor_Simple(int max)
        {

            var c = new Color
            {
                R = (byte)(_rng.Next() % max),
                G = (byte)(_rng.Next() % max),
                B = (byte)(_rng.Next() % max),
                A = 255
            };

            //c.R = c.G = c.B;

            return c;
        }

        public static Color GetRandomColorOffset(this Color c, double max)
        {
            if (_rng == null)
                _rng = new Random(DateTime.Now.GetHashCode());


            return Color.FromArgb(255,

                (byte)FloatToByte((ByteToFloat(c.R) + _rng.NextDouble() * 2 * max - max)),
                (byte)FloatToByte((ByteToFloat(c.G) + _rng.NextDouble() * 2 * max - max)),
               (byte)FloatToByte((ByteToFloat(c.B) + _rng.NextDouble() * 2 * max - max))
            );
        }

        public static double ByteToFloat(int byteValue)
        {
            return byteValue/256.0;
        }

        public static int FloatToByte(double floatValue)
        {

            var byteVal = (int)(floatValue*256);

            return SaturateByte(byteVal);

        }

        private static int SaturateByte(int byteVal)
        {
            if (byteVal > 255)
                byteVal = 255;

            else if (byteVal < 0)
                byteVal = 0;

            return byteVal;
        }

        public static T Clamp<T>(T value, T min, T max) where T: IComparable
        {

            if (value.CompareTo(min) < 0)
                value = min;

            else if (value.CompareTo(max) > 0)
                value = max;

            return value;
        }
    }
}
