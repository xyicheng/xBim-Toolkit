#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcOneDirectionRepeatFactor.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcOneDirectionRepeatFactor : IfcGeometricRepresentationItem, IfcHatchLineDistanceSelect
    {
        #region Fields

        private IfcVector _repeatFactor;

        #endregion

        #region Part 21 Step file Parse routines

        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcVector RepeatFactor
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _repeatFactor;
            }
            set { ModelManager.SetModelValue(this, ref _repeatFactor, value, v => RepeatFactor = v, "RepeatFactor"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _repeatFactor = (IfcVector) value.EntityVal;
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