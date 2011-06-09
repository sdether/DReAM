using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("mindtouch.dream.client")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("mindtouch.dream.client")]
[assembly: AssemblyCopyright("Copyright (c) 2006-2011 MindTouch, Inc.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("5be5dd0f-5a82-48b8-a16e-e040fa4e86fa")]

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
[assembly: AssemblyVersion("3.0.0.0")]
[assembly: AssemblyFileVersion("3.0.0.0")]

#if SIGNED
[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile(@"..\mindtouch.snk")]
[assembly: InternalsVisibleTo("mindtouch.web.server, PublicKey=0024000004800000940000000602000000240000525341310004000001000100c59c584b5a2a3c335322d6cc44f0855889b5f16de611ab96788b1ae38f061514542ef69091168b01161968191345f509072c7f11c48710869ae14770c99e83dbe14b981aab3ba7306203f86bca0cebe91fe174c525095b31b0387211653b1b569d01d7c9ed889d460b915a91442705655498be9da4cd15e4af1811851e3dbbd7")]
#else
[assembly: InternalsVisibleTo("mindtouch.web.server")]
#endif
