using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Altus.Core.Component;
using System.Collections;
using System.Data.Entity.Design.PluralizationServices;

namespace Altus.Core.Serialization.Html
{
    public static class ReflectionSerializer
    {
        public static byte[] Serialize(object source)
        {
            StringBuilder sb = new StringBuilder();
            

            IEnumerable list = null;
            string fullName = "";
            if (source is IEnumerable)
            {
                PluralizationService ps = PluralizationService.CreateService(System.Threading.Thread.CurrentThread.CurrentCulture);
                IEnumerator en = ((IEnumerable)source).GetEnumerator();
                en.MoveNext();
                fullName = en.Current.GetType().FullName;
                string shortName = en.Current.GetType().Name;
                fullName = fullName.Replace(shortName, ps.Pluralize(shortName));
                sb.Append("<" + fullName + ">");
            }
            else
                list = new object[] { source };

            foreach (object sourceItem in list)
            {
                string sourceType = source.GetType().FullName;
                sb.Append("<" + sourceType + ">");
                MemberInfo[] members = sourceItem.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance);

                foreach (MemberInfo member in members)
                {
                    object value = null;
                    string name = member.Name;
                    object[] ignores = member.GetCustomAttributes(typeof(HtmlIgnoreAttribute), true);
                    if (ignores == null || ignores.Length == 0)
                    {
                        if (member is PropertyInfo)
                        {
                            value = ((PropertyInfo)member).GetValue(source, null);
                        }
                        else if (member is FieldInfo)
                        {
                            value = ((FieldInfo)member).GetValue(source);
                        }
                    }
                    if (value != null)
                    {
                        sb.Append("<" + sourceType + ".Attribute name=\"" + name + "\">");
                        ISerializer serializer = App.Instance.Shell.GetComponents<ISerializer>()
                            .Where(s => s.SupportsFormat(StandardFormats.HTML) && s.SupportsType(value.GetType())).FirstOrDefault();
                        if (serializer == null)
                        {
                            sb.Append(value.ToString());
                        }
                        else
                        {
                            sb.Append(SerializationContext.TextEncoding.GetString(serializer.Serialize(value)));
                        }
                        sb.Append("</" + sourceType + ".Attribute>");
                    }
                }
                sb.Append("</" + sourceType + ">");
            }

            if (source is IEnumerable)
            {
                sb.Append("</" + fullName + ">");
            }

            return SerializationContext.TextEncoding.GetBytes(sb.ToString());
        }
    }
}
