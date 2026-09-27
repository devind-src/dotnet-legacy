using System;
using System.Collections.Generic;

namespace SyncNet.Routing
{
    // pilih satu item secara acak menurut bobotnya (mode LOAD_BALANCE).
    // stateless, jadi aman dipakai banyak request dan banyak instance sekaligus;
    // pembagian mendekati bobot pada volume besar.
    internal static class WeightedPicker
    {
        public static T Pick<T>(IReadOnlyList<T> items, Func<T, int> weight) where T : class
        {
            int total = 0;
            foreach (var item in items)
                total += Math.Max(0, weight(item));

            if (total <= 0) return null;

            int roll = Random.Shared.Next(total);
            foreach (var item in items)
            {
                int w = Math.Max(0, weight(item));
                if (roll < w) return item;
                roll -= w;
            }

            return null;
        }
    }
}
