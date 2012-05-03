#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcLShapeProfileDef.cs
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
    public class IfcLShapeProfileDef : IfcParameterizedProfileDef
    {
        #region Fields

        private IfcPositiveLengthMeasure _depth;
        private IfcPositiveLengthMeasure? _width;
        private IfcPositiveLengthMeasure _thickness;
        private IfcPositiveLengthMeasure? _filletRadius;
        private IfcPositiveLengthMeasure? _edgeRadius;
        private IfcPlaneAngleMeasure? _legSlope;
        private IfcPositiveLengthMeasure? _centreOfGravityInX;
        private IfcPositiveLengthMeasure? _centreOfGravityInY;

        #endregion

        #region Properties

        /// <summary>
        ///   Leg length, see illustration above (= h).
        /// </summary>
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

        /// <summary>
        ///   Leg length, see illustration above (= b). If not given, the value of the Depth attribute is applied to Width.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure? Width
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _width;
            }
            set { ModelManager.SetModelValue(this, ref _width, value, v => Width = v, "Width"); }
        }

        /// <summary>
        ///   Constant wall thickness of profile, see illustration above (= ts).
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure Thickness
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


        /// <summary>
        ///   Fillet radius according the above illustration (= r1). If it is not given, zero is assumed.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
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

        /// <summary>
        ///   Edge radius according the above illustration (= r2). If it is not given, zero is assumed.
        /// </summary>
        [IfcAttribute(8, IfcAttributeState.Optional)]
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

        /// <summary>
        ///   Slope of leg of the profile. If it is not given, zero is assumed.
        /// </summary>
        [IfcAttribute(9, IfcAttributeState.Optional)]
        public IfcPlaneAngleMeasure? LegSlope
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _legSlope;
            }
            set { ModelManager.SetModelValue(this, ref _legSlope, value, v => LegSlope = v, "LegSlope"); }
        }

        /// <summary>
        ///   Location of centre of gravity along the x axis measured from the center of the bounding box.
        /// </summary>
        [IfcAttribute(10, IfcAttributeState.Optional)]
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

        /// <summary>
        ///   Location of centre of gravity along the Y axis measured from the center of the bounding box.
        /// </summary>
        [IfcAttribute(11, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? CentreOfGravityInY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _centreOfGravityInY;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _centreOfGravityInY, value, v => CentreOfGravityInY = v,
                                           "CentreOfGravityInY");
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
                    _width = value.RealVal;
                    break;
                case 5:
                    _thickness = value.RealVal;
                    break;
                case 6:
                    _filletRadius = value.RealVal;
                    break;
                case 7:
                    _edgeRadius = value.RealVal;
                    break;
                case 8:
                    _legSlope = value.RealVal;
                    break;
                case 9:
                    _centreOfGravityInX = value.RealVal;
                    break;
                case 10:
                    _centreOfGravityInY = value.RealVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        public override string WhereRule()
        {
            string err = "";
            if (_thickness >= _depth)
                err +=
                    "WR21 LShapeProfileDef : The thickness of the flange has to be smaller than the depth of the profile.\n";
            if (_width.HasValue && _thickness >= _width.Value)
                err +=
                    "WR2 LShapeProfileDef : The thickness of the flange has to be smaller than the width of the profile (if given).\n";
            return err;
        }
    }
}