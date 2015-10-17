using System;
using System.Windows.Media;

namespace Visualizer.Extensions
{
    public static class Helpers
    {
        private static Random _rng;

        public static Color RandomColor(bool grayscale = false)
        {
            if(_rng == null)
                _rng = new Random(DateTime.Now.GetHashCode());

            var c = new Color
            {
                R = (byte)(_rng.Next() % 255),
                G = (byte)(_rng.Next() % 255),
                B = (byte)(_rng.Next() % 255),
                A = 255
            };

            if (grayscale)
                c.R = c.G = c.B;

            return c;
        }
    }
}
