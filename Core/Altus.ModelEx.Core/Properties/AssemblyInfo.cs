using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Altus.Core.Component;
using Altus.Core.Data;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Altus.Core")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Altus.Core")]
[assembly: AssemblyCopyright("Copyright ©  2014")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4deef94f-b860-4d16-9847-0f036f520ab3")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: DataContextScripts("Altus.Core.Data.SQLite.Scripts")]
[assembly: AppDataContext(
    typeof(Altus.Core.Data.SQlite.MetaDataContextSQLite),
    typeof(Altus.Core.Data.SQlite.MetaDataContextConnection),
    typeof(Altus.Core.Data.SQlite.MetaDataContextConnectionManager)
    )]
[assembly: CoreAssembly("4D05ECF0-487B-49DA-97D3-A68066968818")]
