﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDefinedSymbol.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.PresentationDefinitionResource
{
    /// <summary>
    ///   A defined symbol is a symbolic representation that gets its shape information by an established convention, 
    ///   either through a predefined symbol, or an externally defined symbol.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcDefinedSymbol : IfcGeometricRepresentationItem
    {
        #region Fields

        private IfcDefinedSymbolSelect _definition;
        private IfcCartesianTransformationOperator2D _target;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   An implicit description of the symbol, either predefined or externally defined. 
        ///   An exception is throw if the type is not PreDefinedSymbol or ExternallyDefinedSymbol
        ///   Use ValidDefinition to check for correct type
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcDefinedSymbolSelect Definition
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _definition;
            }
            set { ModelManager.SetModelValue(this, ref _definition, value, v => Definition = v, "Definition"); }
        }

        /// <summary>
        ///   A description of the placement, orientation and (uniform or non-uniform) scaling of the defined symbol.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcCartesianTransformationOperator2D Target
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _target;
            }
            set { ModelManager.SetModelValue(this, ref _target, value, v => Target = v, "Target"); }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _definition = (IfcDefinedSymbolSelect) value.EntityVal;
                    break;
                case 1:
                    _target = (IfcCartesianTransformationOperator2D) value.EntityVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }
    }
}