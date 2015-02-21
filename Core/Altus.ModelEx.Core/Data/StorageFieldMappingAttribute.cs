﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Data
{
    [Flags]
    public enum StorageFieldModifiers
    {
        None = 0,
        Key = 1,
        AutoGenerated = 2,
        Unique = 4
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false, Inherited=true)]
    public class StorageFieldMappingAttribute : Attribute
    {
        public StorageFieldMappingAttribute() : this(null, StorageFieldModifiers.None) { }

        public StorageFieldMappingAttribute(string storageFieldName)
            : this(storageFieldName, StorageFieldModifiers.None)
        {

        }

        public StorageFieldMappingAttribute(string storageFieldName, params StorageFieldModifiers[] modifiers)
        {
            this.FieldName = storageFieldName;
            foreach (StorageFieldModifiers mods in modifiers)
                this.FieldModifiers |= mods;
        }

        public string FieldName { get; private set; }
        public StorageFieldModifiers FieldModifiers { get; private set; }
    }
}
