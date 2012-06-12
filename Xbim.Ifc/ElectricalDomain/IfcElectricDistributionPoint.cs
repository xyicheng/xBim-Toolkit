#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcElectricDistributionPoint.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ElectricalDomain
{
    [IfcPersistedEntity, Serializable]
    public class IfcElectricDistributionPoint : IfcFlowController
    {
        #region Fields

        private IfcElectricDistributionPointFunctionEnum _distributionPointFunction;
        private IfcLabel _userDefinedFunction;

        #endregion

        [IfcAttribute(9, IfcAttributeState.Mandatory)]
        public IfcElectricDistributionPointFunctionEnum DistributionPointFunction
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _distributionPointFunction;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _distributionPointFunction, value,
                                           v => DistributionPointFunction = v, "DistributionPointFunction");
            }
        }

        [IfcAttribute(10, IfcAttributeState.Optional)]
        public IfcLabel UserDefinedFunction
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _userDefinedFunction;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _userDefinedFunction, value, v => UserDefinedFunction = v,
                                           "UserDefinedFunction");
            }
        }

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
                    base.IfcParse(propIndex, value);
                    break;
                case 8:
                    _distributionPointFunction =
                        (IfcElectricDistributionPointFunctionEnum)
                        Enum.Parse(typeof (IfcElectricDistributionPointFunctionEnum), value.EnumVal, true);
                    break;
                case 9:
                    _userDefinedFunction = value.StringVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }
    }
}