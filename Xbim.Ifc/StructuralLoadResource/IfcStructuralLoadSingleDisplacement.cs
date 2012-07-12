#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralLoadSingleDisplacement.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.StructuralLoadResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcStructuralLoadSingleDisplacement : IfcStructuralLoadStatic
    {
        #region Fields

        private IfcLengthMeasure? _displacementX;
        private IfcLengthMeasure? _displacementY;
        private IfcLengthMeasure? _displacementZ;
        private IfcPlaneAngleMeasure? _rotationalDisplacementRX;
        private IfcPlaneAngleMeasure? _rotationalDisplacementRY;
        private IfcPlaneAngleMeasure? _rotationalDisplacementRZ;

        #endregion

        #region Properties

        /// <summary>
        ///   Displacement value in x-direction.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcLengthMeasure? DisplacementX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _displacementX;
            }
            set { ModelManager.SetModelValue(this, ref _displacementX, value, v => DisplacementX = v, "DisplacementX"); }
        }

        /// <summary>
        ///   Displacement value in y-direction.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLengthMeasure? DisplacementY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _displacementY;
            }
            set { ModelManager.SetModelValue(this, ref _displacementY, value, v => DisplacementY = v, "DisplacementY"); }
        }

        /// <summary>
        ///   Displacement value in z-direction.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcLengthMeasure? DisplacementZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _displacementZ;
            }
            set { ModelManager.SetModelValue(this, ref _displacementZ, value, v => DisplacementZ = v, "DisplacementZ"); }
        }

        /// <summary>
        ///   RotationalDisplacementR about the x-axis.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcPlaneAngleMeasure? RotationalDisplacementRX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalDisplacementRX;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalDisplacementRX, value, v => RotationalDisplacementRX = v,
                                           "RotationalDisplacementRX");
            }
        }

        /// <summary>
        ///   RotationalDisplacementR about the y-axis.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcPlaneAngleMeasure? RotationalDisplacementRY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalDisplacementRY;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalDisplacementRY, value, v => RotationalDisplacementRY = v,
                                           "RotationalDisplacementRY");
            }
        }

        /// <summary>
        ///   RotationalDisplacementR about the z-axis.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcPlaneAngleMeasure? RotationalDisplacementRZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalDisplacementRZ;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalDisplacementRZ, value, v => RotationalDisplacementRZ = v,
                                           "RotationalDisplacementRZ");
            }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    base.IfcParse(propIndex, value);
                    break;
                case 1:
                    _displacementX = value.RealVal;
                    break;
                case 2:
                    _displacementY = value.RealVal;
                    break;
                case 3:
                    _displacementZ = value.RealVal;
                    break;
                case 4:
                    _rotationalDisplacementRX = value.RealVal;
                    break;
                case 5:
                    _rotationalDisplacementRY = value.RealVal;
                    break;
                case 6:
                    _rotationalDisplacementRZ = value.RealVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}