#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcStructuralAnalysisModel.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.StructuralAnalysisDomain
{
    [IfcPersistedEntity, Serializable]
    public class IfcStructuralAnalysisModel : IfcSystem
    {
        #region Fields

        private IfcAnalysisModelTypeEnum _predefinedType;
        private IfcAxis2Placement3D _orientationOf2DPlane;
        private XbimSet<IfcStructuralLoadGroup> _loadedBy;
        private IfcStructuralResultGroup _hasResults;

        #endregion

        #region Properties

        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcAnalysisModelTypeEnum PredefinedType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _predefinedType;
            }
            set { ModelManager.SetModelValue(this, ref _predefinedType, value, v => PredefinedType = v, "PredefinedType"); }
        }

        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcAxis2Placement3D OrientationOf2DPlane
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _orientationOf2DPlane;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _orientationOf2DPlane, value, v => OrientationOf2DPlane = v,
                                           "OrientationOf2DPlane");
            }
        }

        [IfcAttribute(8, IfcAttributeState.Optional)]
        public XbimSet<IfcStructuralLoadGroup> LoadedBy
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _loadedBy;
            }
            set { ModelManager.SetModelValue(this, ref _loadedBy, value, v => LoadedBy = v, "LoadedBy"); }
        }

        [IfcAttribute(9, IfcAttributeState.Optional)]
        public IfcStructuralResultGroup HasResults
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _hasResults;
            }
            set { ModelManager.SetModelValue(this, ref _hasResults, value, v => HasResults = v, "HasResults"); }
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
                    base.IfcParse(propIndex, value);
                    break;
                case 5:
                    _predefinedType =
                        (IfcAnalysisModelTypeEnum) Enum.Parse(typeof (IfcAnalysisModelTypeEnum), value.StringVal, true);
                    break;
                case 6:
                    _orientationOf2DPlane = (IfcAxis2Placement3D) value.EntityVal;
                    break;
                case 7:
                    if (_loadedBy == null) _loadedBy = new XbimSet<IfcStructuralLoadGroup>(this);
                    _loadedBy.Add((IfcStructuralLoadGroup) value.EntityVal);
                    break;
                case 8:
                    _hasResults = (IfcStructuralResultGroup) value.EntityVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }
    }
}