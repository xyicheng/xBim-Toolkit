#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelProjectsElement.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ProductExtension
{
    /// <summary>
    ///   The IfcRelProjectsElement is an objectified relationship between an building element and one projection element that creates a modifier to the shape of the element.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: The IfcRelProjectsElement is an objectified relationship between an building element and one projection element that creates a modifier to the shape of the element. This relationship implies a Boolean operation of addition for the geometric bodies of the building element and the projection element.
    ///   The relationship is defined to be a 1:1 relationship, if a building element has more than one projection, several relationship objects have to be used, each pointing to a different projection element.
    ///   HISTORY New entity in Release IFC2x Edition 2.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcRelProjectsElement : IfcRelConnects
    {
        #region Fields

        private IfcElement _relatingElement;
        private IfcFeatureElementAddition _relatedFeatureElement;

        #endregion

        /// <summary>
        ///   Element at which a projection is created by the associated IfcProjectionElement.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcElement RelatingElement
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingElement;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatingElement, value, v => RelatingElement = v,
                                           "RelatingElement");
            }
        }

        /// <summary>
        ///   Reference to the IfcFeatureElementAddition that defines an addition to the volume of the element, by using a Boolean addition operation. An example is a projection at the associated element.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcFeatureElementAddition RelatedFeatureElement
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatedFeatureElement;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatedFeatureElement, value, v => RelatedFeatureElement = v,
                                           "RelatedFeatureElement");
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
                    base.IfcParse(propIndex, value);
                    break;
                case 4:
                    _relatingElement = (IfcElement) value.EntityVal;
                    break;
                case 5:
                    _relatedFeatureElement = (IfcFeatureElementAddition) value.EntityVal;
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