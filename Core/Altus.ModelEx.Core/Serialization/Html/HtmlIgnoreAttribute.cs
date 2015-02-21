using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Altus.Core.Serialization.Html
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple=false, Inherited=true)]
    public class HtmlIgnoreAttribute : Attribute
    {
    }
}
