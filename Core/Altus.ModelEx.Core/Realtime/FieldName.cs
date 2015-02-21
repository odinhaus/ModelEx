using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Altus.Core.Realtime
{
    [DataContract()]
    public struct FieldName : IEqualityComparer<Field>
    {
        [DataMember()]
        public string Name;
        public string Object;
        public static FieldName Create(string qualifiedName)
        {
            string[] parts = qualifiedName.Split('.');
            FieldName fn = new FieldName();
            fn.Object = parts[0];
            fn.Name = parts[1];
            return fn;
        }

        public bool IsValid { get { return !string.IsNullOrEmpty(Name) ; } }

        public bool Equals(Field x, Field y)
        {
            return x.Name.Equals(y.Name, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(Field obj)
        {
            return obj.GetHashCode();
        }
    }
}
