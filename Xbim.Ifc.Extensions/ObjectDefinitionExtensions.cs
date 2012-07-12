#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    ObjectDefinitionExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

using System;
using System.Linq;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;


namespace Xbim.Ifc.Extensions
{
    public static class ObjectDefinitionExtensions
    {
        public static void AddDecomposingObjectToFirstAggregation(this IfcObjectDefinition obj, IModel model,
                                                                 IfcObjectDefinition decomposingObject)
        {
            IfcRelAggregates rel =
                model.InstancesWhere<IfcRelAggregates>(r => r.RelatingObject == obj).FirstOrDefault() ??
                model.New<IfcRelAggregates>(r => r.RelatingObject = obj);

            rel.RelatedObjects.Add_Reversible(decomposingObject);
        }

        public static IfcMaterialSelect GetMaterial(this IfcObjectDefinition objDef)
        {
            IfcRelAssociatesMaterial relMat =  objDef.HasAssociations.OfType<IfcRelAssociatesMaterial>().FirstOrDefault();
            if (relMat != null)
                return relMat.RelatingMaterial;
            else
                return null;
        }
    }

}