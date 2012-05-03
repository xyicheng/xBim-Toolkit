#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRectangleProfileDef.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Runtime.Serialization;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ProfileResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcRectangleProfileDef : IfcParameterizedProfileDef
    {
        #region Fields

        private IfcPositiveLengthMeasure _XDim;
        private IfcPositiveLengthMeasure _YDim;

        #endregion

        #region Part 21 Step file Parse routines

        [DataMember(Order = 3)]
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure XDim
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _XDim;
            }
            set { ModelManager.SetModelValue(this, ref _XDim, value, v => XDim = v, "XDim"); }
        }


        [DataMember(Order = 4)]
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure YDim
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _YDim;
            }
            set { ModelManager.SetModelValue(this, ref _YDim, value, v => YDim = v, "YDim"); }
        }


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
                    _XDim = value.RealVal;
                    break;
                case 4:
                    _YDim = value.RealVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }
    }
}