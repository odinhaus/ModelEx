using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Altus.Core.Presentation.Wpf
{
    public static class DependencyObjectEx
    {
        public static T FindParent<T>(this DependencyObject initial) where T : DependencyObject
        {
            DependencyObject current = initial;

            while (current != null && current.GetType() != typeof(T))
            {
                current = VisualTreeHelper.GetParent(current);
            }
            return current as T;
        }

        public static T FindVisualChild<T>(this DependencyObject container) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(container, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject container) where T : DependencyObject
        {
            if (container != null && container is T)
                yield return (T)container;
            else
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(container); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(container, i);
                    if (child != null && child is T)
                        yield return (T)child;
                    else
                    {
                        IEnumerable<T> childrenOfChild = FindVisualChildren<T>(child);
                        if (childrenOfChild != null)
                        {
                            foreach (T achild in childrenOfChild)
                            {
                                if (achild != null && achild is T)
                                    yield return achild;
                            }
                        }
                    }
                }
                yield return null;
            }
        }
    }
}
