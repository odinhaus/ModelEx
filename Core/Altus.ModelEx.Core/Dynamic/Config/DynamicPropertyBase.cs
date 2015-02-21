using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Data;
using Altus.Core.Dynamic.Config;

namespace Altus.Core.Dynamic.Config
{
    public class DynamicPropertyBase
    {
        DynamicPropertyConfig _config;
        public DynamicPropertyBase() { }        

        public DynamicPropertyBase(string instanceType, DynamicPropertyConfig propertyConfig, ushort instanceId, ushort objectId, string value)
        {
            this.InstanceType = instanceType;
            this.Config = propertyConfig;
            this.PropertyExId = propertyConfig.Id;
            this.InstanceId = instanceId;
            this.ObjectId = objectId;
            this.Value = value;            
        }

        public DynamicPropertyBase(string instanceType, int propertyConfigId, ushort instanceId, ushort objectId, string value)
        {
            this.InstanceType = instanceType;
            this.PropertyExId = propertyConfigId;
            this.InstanceId = instanceId;
            this.ObjectId = objectId;
            this.Value = value;
        }

        public DynamicPropertyBase(string instanceType, string propName, ushort instanceId, ushort objectId, string value)
        {
            this.InstanceType = instanceType;
            this.PropertyExId = GetPropertyId(propName);
            this.InstanceId = instanceId;
            this.ObjectId = objectId;
            this.Value = value;
        }

        [StorageFieldMapping("InstanceType", StorageFieldModifiers.Key)]
        [StorageFieldWriteConstraint(WriteConstraintType.IsNotNull)]
        public string InstanceType { get; set; }

        [StorageFieldMapping("InstanceId", StorageFieldModifiers.Key)]
        [StorageFieldWriteConstraint(WriteConstraintType.IsNotNull)]
        public ushort InstanceId { get; set; }

        [StorageFieldMapping("ObjectId", StorageFieldModifiers.Key)]
        [StorageFieldWriteConstraint(WriteConstraintType.IsNotNull)]
        public ushort ObjectId { get; set; }

        [StorageFieldMapping("PropertyExId", StorageFieldModifiers.Key)]
        [StorageFieldWriteConstraint(WriteConstraintType.IsNotNull)]
        public int PropertyExId { get; set; }

        [StorageFieldMapping("Value")]
        public string Value { get; set; }
        
        public DynamicPropertyConfig Config
        {
            get
            {
                return _config;
            }
            set
            {
                _config = value;
            }
        }

        private int GetPropertyId(string name)
        {
            DynamicPropertyConfig dp = Altus.Core.Data.DataContext.Default.Select<DynamicPropertyConfig>(new { Name = name }).FirstOrDefault<DynamicPropertyConfig>();
            return dp == null ? 0 : dp.Id;
        }

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
                this.Value == newobj.Value;
            return retVal;
        }
    }
}
