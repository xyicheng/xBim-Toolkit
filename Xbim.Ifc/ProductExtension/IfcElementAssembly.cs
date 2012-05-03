#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcElementAssembly.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.ProductExtension
{
    /// <summary>
    ///   A container class that represents complex element assemblies aggregated from several elements, such as discrete elements, building elements, or other elements.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: A container class that represents complex element assemblies aggregated from several elements, such as discrete elements, building elements, or other elements.
    ///   EXAMPLE: Steel construction assemblies, such as trusses and different kinds of frames, can be represented by the IfcElementAssembly entity. Other examples include slab fields aggregated from a number of precast concrete slabs or reinforcement units made from several reinforcement bars. 
    ///   HISTORY: New Entity for Release IFC2x Edition 2.
    ///   Geometry Use Definitions
    ///   The geometric representation of IfcElementAssembly is given by the IfcProductDefinitionShape, allowing multiple geometric representations. 
    ///   Local Placement
    ///   The local placement for IfcElementAssembly is defined in its supertype IfcProduct. It is defined by the IfcLocalPlacement, which defines the local coordinate system that is referenced by all geometric representations. 
    ///   The PlacementRelTo relationship of IfcLocalPlacement shall point (if given) to the local placement of the same IfcBuildingElement, which is used in the Decomposes inverse attribute, i.e. the local placement is defined relative to the local placement of the building element in which the assembly is contained. 
    ///   If the relative placement is not used, the absolute placement is defined within the world coordinate system. 
    ///   Geometric Representations
    ///   The geometry of an IfcElementAssembly is generally formed from its components, in which case it does not need to have an explicit geometric representation. In some cases it may be useful to also expose a simple explicit representation as a bounding box representation of the complex composed shape independently.
    ///   Formal Propositions:
    ///   WR1   :   The attribute ObjectType shall be given, if the predefined type is set to USERDEFINED.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcElementAssembly : IfcElement
    {
        /// <summary>
        ///   Optional. A designation of where the assembly is intended to take place defined by an Enum.
        /// </summary>
        public IfcAssemblyPlaceEnum AssemblyPlace
        {
            get { throw new NotImplementedException(); }
            set { }
        }

        /// <summary>
        ///   Predefined generic types for a element assembly that are specified in an enumeration.
        /// </summary>
        public IfcElementAssemblyTypeEnum PredefinedType
        {
            get { throw new NotImplementedException(); }
            set { }
        }
    }
}