#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelConnectsWithEccentricity.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.StructuralAnalysisDomain
{
    [IfcPersistedEntity, Serializable]
    public class IfcRelConnectsWithEccentricity : IfcRelConnectsStructuralMember
    {
        #region Fields

        private IfcConnectionGeometry _connectionConstraint;

        #endregion

        #region Properties

        [IfcAttribute(11, IfcAttributeState.Mandatory)]
        public IfcConnectionGeometry ConnectionConstraint
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _connectionConstraint;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _connectionConstraint, value, v => ConnectionConstraint = v,
                                           "ConnectionConstraint");
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
                case 3:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                    base.IfcParse(propIndex, value);
                    break;
                case 10:
                    _connectionConstraint = (IfcConnectionGeometry) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }
    }
}