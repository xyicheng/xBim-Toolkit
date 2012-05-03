#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcBoundaryEdgeCondition.cs
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

namespace Xbim.Ifc.StructuralLoadResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcBoundaryEdgeCondition : IfcBoundaryCondition
    {
        #region Fields

        private IfcModulusOfLinearSubgradeReactionMeasure? _linearStiffnessByLengthX;
        private IfcModulusOfLinearSubgradeReactionMeasure? _linearStiffnessByLengthY;
        private IfcModulusOfLinearSubgradeReactionMeasure? _linearStiffnessByLengthZ;
        private IfcModulusOfRotationalSubgradeReactionMeasure? _rotationalStiffnessByLengthX;
        private IfcModulusOfRotationalSubgradeReactionMeasure? _rotationalStiffnessByLengthY;
        private IfcModulusOfRotationalSubgradeReactionMeasure? _rotationalStiffnessByLengthZ;

        #endregion

        #region Properties

        /// <summary>
        ///   Linear stiffness value in x-direction of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcModulusOfLinearSubgradeReactionMeasure? RotationalStiffnessByLengthByLengthX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearStiffnessByLengthX;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _linearStiffnessByLengthX, value,
                                           v => RotationalStiffnessByLengthByLengthX = v,
                                           "RotationalStiffnessByLengthByLengthX");
            }
        }

        /// <summary>
        ///   Linear stiffness value in y-direction of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcModulusOfLinearSubgradeReactionMeasure? RotationalStiffnessByLengthByLengthY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearStiffnessByLengthY;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _linearStiffnessByLengthY, value,
                                           v => RotationalStiffnessByLengthByLengthY = v,
                                           "RotationalStiffnessByLengthByLengthY");
            }
        }

        /// <summary>
        ///   Linear stiffness value in z-direction of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcModulusOfLinearSubgradeReactionMeasure? RotationalStiffnessByLengthByLengthZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _linearStiffnessByLengthZ;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _linearStiffnessByLengthZ, value,
                                           v => RotationalStiffnessByLengthByLengthZ = v,
                                           "RotationalStiffnessByLengthByLengthZ");
            }
        }

        /// <summary>
        ///   Rotational stiffness value about the x-axis of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcModulusOfRotationalSubgradeReactionMeasure? RotationalStiffnessByLengthX
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalStiffnessByLengthX;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalStiffnessByLengthX, value,
                                           v => RotationalStiffnessByLengthX = v, "RotationalStiffnessByLengthX");
            }
        }

        /// <summary>
        ///   Rotational stiffness value about the y-axis of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcModulusOfRotationalSubgradeReactionMeasure? RotationalStiffnessByLengthY
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalStiffnessByLengthY;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalStiffnessByLengthY, value,
                                           v => RotationalStiffnessByLengthY = v, "RotationalStiffnessByLengthY");
            }
        }

        /// <summary>
        ///   Rotational stiffness value about the z-axis of the coordinate system defined by the instance which uses this resource object.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcModulusOfRotationalSubgradeReactionMeasure? RotationalStiffnessByLengthZ
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _rotationalStiffnessByLengthZ;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _rotationalStiffnessByLengthZ, value,
                                           v => RotationalStiffnessByLengthZ = v, "RotationalStiffnessByLengthZ");
            }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    base.IfcParse(propIndex, value);
                    break;
                case 1:
                    _linearStiffnessByLengthX = value.RealVal;
                    break;
                case 2:
                    _linearStiffnessByLengthY = value.RealVal;
                    break;
                case 3:
                    _linearStiffnessByLengthZ = value.RealVal;
                    break;
                case 4:
                    _rotationalStiffnessByLengthX = value.RealVal;
                    break;
                case 5:
                    _rotationalStiffnessByLengthY = value.RealVal;
                    break;
                case 6:
                    _rotationalStiffnessByLengthZ = value.RealVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        public override string WhereRule()
        {
            return "";
        }
    }
}