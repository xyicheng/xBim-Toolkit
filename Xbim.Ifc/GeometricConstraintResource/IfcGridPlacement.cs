#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcGridPlacement.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.GeometricConstraintResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcGridPlacement : IfcObjectPlacement
    {
        #region Fields

        private VirtualGridIntersection _placementLocation;
        private VirtualGridIntersection _placementRefDirection;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   A constraint on one or both ends of the path for an ExtrudedSolid
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public VirtualGridIntersection PlacementLocation
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _placementLocation;
            }
            protected set
            {
                ModelManager.SetModelValue(this, ref _placementLocation, value, v => PlacementLocation = v,
                                           "PlacementLocation ");
            }
        }


        /// <summary>
        ///   Reference to a second grid axis intersection, which defines the orientation of the grid placement
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public VirtualGridIntersection PlacementRefDirection
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _placementRefDirection;
            }
            protected set
            {
                ModelManager.SetModelValue(this, ref _placementRefDirection, value, v => PlacementRefDirection = v,
                                           "PlacementRefDirection ");
            }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _placementLocation = (VirtualGridIntersection) value.EntityVal;
                    break;
                case 1:
                    _placementRefDirection = (VirtualGridIntersection) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        internal override void CopyValues(IfcObjectPlacement value)
        {
            IfcGridPlacement gp = value as IfcGridPlacement;
            PlacementLocation = gp.PlacementLocation;
            PlacementRefDirection = gp.PlacementRefDirection;
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}