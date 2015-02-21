using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using Altus.Core.Compilation;
using Altus.Core.Diagnostics;

namespace Altus.Core.Dynamic
{
    public static class RuntimeTypeBuilder
    {
        private static Dictionary<string, Type> builtTypes = new Dictionary<string, Type>();
        private static long _typeNumber = 1;

        private static string GetTypeKey(Dictionary<string, Type> fields, Type baseType, string typeName)
        {
            //TODO: optimize the type caching 
            // if fields are simply reordered, 
            // that doesn't mean that they're actually different types, so this needs to be smarter
            string key = string.Empty;
            foreach (var field in fields)
                key += field.Key + ";" + field.Value.Name + ";";
            if (baseType != null) key += ";" + baseType.FullName;
            if (typeName != null) key += ";" + typeName;
            return key;
        }

        public static Type GetDynamicType<T>(Dictionary<string, Type> fields)
        {
            return GetDynamicType(fields, typeof(T));
        }
        public static Type GetDynamicType(Dictionary<string, Type> fields, Type baseType)
        {
            return GetDynamicType(fields, baseType, null, null);
        }
        public static Type GetDynamicType(Dictionary<string, Type> fields, Type baseType, string typeName, string body, params string[] references)
        {
            string template = _template;
            try
            {
                lock (builtTypes)
                {
                    string typeKey = GetTypeKey(fields, baseType, typeName);
                    if (builtTypes.ContainsKey(typeKey))
                        return builtTypes[typeKey];

                    string className = typeName ?? "DynamicType" + _typeNumber++;
                    template = template.Replace("@ClassName", className);
                    template = template.Replace("@Namespace", "Altus");
                    template = template.Replace("@Body", body ?? "");
                    string[] refs;
                    if (baseType == null)
                    {
                        template = template.Replace("@BaseType", "");
                        refs = references;
                    }
                    else
                    {
                        template = template.Replace("@BaseType", ": " + baseType.FullName);
                        if (references == null)
                            refs = new string[1];
                        else
                        {
                            refs = new string[references.Length + 1];
                            references.CopyTo(refs, 0);
                        }
                        refs[refs.Length - 1] = baseType.Assembly.Location;
                    }

                    StringBuilder props = new StringBuilder();
                    Dictionary<string, Type>.Enumerator en = fields.GetEnumerator();
                    while (en.MoveNext())
                    {
                        props.Append("\t\tpublic ");
                        props.Append(en.Current.Value.FullName.Replace(" ", "_"));
                        props.Append(" ");
                        props.Append(en.Current.Key.Replace(" ", "_"));
                        props.AppendLine(" { get; set; }");
                    }
                    template = template.Replace("@Properties", props.ToString());
                    bool hasErrors = false;
                    CompilerErrorCollection errors;
                    Type t = Altus.Core.Compilation.CSharpCompiler.Compile(template,
                        className,
                        Context.GetEnvironmentVariable<string>("TempDir", "Temp"),
                        refs,
                        out hasErrors,
                        out errors);
                    if (hasErrors)
                    {
                        throw (new InvalidProgramException(errors.ToErrorString()));
                    }
                    builtTypes.Add(typeKey, t);
                    return t;
                }
            }
            catch
            {

            }
            return null;
        }

        #region Dynamic IL
        //public static Type GetDynamicType(Dictionary<string, Type> fields)
        //{
        //    if (null == fields)
        //        throw new ArgumentNullException("fields");
        //    if (0 == fields.Count)
        //        throw new ArgumentOutOfRangeException("fields", "fields must have at least 1 field definition");

        //    try
        //    {
        //        Monitor.Enter(builtTypes);
        //        string typeKey = GetTypeKey(fields);

        //        if (builtTypes.ContainsKey(typeKey))
        //            return builtTypes[typeKey];

        //        string className = "DynamicType" + _typeNumber++;

        //        TypeBuilder typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Serializable);

        //        foreach (var field in fields)
        //        {
        //            var fld = typeBuilder.DefineField("_" + field.Key, field.Value, FieldAttributes.Private);

        //            var property = typeBuilder.DefineProperty(field.Key,
        //                PropertyAttributes.HasDefault,
        //                field.Value,
        //                null);
        //            var getter = typeBuilder.DefineMethod("get_" + field.Key,
        //                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
        //                field.Value,
        //                Type.EmptyTypes);
        //            var getterCode = getter.GetILGenerator();
        //            getterCode.Emit(OpCodes.Ldarg_0);
        //            getterCode.Emit(OpCodes.Ldfld, fld);
        //            getterCode.Emit(OpCodes.Ret);

        //            var setter = typeBuilder.DefineMethod("set_" + field.Key,
        //                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
        //                null,
        //                new[] { field.Value });
        //            var setterCode = setter.GetILGenerator();
        //            setterCode.Emit(OpCodes.Ldarg_0);
        //            setterCode.Emit(OpCodes.Ldarg_1);
        //            setterCode.Emit(OpCodes.Stfld, fld);

        //            property.SetGetMethod(getter);
        //            property.SetSetMethod(setter);
        //        }

        //        builtTypes[typeKey] = typeBuilder.CreateType();

        //        return builtTypes[typeKey];
        //    }
        //    catch (Exception ex)
        //    {
        //        Logger.LogError(ex);
        //    }
        //    finally
        //    {
        //        Monitor.Exit(builtTypes);
        //    }

        //    return null;
        //}
        #endregion

        private static string GetTypeKey(IEnumerable<MemberInfo> fields)
        {
            return GetTypeKey(fields.ToDictionary(f => f.Name, f => f.MemberType == MemberTypes.Field ? ((FieldInfo)f).FieldType : ((PropertyInfo)f).PropertyType), null, null);
        }

        public static Type GetDynamicType(IEnumerable<MemberInfo> fields)
        {
            return GetDynamicType(fields.ToDictionary(f => f.Name, f => f.MemberType == MemberTypes.Field ? ((FieldInfo)f).FieldType : ((PropertyInfo)f).PropertyType), null);
        }

        public static dynamic GetDynamicType(Type type, string[] names)
        {
            Dictionary<string, Type> props = new Dictionary<string, Type>();
            foreach (MemberInfo mi in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                if (mi is FieldInfo || mi is PropertyInfo)
                {
                    foreach (string prop in names)
                    {
                        props.Add(prop, mi.MemberType == MemberTypes.Field ? ((FieldInfo)mi).FieldType : ((PropertyInfo)mi).PropertyType);
                    }
                }
            }
            return GetDynamicType(props, null);
        }

        private readonly static string _template = @"
using System;

namespace @Namespace
{
    [Serializable]
    public class @ClassName @BaseType
    {
@Properties
        @Body
    }
}";
    }
}
