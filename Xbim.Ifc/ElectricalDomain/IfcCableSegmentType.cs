using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

namespace Xbim.Ifc.ElectricalDomain
{
    [IfcPersistedEntity, Serializable]
    public class IfcCableSegmentType : IfcFlowSegmentType
    {
        #region Fields

        IfcCableSegmentTypeEnum _predefinedType;
        
        #endregion

        #region IfcProperties

        /// <summary>
        /// Identifies the predefined types of cable segment from which the type required may be set. 
        /// </summary>
        [IfcAttribute(10, IfcAttributeState.Mandatory) /*, IfcPrimaryIndex*/]
        public IfcCableSegmentTypeEnum PredefinedType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _predefinedType;
            }
            set { ModelManager.SetModelValue(this, ref _predefinedType, value, v => PredefinedType = v, "PredefinedType"); }
        }

        #endregion

        #region IfcParse

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
                    _predefinedType = (IfcCableSegmentTypeEnum)
                        Enum.Parse(typeof(IfcCableSegmentTypeEnum), value.EnumVal, true);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

    }
}
