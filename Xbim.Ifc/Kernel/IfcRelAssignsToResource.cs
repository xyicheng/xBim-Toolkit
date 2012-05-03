#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelAssignsToResource.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.Kernel
{
    /// <summary>
    ///   This objectified relationship (IfcRelAssignsToResource) handles the assignment of objects (subtypes of IfcObject) to a resource (subtypes of IfcResource).
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: This objectified relationship (IfcRelAssignsToResource) handles the assignment of objects (subtypes of IfcObject) to a resource (subtypes of IfcResource). 
    ///   For example: The assignment of a resource (e.g. a labor resource - as subtype of IfcResource) to a construction process on site (process as subtype of IfcObject) , is an application of this generic relationship.
    ///   HISTORY New Entity in IFC Release 2x. 
    ///   Formal Propositions:
    ///   WR1   :   The instance to with the relation points shall not be contained in the List of RelatedObjects.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcRelAssignsToResource : IfcRelAssigns
    {
        #region Fields

        private IfcResource _relatingResource;

        #endregion

        [IfcAttribute(7, IfcAttributeState.Mandatory)]
        public IfcResource RelatingResource
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingResource;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatingResource, value, v => RelatingResource = v,
                                           "RelatingResource");
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
                    base.IfcParse(propIndex, value);
                    break;
                case 6:
                    _relatingResource = (IfcResource) value.EntityVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }
    }
}