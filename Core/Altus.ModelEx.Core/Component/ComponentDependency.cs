using Altus.Core.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Component
{
    public class ComponentDependency
    {
        public ComponentAttribute ComponentAttribute { get; set; }
        public Dictionary<string, ComponentDependency> Dependencies { get; set; }
        public bool Created { get; set; }

        public static Dictionary<string, ComponentDependency> BuildDependencyGraph(ComponentAttribute[] attributes)
        {
            List<ComponentAttribute> attribs = new List<ComponentAttribute>(attributes);

            // bubble the installers to the top of the list
            attribs.Sort(new Comparison<ComponentAttribute>((a1, a2) =>
                {
                    bool a1Is = typeof(IInstaller).IsAssignableFrom(a1.ComponentType);
                    bool a2Is = typeof(IInstaller).IsAssignableFrom(a2.ComponentType);
                    bool a1Ss = typeof(ISerializer).IsAssignableFrom(a1.ComponentType);
                    bool a2Ss = typeof(ISerializer).IsAssignableFrom(a2.ComponentType);
                    int a1V = a1Is ? 1 << 16 : 0;
                    int a2V = a2Is ? 1 << 16 : 0;
                    int a1S = a1Ss ? 1 << 30 : 0;
                    int a2S = a2Ss ? 1 << 30 : 0;
                    a1V += a1S + (int)a1.Priority;
                    a2V += a2S + (int)a2.Priority;

                    return -a1V.CompareTo(a2V);
                }));

            Dictionary<string, ComponentDependency> list = new Dictionary<string, ComponentDependency>();

            // index all the attributes in first pass
            for (int i = 0; i < attribs.Count; i++)
            {
                ComponentDependency dep = new ComponentDependency();
                dep.ComponentAttribute = attribs[i];
                dep.Dependencies = new Dictionary<string, ComponentDependency>();
                list.Add(attribs[i].Name, dep);
            }

            // assign attributes to parents in second pass
            Dictionary<string, ComponentDependency>.Enumerator listEnum = list.GetEnumerator();
            while (listEnum.MoveNext())
            {
                if (listEnum.Current.Value.ComponentAttribute.Dependencies != null
                    && listEnum.Current.Value.ComponentAttribute.Dependencies.Length > 0)
                {
                    for (int i = 0; i < listEnum.Current.Value.ComponentAttribute.Dependencies.Length; i++)
                    {
                        if (list.ContainsKey(listEnum.Current.Value.ComponentAttribute.
                            Dependencies[i])
                            && !list[listEnum.Current.Value.ComponentAttribute.
                            Dependencies[i]].Dependencies.ContainsKey(listEnum.Current.Value.ComponentAttribute.Name)
                            && !list[listEnum.Current.Value.ComponentAttribute.Dependencies[i]].ComponentAttribute.Name.Equals(listEnum.Current.Value.ComponentAttribute.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            list[listEnum.Current.Value.ComponentAttribute.Dependencies[i]].Dependencies.Add(
                                listEnum.Current.Value.ComponentAttribute.Name,
                                listEnum.Current.Value);
                        }
                    }
                }
            }

            // remove parented dependencies from root graph in third pass
            List<string> keys = new List<string>();
            listEnum = list.GetEnumerator();
            while (listEnum.MoveNext())
            {
                if (listEnum.Current.Value.ComponentAttribute.Dependencies != null
                    && listEnum.Current.Value.ComponentAttribute.Dependencies.Length > 0
                    && !keys.Contains(listEnum.Current.Key))
                {
                    foreach (string dep in listEnum.Current.Value.ComponentAttribute.Dependencies)
                    {
                        if (list.ContainsKey(dep))
                        {
                            keys.Add(listEnum.Current.Key);
                            break;
                        }
                    }
                }
            }

            foreach (string key in keys)
            {
                list.Remove(key);
            }

            return list;
        }
    }
}

