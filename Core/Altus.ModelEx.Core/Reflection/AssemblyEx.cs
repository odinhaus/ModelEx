using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Altus.Core.Reflection
{
    public static class AssemblyEx
    {
        public static T GetCustomAttribute<T>(this Assembly assembly) where T : Attribute
        {
            return assembly.GetCustomAttributes(true).Where(ca => ca.GetType().Equals(typeof(T))).FirstOrDefault() as T;
        }

        public static T GetCustomAttribute<T>(this Assembly assembly, bool inherit) where T : Attribute
        {
            return assembly.GetCustomAttributes(inherit).Where(ca => ca.GetType().Equals(typeof(T))).FirstOrDefault() as T;
        }
    }

    public static class TypeEx
    {
        public static T GetCustomAttribute<T>(this Type assembly) where T : Attribute
        {
            return assembly.GetCustomAttributes(true).Where(ca => ca.GetType().Equals(typeof(T))).FirstOrDefault() as T;
        }

        public static T GetCustomAttribute<T>(this Type assembly, bool inherit) where T : Attribute
        {
            return assembly.GetCustomAttributes(inherit).Where(ca => ca.GetType().Equals(typeof(T))).FirstOrDefault() as T;
        }
    }
}
