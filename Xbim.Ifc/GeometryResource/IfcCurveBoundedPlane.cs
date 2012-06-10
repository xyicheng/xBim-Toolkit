#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCurveBoundedPlane.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Xml.Serialization;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcCurveBoundedPlane : IfcBoundedSurface, IPlacement3D
    {
        public IfcCurveBoundedPlane()
        {
            _innerBoundaries = new CurveSet(this);
        }

        #region Fields

        private IfcPlane _basisSurface;
        private IfcCurve _outerBoundary;
        private CurveSet _innerBoundaries;

        #endregion

        #region Part 21 Step file representation

        /// <summary>
        ///   The surface to be bound.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcPlane BasisSurface
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _basisSurface;
            }
            set { ModelManager.SetModelValue(this, ref _basisSurface, value, v => BasisSurface = v, "BasisSurface"); }
        }


        /// <summary>
        ///   The outer boundary of the surface.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcCurve OuterBoundary
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _outerBoundary;
            }
            set { ModelManager.SetModelValue(this, ref _outerBoundary, value, v => OuterBoundary = v, "OuterBoundary"); }
        }


        /// <summary>
        ///   An optional set of inner boundaries. They shall not intersect each other or the outer boundary.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public CurveSet InnerBoundaries
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _innerBoundaries;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _innerBoundaries, value, v => InnerBoundaries = v,
                                           "InnerBoundaries");
            }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _basisSurface = (IfcPlane) value.EntityVal;
                    break;
                case 1:
                    _outerBoundary = (IfcCurve) value.EntityVal;
                    break;
                case 2:
                    _innerBoundaries.Add((IfcCurve) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }


        [XmlIgnore]
        public override IfcDimensionCount Dim
        {
            get { return _basisSurface == null ? (IfcDimensionCount) 0 : _basisSurface.Dim; }
        }

        #endregion

        #region IPlacement3D Members

        IfcAxis2Placement3D IPlacement3D.Position
        {
            get { return _basisSurface.Position; }
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }
    }
}