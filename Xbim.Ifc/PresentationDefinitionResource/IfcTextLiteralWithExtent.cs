#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTextLiteralWithExtent.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.PresentationResource;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.PresentationDefinitionResource
{
    public class IfcTextLiteralWithExtent : IfcTextLiteral
    {
        #region Fields

        private IfcPlanarExtent _extent;
        private IfcBoxAlignment _boxAlignment;

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                    base.IfcParse(propIndex, value);
                    break;
                case 3:
                    _extent = (IfcPlanarExtent) value.EntityVal;
                    break;
                case 4:
                    _boxAlignment = value.StringVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }
    }
}