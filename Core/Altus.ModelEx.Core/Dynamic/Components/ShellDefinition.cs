using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Altus.Core.Component;

[assembly: CompositionContainer(
    LoaderType="Altus.dynamic.components.DynamicComponentLoader, Altus",
    ShellType="Altus.dynamic.components.DynamicHost, Altus")]
