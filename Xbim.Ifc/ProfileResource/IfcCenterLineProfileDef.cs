#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCenterLineProfileDef.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ProfileResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcCenterLineProfileDef : IfcArbitraryOpenProfileDef
    {
        private IfcPositiveLengthMeasure _thickness;

        #region Properties

        public IfcPositiveLengthMeasure Thickness
        {
            get { return _thickness; }
            set { ModelManager.SetModelValue(this, ref _thickness, value, v => Thickness = v, "Thickness"); }
        }

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
                    _thickness = value.RealVal;
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