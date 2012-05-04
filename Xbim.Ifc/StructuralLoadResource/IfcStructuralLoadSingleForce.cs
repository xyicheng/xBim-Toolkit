﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralLoadSingleForce.cs
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
    public class IfcStructuralLoadSingleForce : IfcStructuralLoadStatic
    {
        #region Fields

        private IfcForceMeasure? _forceX;
        private IfcForceMeasure? _forceY;
        private IfcForceMeasure? _forceZ;
        private IfcTorqueMeasure? _momentX;
        private IfcTorqueMeasure? _momentY;
        private IfcTorqueMeasure? _momentZ;

        #endregion

        #region Properties

        /// <summary>
        ///   Force value in x-direction.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcForceMeasure? ForceX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _forceX;
            }
            set { ModelManager.SetModelValue(this, ref _forceX, value, v => ForceX = v, "ForceX"); }
        }

        /// <summary>
        ///   Force value in y-direction.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcForceMeasure? ForceY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _forceY;
            }
            set { ModelManager.SetModelValue(this, ref _forceY, value, v => ForceY = v, "ForceY"); }
        }

        /// <summary>
        ///   Force value in z-direction.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcForceMeasure? ForceZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _forceZ;
            }
            set { ModelManager.SetModelValue(this, ref _forceZ, value, v => ForceZ = v, "ForceZ"); }
        }

        /// <summary>
        ///   Moment about the x-axis.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcTorqueMeasure? MomentX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _momentX;
            }
            set { ModelManager.SetModelValue(this, ref _momentX, value, v => MomentX = v, "MomentX"); }
        }

        /// <summary>
        ///   Moment about the y-axis.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcTorqueMeasure? MomentY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _momentY;
            }
            set { ModelManager.SetModelValue(this, ref _momentY, value, v => MomentY = v, "MomentY"); }
        }

        /// <summary>
        ///   Moment about the z-axis.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcTorqueMeasure? MomentZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _momentZ;
            }
            set { ModelManager.SetModelValue(this, ref _momentZ, value, v => MomentZ = v, "MomentZ"); }
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
                    _forceX = value.RealVal;
                    break;
                case 2:
                    _forceY = value.RealVal;
                    break;
                case 3:
                    _forceZ = value.RealVal;
                    break;
                case 4:
                    _momentX = value.RealVal;
                    break;
                case 5:
                    _momentY = value.RealVal;
                    break;
                case 6:
                    _momentZ = value.RealVal;
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