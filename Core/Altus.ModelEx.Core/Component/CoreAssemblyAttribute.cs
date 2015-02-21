﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Altus.Core.Component
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited= false, AllowMultiple=false)]
    public class CoreAssemblyAttribute : Attribute
    {
        public CoreAssemblyAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; private set; }
    }
}
