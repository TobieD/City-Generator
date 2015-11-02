using System;
using System.Collections.Generic;
using System.Linq;

namespace CityGenerator.Extensions
{
    public static class Extensions
    {
        private static Random _rng;

        public static T GetRandomValue<T>(this IEnumerable<T> list)
        {
            SeedRng();

            var count = list.Count();
            return list.ElementAt(_rng.Next(count));
        }

        private static void SeedRng()
        {
            if(_rng == null)
                _rng = new Random(DateTime.Now.GetHashCode());
        }


    }
}
