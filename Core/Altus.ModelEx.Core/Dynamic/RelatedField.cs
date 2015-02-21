using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Realtime;

namespace Altus.Core.Dynamic
{
    public class RelatedField
    {
        public RelatedField(Field field, string relationshipType)
        {
            this.Field = field;
            this.RelationshipType = relationshipType;
        }

        public Field Field { get; private set; }
        public string RelationshipType { get; private set; }
        public bool IsWired { get; set; }
        public DynamicField DynamicField { get; set; }
    }
}
