using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Data;
using Altus.Core.Dynamic.Config;

namespace Altus.Core.Dynamic.Config
{
    [StorageMapping("PropertyExText")]
    public class DynamicPropertyText : DynamicPropertyBase
    {
        public DynamicPropertyText() { }     
        public DynamicPropertyText(string instanceType, DynamicPropertyConfig propertyConfig, ushort instanceId, ushort objectId, string value)
            :base(instanceType, propertyConfig, instanceId, objectId, value){}

        public DynamicPropertyText(string instanceType, int propertyConfigId, ushort instanceId, ushort objectId, string value)
            : base(instanceType, propertyConfigId, instanceId, objectId, value) { }
        
        public override bool Equals(object obj)
        {
            bool retVal = false;
            DynamicPropertyBase newobj = obj as DynamicPropertyBase;
            if (newobj == null) return false;
            if (this == newobj) return true;

            retVal =
                this.ObjectId == newobj.ObjectId &&
                this.InstanceType == newobj.InstanceType &&
                this.InstanceId == newobj.InstanceId &&
                this.PropertyExId == newobj.PropertyExId &&
                this.Value.Replace("'", "''") == newobj.Value;
            return retVal;
        }
    }
}
