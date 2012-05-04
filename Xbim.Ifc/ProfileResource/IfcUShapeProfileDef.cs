﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcUShapeProfileDef.cs
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

namespace Xbim.Ifc.ProfileResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcUShapeProfileDef : IfcParameterizedProfileDef
    {
        #region Fields 

        private IfcPositiveLengthMeasure _depth;
        private IfcPositiveLengthMeasure _flangeWidth;
        private IfcPositiveLengthMeasure _webThickness;
        private IfcPositiveLengthMeasure _flangeThickness;
        private IfcPositiveLengthMeasure? _filletRadius;
        private IfcPositiveLengthMeasure? _edgeRadius;
        private IfcPlaneAngleMeasure? _flangeSlope;
        private IfcPositiveLengthMeasure? _centreOfGravityInX;

        #endregion

        #region Properties

        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure Depth
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _depth;
            }
            set { ModelManager.SetModelValue(this, ref _depth, value, v => Depth = v, "Depth"); }
        }

        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure FlangeWidth
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _flangeWidth;
            }
            set { ModelManager.SetModelValue(this, ref _flangeWidth, value, v => FlangeWidth = v, "FlangeWidth"); }
        }

        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure WebThickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _webThickness;
            }
            set { ModelManager.SetModelValue(this, ref _webThickness, value, v => WebThickness = v, "WebThickness"); }
        }

        [IfcAttribute(7, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure FlangeThickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _flangeThickness;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _flangeThickness, value, v => FlangeThickness = v,
                                           "FlangeThickness");
            }
        }

        [IfcAttribute(8, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? FilletRadius
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _filletRadius;
            }
            set { ModelManager.SetModelValue(this, ref _filletRadius, value, v => FilletRadius = v, "FilletRadius"); }
        }

        [IfcAttribute(9, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? EdgeRadius
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _edgeRadius;
            }
            set { ModelManager.SetModelValue(this, ref _edgeRadius, value, v => EdgeRadius = v, "EdgeRadius"); }
        }

        [IfcAttribute(10, IfcAttributeState.Optional)]
        public IfcPlaneAngleMeasure? FlangeSlope
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _flangeSlope;
            }
            set { ModelManager.SetModelValue(this, ref _flangeSlope, value, v => FlangeSlope = v, "FlangeSlope"); }
        }

        [IfcAttribute(11, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? CentreOfGravityInX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _centreOfGravityInX;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _centreOfGravityInX, value, v => CentreOfGravityInX = v,
                                           "CentreOfGravityInX");
            }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                    base.IfcParse(propIndex, value);
                    break;
                case 3:
                    _depth = value.RealVal;
                    break;
                case 4:
                    _flangeWidth = value.RealVal;
                    break;
                case 5:
                    _webThickness = value.RealVal;
                    break;
                case 6:
                    _flangeThickness = value.RealVal;
                    break;
                case 7:
                    _filletRadius = value.RealVal;
                    break;
                case 8:
                    _edgeRadius = value.RealVal;
                    break;
                case 9:
                    _flangeSlope = value.RealVal;
                    break;
                case 10:
                    _centreOfGravityInX = value.RealVal;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        public override string WhereRule()
        {
            string err = "";
            if (_flangeThickness >= _depth/2)
                err += "WR21 UShapeProfileDef : The flange thickness shall be smaller than half of the depth.\n";
            if (_webThickness >= _flangeWidth)
                err += "WR2 UShapeProfileDef : The web thickness shall be smaller than the flange width.\n";
            return err;
        }
    }
}