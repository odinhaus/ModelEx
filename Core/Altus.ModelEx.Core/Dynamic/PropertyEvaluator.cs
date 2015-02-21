using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.dynamic
{
    public class PropertyEvaluator : IDynamicPropertyEvaluator
    {
        public PropertyEvaluator() 
        {
            this.Gettor = new Func<object>(this._Gettor);
            this.Settor = new Action<object>(this._Settor);
        }

        public Func<object> Gettor
        {
            get;
            private set;
        }

        public Action<object> Settor
        {
            get;
            private set;
        }

        private object _Gettor()
        {
            @Gettor
        }

        private void _Settor(object value)
        {
            @Settor
        }
    }
}
