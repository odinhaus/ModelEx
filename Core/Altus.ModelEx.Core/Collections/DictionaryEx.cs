using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Data;

namespace System.Collections
{
    public static class DictionaryEx
    {
        public static bool Find<U, T>(this Dictionary<U, T> dictionary, U key, FindMode mode, out T found) where U : IComparable
        {
            bool ret = false;
            found = default(T);
            if (mode == FindMode.Exact)
            {
                try
                {
                    found = dictionary[key];
                    ret = true;
                }
                catch { }
            }
            else if (mode == FindMode.Before)
            {
                List<U> keys = dictionary.Keys.ToList();
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i].CompareTo(key) > 0)
                    {
                        if (i > 0)
                        {
                            found = dictionary[keys[i - 1]];
                            ret = true;
                        }
                        break;
                    }
                }
            }
            else if (mode == FindMode.After)
            {
                List<U> keys = dictionary.Keys.ToList();
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i].CompareTo(key) > 0)
                    {
                        found = dictionary[keys[i]];
                        ret = true;
                        break;
                    }
                }
            }
            else if (mode == FindMode.ExactOrBefore)
            {
                List<U> keys = dictionary.Keys.ToList();
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    int compare = keys[i].CompareTo(key);
                    if ( compare > 0)
                    {
                        if (i > 0)
                        {
                            found = dictionary[keys[i - 1]];
                            ret = true;
                        }
                        break;
                    }
                    else if (compare == 0)
                    {
                        found = dictionary[keys[i]];
                        ret = true;
                    }
                }
            }
            else if (mode == FindMode.ExactOrAfter)
            {
                List<U> keys = dictionary.Keys.ToList();
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    if (keys[i].CompareTo(key) >= 0)
                    {
                        found = dictionary[keys[i]];
                        ret = true;
                        break;
                    }
                }
            }
            return ret;
        }
    }
}
