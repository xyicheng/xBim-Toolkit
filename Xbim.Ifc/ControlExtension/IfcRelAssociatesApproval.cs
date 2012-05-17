using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc.ApprovalResource;
using Xbim.XbimExtensions.Parser;

namespace Xbim.Ifc.ControlExtension
{
    [IfcPersistedEntity, Serializable]
    public class IfcRelAssociatesApproval : IfcRelAssociates
    {
        #region Fields
        IfcApproval _relatingApproval;
        #endregion

        #region Ifc Properties
        /// <summary>
        /// Reference to approval that is being applied using this relationship.
        /// </summary>
        [IfcAttribute(9, IfcAttributeState.Mandatory) /*, IfcPrimaryIndex*/]
        public IfcApproval RelatingApproval
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _relatingApproval;
            }
            set { ModelManager.SetModelValue(this, ref _relatingApproval, value, v => RelatingApproval = v, "RelatingApproval"); }
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
                    base.IfcParse(propIndex, value);
                    break;
                case 6:
                    _relatingApproval = (IfcApproval)value.EntityVal; 
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }
        #endregion

    }
}
