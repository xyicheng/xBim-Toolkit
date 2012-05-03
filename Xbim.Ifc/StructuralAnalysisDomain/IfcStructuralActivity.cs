#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralActivity.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.RepresentationResource;
using Xbim.Ifc.StructuralLoadResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.StructuralAnalysisDomain
{
    [IfcPersistedEntity, Serializable]
    public abstract class IfcStructuralActivity : IfcProduct
    {
        #region Fields

        private IfcStructuralLoad _appliedLoad;


        private IfcGlobalOrLocalEnum _globalOrLocal;

        #endregion

        [IfcAttribute(8, IfcAttributeState.Mandatory)]
        public IfcStructuralLoad AppliedLoad
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _appliedLoad;
            }
            set { ModelManager.SetModelValue(this, ref _appliedLoad, value, v => AppliedLoad = v, "AppliedLoad"); }
        }

        [IfcAttribute(9, IfcAttributeState.Mandatory)]
        public IfcGlobalOrLocalEnum GlobalOrLocal
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _globalOrLocal;
            }
            set { ModelManager.SetModelValue(this, ref _globalOrLocal, value, v => GlobalOrLocal = v, "GlobalOrLocal"); }
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
                case 6:
                    base.IfcParse(propIndex, value);
                    break;
                case 7:
                    _appliedLoad = (IfcStructuralLoad) value.EntityVal;
                    break;
                case 8:
                    _globalOrLocal =
                        (IfcGlobalOrLocalEnum) Enum.Parse(typeof (IfcGlobalOrLocalEnum), value.StringVal, true);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        public override string WhereRule()
        {
            return base.WhereRule();
        }
    }
}