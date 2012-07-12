#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcPlanarExtent.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcPlanarExtent : IfcGeometricRepresentationItem
    {
        #region Fields

        private IfcLengthMeasure _sizeInX;
        private IfcLengthMeasure _sizeInY;

        #endregion

        #region Properties

        /// <summary>
        ///   The extent in the direction of the x-axis.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure SizeInX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _sizeInX;
            }
            set { ModelManager.SetModelValue(this, ref _sizeInX, value, v => SizeInX = v, "SizeInX"); }
        }

        /// <summary>
        ///   The extent in the direction of the y-axis.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure SizeInY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _sizeInY;
            }
            set { ModelManager.SetModelValue(this, ref _sizeInY, value, v => SizeInY = v, "SizeInY"); }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _sizeInX = value.RealVal;
                    break;
                case 1:
                    _sizeInY = value.RealVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}