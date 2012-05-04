﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcReinforcingBar.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProfilePropertyResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.StructuralElementsDomain
{
    [IfcPersistedEntity, Serializable]
    public class IfcReinforcingBar : IfcReinforcingElement
    {
        #region Fields

        private IfcPositiveLengthMeasure _nominalDiameter;
        private IfcAreaMeasure _crossSectionArea;
        private IfcPositiveLengthMeasure? _barLength;
        private IfcReinforcingBarRoleEnum _barRole;
        private IfcReinforcingBarSurfaceEnum? _barSurface;

        #endregion

        #region Properties

        [IfcAttribute(10, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure NominalDiameter
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _nominalDiameter;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _nominalDiameter, value, v => NominalDiameter = v,
                                           "NominalDiameter");
            }
        }

        [IfcAttribute(11, IfcAttributeState.Mandatory)]
        public IfcAreaMeasure CrossSectionArea
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _crossSectionArea;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _crossSectionArea, value, v => CrossSectionArea = v,
                                           "CrossSectionArea");
            }
        }

        [IfcAttribute(12, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? BarLength
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _barLength;
            }
            set { ModelManager.SetModelValue(this, ref _barLength, value, v => BarLength = v, "BarLength"); }
        }

        [IfcAttribute(13, IfcAttributeState.Mandatory)]
        public IfcReinforcingBarRoleEnum BarRole
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _barRole;
            }
            set { ModelManager.SetModelValue(this, ref _barRole, value, v => BarRole = v, "BarRole"); }
        }

        [IfcAttribute(14, IfcAttributeState.Optional)]
        public IfcReinforcingBarSurfaceEnum? BarSurface
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _barSurface;
            }
            set { ModelManager.SetModelValue(this, ref _barSurface, value, v => BarSurface = v, "BarSurface"); }
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
                case 7:
                case 8:
                    base.IfcParse(propIndex, value);
                    break;
                case 9:
                    _nominalDiameter = value.RealVal;
                    break;
                case 10:
                    _crossSectionArea = value.RealVal;
                    break;
                case 11:
                    _barLength = value.RealVal;
                    break;
                case 12:
                    _barRole =
                        (IfcReinforcingBarRoleEnum)
                        Enum.Parse(typeof (IfcReinforcingBarRoleEnum), value.StringVal, true);
                    break;
                case 13:
                    _barSurface =
                        (IfcReinforcingBarSurfaceEnum)
                        Enum.Parse(typeof (IfcReinforcingBarSurfaceEnum), value.StringVal, true);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        public override string WhereRule()
        {
            string baseErr = base.WhereRule();
            if (_barRole == IfcReinforcingBarRoleEnum.USERDEFINED && !ObjectType.HasValue)
                baseErr +=
                    "WR1 ReinforcingBar : The attribute ObjectType shall be given, if the bar role type is set to USERDEFINED.\n";
            return baseErr;
        }
    }
}