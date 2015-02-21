using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;

namespace Altus.Core.Processing
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple= false, Inherited=true)]
    public class OptionalAttribute : Attribute
    {
        public OptionalAttribute(Type defaultValueType, params object[] ctorArgs)
        {
            DefaultValue = Activator.CreateInstance(defaultValueType, ctorArgs);
        }

        public OptionalAttribute(string defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(byte defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(char defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(uint defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(int defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(ulong defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(long defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(float defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(double defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public OptionalAttribute(decimal defaultValue)
        {
            this.DefaultValue = defaultValue;
        }

        public object DefaultValue { get; private set; }
    }
}
