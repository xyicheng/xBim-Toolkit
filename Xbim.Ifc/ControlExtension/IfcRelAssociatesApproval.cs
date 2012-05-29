#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelAssociatesApproval.cs
// Published:   05, 2012
// Last Edited: 15:00 PM on 23 05 2012
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ControlExtension
{
    /// <summary>
    /// The entity IfcRelAssociatesApproval is used to apply approval information defined by IfcApproval, 
    /// in IfcApprovalResource schema, to all subtypes of IfcRoot.
    /// </summary>
    /// <remarks>
    /// Definition from IAI: The entity IfcRelAssociatesApproval is used to apply approval information defined by IfcApproval, 
    /// in IfcApprovalResource schema, to all subtypes of IfcRoot.
    /// NOTE: This entity replaces the IfcApprovalUsage in IFC2x
    /// HISTORY: New entity in Release IFC2x Edition 2.
    /// </remarks>
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
        [IfcAttribute(9, IfcAttributeState.Mandatory)]
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
