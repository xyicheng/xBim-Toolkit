#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRectangleHollowProfileDef.cs
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
    public class IfcRectangleHollowProfileDef : IfcRectangleProfileDef
    {
        #region Fields

        private IfcPositiveLengthMeasure _wallThickness;
        private IfcPositiveLengthMeasure? _innerFilletThickness;
        private IfcPositiveLengthMeasure? _outerFilletThickness;

        #endregion

        #region Properties

        /// <summary>
        ///   Thickness of the material.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure WallThickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _wallThickness;
            }
            set { ModelManager.SetModelValue(this, ref _wallThickness, value, v => WallThickness = v, "WallThickness"); }
        }

        /// <summary>
        ///   Radius of the circular arcs, by which all four corners of the outer contour of rectangle are equally rounded. 
        ///   If not given, zero (= no rounding arcs) applies.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure? InnerFilletThickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _innerFilletThickness;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _innerFilletThickness, value, v => InnerFilletThickness = v,
                                           "InnerFilletThickness");
            }
        }

        /// <summary>
        ///   Radius of the circular arcs, by which all four corners of the outer contour of rectangle are equally rounded. 
        ///   If not given, zero (= no rounding arcs) applies.
        /// </summary>
        [IfcAttribute(8, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure? OuterFilletThickness
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _outerFilletThickness;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _outerFilletThickness, value, v => OuterFilletThickness = v,
                                           "OuterFilletThickness");
            }
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
                    _wallThickness = value.RealVal;
                    break;
                case 6:
                    _innerFilletThickness = value.RealVal;
                    break;
                case 7:
                    _outerFilletThickness = value.RealVal;
                    break;

                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public override string WhereRule()
        {
            string baseErr = base.WhereRule();
            if (_wallThickness >= XDim/2 || _wallThickness >= YDim/2)
                baseErr +=
                    "WR31 RectangleHollowProfileDef : The wall thickness shall be smaller than half the value of the X or Y dimension of the rectangle.\n";
            if (_outerFilletThickness.HasValue &&
                (_outerFilletThickness.Value >= XDim/2 || _outerFilletThickness.Value >= YDim/2))
                baseErr +=
                    "WR32 RectangleHollowProfileDef : The outer fillet radius (if given) shall be smaller than or equal to half the value of the Xdim and the YDim attribute\n";
            if (_innerFilletThickness.HasValue &&
                (_innerFilletThickness.Value >= XDim/2 || _innerFilletThickness.Value >= YDim/2))
                baseErr +=
                    "WR33 RectangleHollowProfileDef : The inner fillet radius (if given) shall be smaller than or equal to half the value of the Xdim and the YDim attribute.\n";
            return baseErr;
        }
    }
}