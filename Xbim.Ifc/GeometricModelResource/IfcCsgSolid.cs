#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCsgSolid.cs
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

namespace Xbim.Ifc.GeometricModelResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcCsgSolid : IfcSolidModel
    {
        #region Fields

        private IfcCsgSelect _treeRootExpression;

        #endregion

        #region Properties

        /// <summary>
        ///   Boolean expression of regularized operators describing the solid. The root of the tree of Boolean expressions is given explicitly 
        ///   as an IfcBooleanResult (the only item in the Select IfcCsgSelect).
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcCsgSelect TreeRootExpression
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _treeRootExpression;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _treeRootExpression, value, v => TreeRootExpression = v,
                                           "TreeRootExpression");
            }
        }

        #endregion

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _treeRootExpression = (IfcCsgSelect) value.EntityVal;
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