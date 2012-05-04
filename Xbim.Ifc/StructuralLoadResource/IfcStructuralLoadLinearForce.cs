﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralLoadLinearForce.cs
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

namespace Xbim.Ifc.StructuralLoadResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcStructuralLoadLinearForce : IfcStructuralLoadStatic
    {
        #region Fields

        private IfcLinearForceMeasure? _linearForceX;
        private IfcLinearForceMeasure? _linearForceY;
        private IfcLinearForceMeasure? _linearForceZ;
        private IfcLinearMomentMeasure? _linearMomentX;
        private IfcLinearMomentMeasure? _linearMomentY;
        private IfcLinearMomentMeasure? _linearMomentZ;

        #endregion

        #region Properties

        /// <summary>
        ///   LinearForce value in x-direction.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcLinearForceMeasure? LinearForceX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearForceX;
            }
            set { ModelManager.SetModelValue(this, ref _linearForceX, value, v => LinearForceX = v, "LinearForceX"); }
        }

        /// <summary>
        ///   LinearForce value in y-direction.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLinearForceMeasure? LinearForceY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearForceY;
            }
            set { ModelManager.SetModelValue(this, ref _linearForceY, value, v => LinearForceY = v, "LinearForceY"); }
        }

        /// <summary>
        ///   LinearForce value in z-direction.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcLinearForceMeasure? LinearForceZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearForceZ;
            }
            set { ModelManager.SetModelValue(this, ref _linearForceZ, value, v => LinearForceZ = v, "LinearForceZ"); }
        }

        /// <summary>
        ///   LinearMoment about the x-axis.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcLinearMomentMeasure? LinearMomentX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearMomentX;
            }
            set { ModelManager.SetModelValue(this, ref _linearMomentX, value, v => LinearMomentX = v, "LinearMomentX"); }
        }

        /// <summary>
        ///   LinearMoment about the y-axis.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcLinearMomentMeasure? LinearMomentY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearMomentY;
            }
            set { ModelManager.SetModelValue(this, ref _linearMomentY, value, v => LinearMomentY = v, "LinearMomentY"); }
        }

        /// <summary>
        ///   LinearMoment about the z-axis.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcLinearMomentMeasure? LinearMomentZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearMomentZ;
            }
            set { ModelManager.SetModelValue(this, ref _linearMomentZ, value, v => LinearMomentZ = v, "LinearMomentZ"); }
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
                    _linearForceX = value.RealVal;
                    break;
                case 2:
                    _linearForceY = value.RealVal;
                    break;
                case 3:
                    _linearForceZ = value.RealVal;
                    break;
                case 4:
                    _linearMomentX = value.RealVal;
                    break;
                case 5:
                    _linearMomentY = value.RealVal;
                    break;
                case 6:
                    _linearMomentZ = value.RealVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}