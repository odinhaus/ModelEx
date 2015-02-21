using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Altus.Core.Data
{
    public class StorageMapping
    {
        static Dictionary<Type, StorageMapping> _cached = new Dictionary<Type, StorageMapping>();

        public static StorageMapping CreateFromInstance(object entity)
        {
            return CreateFromType(entity.GetType());
        }

        public static StorageMapping CreateFromType(Type type)
        {
            lock (_cached)
            {
                if (_cached.ContainsKey(type)) return _cached[type];
                return CreateInternal(type);
            }
        }

        private static StorageMapping CreateInternal(Type entityType)
        {
            StorageMappingAttribute sma = (StorageMappingAttribute)entityType.GetCustomAttributes(typeof(StorageMappingAttribute), true).FirstOrDefault();
            StorageMapping sm = new StorageMapping();
            sm.EntityType = entityType;

            if (sma == null)
            {
                sm.IsMapped = false;
            }
            else
            {
                sm.StorageEntity = sma.EntityName;
                GetMappedMembers(entityType, sm);
            }

            _cached.Add(entityType, sm);
            return sm;
        }

        private static void GetMappedMembers(Type entityType, StorageMapping sm)
        {
            Type T = entityType;
            MemberInfo[] members = T.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            List<StorageFieldMapping> mappings = new List<StorageFieldMapping>();
            
            foreach (MemberInfo member in members.Where(mi => mi is PropertyInfo || mi is FieldInfo))
            {
                object[] attribs = member.GetCustomAttributes(true);
                bool isMapped = false;
                StorageFieldModifiers modifiers = StorageFieldModifiers.None;
                List<Func<object, bool>> constraints = new List<Func<object, bool>>();
                string storageField = null;

                foreach (object attrib in attribs.Where(a => a is StorageFieldWriteConstraintAttribute || a is StorageFieldMappingAttribute))
                {
                    isMapped = true;
                    if (attrib is StorageFieldWriteConstraintAttribute)
                    {
                        constraints.Add(CreateConstraintHandler(entityType, member, attrib as Attribute));
                    }
                    else
                    {
                        modifiers = ((StorageFieldMappingAttribute)attrib).FieldModifiers;
                        storageField = string.IsNullOrEmpty(((StorageFieldMappingAttribute)attrib).FieldName) ? member.Name : ((StorageFieldMappingAttribute)attrib).FieldName;
                    }
                }

                if (isMapped)
                {
                    StorageFieldMapping sfm = new StorageFieldMapping(member, storageField, constraints.ToArray(), modifiers);
                    mappings.Add(sfm);
                }
            }
            sm.MappedMembers = mappings.ToArray();
        }

        private static Func<object, bool> CreateConstraintHandler(Type entityType, MemberInfo member, Attribute attrib)
        {
            switch (((StorageFieldWriteConstraintAttribute)attrib).ConstraintType)
            {
                case WriteConstraintType.Equals:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            object entityValue = GetMemberValue(value, member);
                            return entityValue == ((StorageFieldWriteConstraintAttribute)attrib).ConstraintValue;
                        });
                    }
                case WriteConstraintType.GreaterThan:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            IComparable compareValue = ((StorageFieldWriteConstraintAttribute)attrib).ConstraintValue;
                            return entityValue == null ? false : entityValue.CompareTo(compareValue) > 0;
                        });
                    }
                case WriteConstraintType.GreaterThanOrEqual:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            IComparable compareValue = ((StorageFieldWriteConstraintAttribute)attrib).ConstraintValue;
                            return (entityValue == null && compareValue != null) ? false : (compareValue == null ? true : entityValue.CompareTo(compareValue) >= 0);
                        });
                    }
                case WriteConstraintType.LessThan:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            IComparable compareValue = ((StorageFieldWriteConstraintAttribute)attrib).ConstraintValue;
                            return compareValue == null ? false : entityValue.CompareTo(compareValue) < 0;
                        });
                    }
                case WriteConstraintType.LessThanOrEqual:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            IComparable compareValue = ((StorageFieldWriteConstraintAttribute)attrib).ConstraintValue;
                            return (entityValue != null && compareValue == null) ? false : (entityValue == null ? true : entityValue.CompareTo(compareValue) >= 0);
                        });
                    }
                case WriteConstraintType.IsNull:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            return entityValue == null;
                        });
                    }
                case WriteConstraintType.IsNotNull:
                    {
                        return new Func<object, bool>(delegate(object value)
                        {
                            IComparable entityValue = GetMemberValue(value, member);
                            return entityValue != null;
                        });
                    }
                case WriteConstraintType.Custom:
                    {
                        MethodInfo mi = entityType.GetMethod(((StorageFieldCustomWriteConstraintAttribute)attrib).ConstraintValue.ToString(),
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                        return new Func<object, bool>(delegate(object value)
                        {
                            return (bool)mi.Invoke(value, new object[] { value });
                        });
                    }
                default: return null;
            }
        }

        private static IComparable GetMemberValue(object entity, MemberInfo member)
        {
            if (member is PropertyInfo)
                return ((PropertyInfo)member).GetValue(entity, null) as IComparable;
            else if (member is PropertyInfo)
                return ((FieldInfo)member).GetValue(entity) as IComparable;
            else
                return null;
        }
        private StorageMapping() { }

        public Type EntityType { get; private set; }
        public string StorageEntity { get; private set; }
        public bool IsMapped { get; private set; }
        public StorageFieldMapping[] MappedMembers { get; private set; }


        public class StorageFieldMapping
        {
            public StorageFieldMapping(MemberInfo memberInfo, string storageMemberName, Func<object, bool>[] constraintHandlers, StorageFieldModifiers modifiers)
            {
                this.MemberInfo = memberInfo;
                this.StorageMemberName = storageMemberName;
                this.ConstraintHandlers = constraintHandlers;
                this.StorageFieldModifiers = modifiers;
                if (memberInfo is PropertyInfo)
                {
                    this.MemberType = ((PropertyInfo)memberInfo).PropertyType;
                }
                else if (memberInfo is FieldInfo)
                {
                    this.MemberType = ((FieldInfo)memberInfo).FieldType;
                }
            }
            public string MemberName { get { return MemberInfo.Name; } }
            public MemberInfo MemberInfo { get; set; }
            public string StorageMemberName { get; private set; }
            public Func<object, bool>[] ConstraintHandlers { get; private set; }
            public StorageFieldModifiers StorageFieldModifiers { get; private set; }

            public object GetValue(object entity)
            {
                MemberInfo mi = this.MemberInfo;
                if (MemberInfo.DeclaringType != entity.GetType())
                {
                    mi = entity.GetType().GetMember(mi.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                }
                if (mi == null) return null;

                if (mi is PropertyInfo)
                    return ((PropertyInfo)mi).GetValue(entity, null);
                else if (mi is FieldInfo)
                    return ((FieldInfo)mi).GetValue(entity);
                else
                    return null;
            }

            public void SetValue(object entity, object value)
            {
                MemberInfo mi = this.MemberInfo;
                if (MemberInfo.DeclaringType != entity.GetType())
                {
                    mi = MemberInfo.DeclaringType.GetMember(mi.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();
                }
                if (mi == null) return;

                if (mi is PropertyInfo)
                {
                    value = CastToEntityType(value, ((PropertyInfo)mi).PropertyType);
                    ((PropertyInfo)mi).SetValue(entity, value, null);
                }
                else if (mi is FieldInfo)
                {
                    value = CastToEntityType(value, ((FieldInfo)mi).FieldType);
                    ((FieldInfo)mi).SetValue(entity, value);
                }
            }

            public Type MemberType { get; private set; }

            private object CastToEntityType(object value, Type type)
            {
                if (value == DBNull.Value)
                {
                    if (type.IsNumeric())
                        value = 0;
                    else if (type.IsDateTime())
                        value = DateTime.MinValue;
                    else
                        value = null;
                }

                if (value == null && type.IsPrimitive)
                {
                    return Activator.CreateInstance(type);
                }
                else if (value == null && type.Equals(typeof(DateTime)))
                {
                    return DateTime.MinValue;
                }

                if (value is DateTime)
                {
                    value = ((DateTime)value).ToLocalTime();
                }

                return Expression.Lambda(Expression.Convert(Expression.Constant(value), type)).Compile().DynamicInvoke();
            }
        }
    }
}
