#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelConnectsPortToElement.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.ProductExtension
{
    /// <summary>
    ///   An IfcRelConnectsPortToElement defines the relationship that is made between one port to the IfcElement in which it is contained.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcRelConnectsPortToElement : IfcRelConnects
    {
        #region Fields

        private IfcPort _relatingPort;
        private IfcElement _relatedElement;

        #endregion

        /// <summary>
        ///   Reference to an Port that is connected by the objectified relationship.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcPort RelatingPort
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingPort;
            }
            set { ModelManager.SetModelValue(this, ref _relatingPort, value, v => RelatingPort = v, "RelatingPort"); }
        }

        /// <summary>
        ///   Reference to an Element that is connected by the objectified relationship.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcElement RelatedElement
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatedElement;
            }
            set { ModelManager.SetModelValue(this, ref _relatedElement, value, v => RelatedElement = v, "RelatedElement"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    base.IfcParse(propIndex, value);
                    break;
                case 4:
                    _relatingPort = (IfcPort) value.EntityVal;
                    break;
                case 5:
                    _relatedElement = (IfcElement) value.EntityVal;
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