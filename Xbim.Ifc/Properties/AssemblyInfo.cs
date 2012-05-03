#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    AssemblyInfo.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Markup;

#endregion

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle("Xbim.Ifc")]
[assembly: AssemblyDescription("Ifc Entities")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Lockley Consulting")]
[assembly: AssemblyProduct("Xbim.Ifc")]
[assembly: AssemblyCopyright("Copyright © Lockley Consulting 2007")]
[assembly: AssemblyTrademark("Xbim")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid("53da0caf-5424-436d-b313-0a61d0ab4ff1")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build NumberOfCellsInRow
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]


[assembly: AssemblyVersion("2.3.*")]
[assembly: AssemblyFileVersion("2.3.1.1")] //Ifc 2x3 TC 1
[assembly: CLSCompliant(true)]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.ActorResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.GeometricConstraintResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.GeometricModelResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.GeometryResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.Kernel")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.MaterialResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.PresentationAppearanceResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.ProductExtension")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.RepresentationResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.SharedBldgElements")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.Ifc.ExternalReferenceResource")]
[assembly: XmlnsDefinition("http://schemas.Xbim.com/ifc", "Xbim.XbimExtensions")]