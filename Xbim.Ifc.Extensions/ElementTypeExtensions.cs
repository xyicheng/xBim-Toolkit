﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    ElementTypeExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Linq;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class ElementTypeExtensions
    {
        public static IfcMaterialSelect GetMaterial(this IfcElementType elemType)
        {
            IfcRelAssociatesMaterial relMat =
                elemType.HasAssociations.OfType<IfcRelAssociatesMaterial>().FirstOrDefault();
            if (relMat != null)
                return relMat.RelatingMaterial;
            else
                return null;
        }

        /// <summary>
        ///   Returns the Material Select or creates
        /// </summary>
        /// <param name = "elemType"></param>
        /// <returns></returns>
        public static void SetMaterial(this IfcElementType elemType, IfcMaterialSelect matSel)
        {
            if (matSel is IfcMaterialLayerSetUsage)
                throw new Exception("IfcElementType cannot have an IfcMaterialLayerSetUsage as its associated material");

            IfcRelAssociatesMaterial relMat =
                elemType.HasAssociations.OfType<IfcRelAssociatesMaterial>().FirstOrDefault();
            if (relMat == null)
            {
                IModel model = ModelManager.ModelOf(elemType);
                if (model == null)
                    throw new Exception("IfcElementType is not contained in a valid model");
                else
                {
                    relMat = model.New<IfcRelAssociatesMaterial>();
                    relMat.RelatedObjects.Add_Reversible(elemType);
                }
            }
            relMat.RelatingMaterial = matSel;
        }
    }
}