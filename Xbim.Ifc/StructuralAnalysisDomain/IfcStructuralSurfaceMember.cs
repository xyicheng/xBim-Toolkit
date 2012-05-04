﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralSurfaceMember.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.StructuralAnalysisDomain
{
    /// <summary>
    ///   Instances of the entity IfcStructuralSurfaceMember shall be used to describe planar structural elements. 
    ///   The material properties are defined by IfcMechanicalMaterialProperties (and subtypes) and they are connected
    ///   through IfcMaterial and IfcRelAssociatesMaterial and are accessible via the inherited inverse relationship HasAssociations.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcStructuralSurfaceMember : IfcStructuralMember
    {
        #region Fields

        private IfcStructuralSurfaceTypeEnum _predefinedType;
        private IfcPositiveLengthMeasure? _thickness;

        #endregion

        #region Properties

        /// <summary>
        ///   Defines the load carrying behavior of the member, as far as it is taken into account in the analysis.
        /// </summary>
        [IfcAttribute(8, IfcAttributeState.Mandatory)]
        public IfcStructuralSurfaceTypeEnum PredefinedType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _predefinedType;
            }
            set { ModelManager.SetModelValue(this, ref _predefinedType, value, v => PredefinedType = v, "PredefinedType"); }
        }

        /// <summary>
        ///   Defines the typically understood thickness of the structural face member, i.e. the smallest spatial dimension of the element.
        /// </summary>
        [IfcAttribute(9, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure? Thickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _thickness;
            }
            set { ModelManager.SetModelValue(this, ref _thickness, value, v => Thickness = v, "Thickness"); }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5:
                case 6:
                    base.IfcParse(propIndex, value);
                    break;
                case 7:
                    _predefinedType =
                        (IfcStructuralSurfaceTypeEnum)
                        Enum.Parse(typeof (IfcStructuralSurfaceTypeEnum), value.StringVal, true);
                    break;
                case 8:
                    _thickness = value.RealVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }
    }
}