using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core
{
    public static class ArrayEx
    {
        public static T[] Clone<T>(this T[] array)
        {
            T[] ret = new T[array.Length];
            array.CopyTo(ret, 0);
            return ret;
        }

        public static T[] Combine<T>(this T[] array, T[] combine)
        {
            if (array.Length == 0) return combine;
            if (combine == null || combine.Length == 0) return array;
            List<T> dest = new List<T>(array);
            foreach (T c in combine)
            {
                if (!dest.Contains(c))
                    dest.Add(c);
            }
            return dest.ToArray();
        }
    }
}
