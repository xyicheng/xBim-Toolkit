#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcExtrudedAreaSolid.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Runtime.Serialization;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;
using WVector3D = System.Windows.Media.Media3D.Vector3D;

#endregion

namespace Xbim.Ifc.GeometricModelResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcExtrudedAreaSolid : IfcSweptAreaSolid
    {
        #region Fields

        private IfcDirection _extrudedDirection;
        private IfcPositiveLengthMeasure _depth;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        [DataMember(Order = 2)]
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcDirection ExtrudedDirection
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _extrudedDirection;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _extrudedDirection, value, v => ExtrudedDirection = v,
                                           "ExtrudedDirection");
            }
        }

        [DataMember(Order = 3)]
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure Depth
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _depth;
            }
            set { ModelManager.SetModelValue(this, ref _depth, value, v => Depth = v, "Depth"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                    base.IfcParse(propIndex, value);
                    break;
                case 2:
                    _extrudedDirection = (IfcDirection) value.EntityVal;
                    break;
                case 3:
                    _depth = value.RealVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        public override string WhereRule()
        {
            string baseErr = base.WhereRule();
            if (_extrudedDirection != null &&
                WVector3D.DotProduct(_extrudedDirection.WVector3D(), new WVector3D(0, 0, 1)) == 0)
                baseErr +=
                    "WR31 ExtrudedAreaSolid : The ExtrudedDirection shall not be perpendicular to the local z-axis.\n";
            return baseErr;
        }
    }
}