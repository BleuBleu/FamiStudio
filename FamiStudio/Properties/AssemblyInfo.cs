using System.Reflection;
using System.Runtime.InteropServices;
#if FAMISTUDIO_ANDROID
using Android.App;
#endif

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("FamiStudio")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("BleuBleu")]
[assembly: AssemblyProduct("FamiStudio")]
[assembly: AssemblyCopyright("Copyright © 2019-2024")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("20ac976f-95bc-42a4-b95c-85609728a36b")]

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

// The last digit is the BETA version number. When it is non-zero, the build and will be 
// interpreted as a development version.
[assembly: AssemblyVersion("4.2.0.3")]
[assembly: AssemblyFileVersion("4.2.0.3")]

#if FAMISTUDIO_ANDROID
// Add some common permissions, these can be removed if not needed
[assembly: UsesPermission(Android.Manifest.Permission.Internet)]
[assembly: UsesPermission(Android.Manifest.Permission.WriteExternalStorage)]

#if DEBUG
[assembly: Application(Debuggable=true)]
#else
[assembly: Application(Debuggable=false)]
#endif

#endif
