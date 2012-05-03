#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcConic.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    [IfcPersistedEntity, Serializable]
    public abstract class IfcConic : IfcCurve
    {
        #region Fields

        private IfcAxis2Placement _position;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The location and orientation of the conic. Further details of the interpretation of this attribute are given for the individual subtypes.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcAxis2Placement Position
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _position;
            }
            set
            {
                if (value is IfcAxis2Placement2D || value is IfcAxis2Placement3D)
                    ModelManager.SetModelValue(this, ref _position, value, v => Position = v, "Position");
                else
                    throw new ArgumentException("Illegal axis2placement type", "Position");
            }
        }

        public override IfcDimensionCount Dim
        {
            get
            {
                if (Position is IfcAxis2Placement2D) return 2;
                else if (Position is IfcAxis2Placement3D) return 3;
                else return 0;
            }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _position = (IfcAxis2Placement) value.EntityVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion
    }
}