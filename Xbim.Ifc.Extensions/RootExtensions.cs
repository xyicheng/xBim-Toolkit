#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    RootExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Diagnostics;
using System.Linq;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.XbimExtensions;
using System;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.Extensions
{
    public static class RootExtensions
    {
        public static IfcClassificationNotation GetFirstClassificationNotation(this IfcRoot root, IModel model)
        {
            IfcRelAssociatesClassification rel =
                model.InstancesWhere<IfcRelAssociatesClassification>(r => r.RelatedObjects.Contains(root)).
                    FirstOrDefault();
            if (rel == null) return null;
            IfcClassificationNotationSelect notationSelect = rel.RelatingClassification;
            if (notationSelect is IfcClassificationReference)
            {
                Debug.WriteLine(
                    "Classification relation does not contain classification notation, but it contains external reference.");
                return null;
            }
            return notationSelect as IfcClassificationNotation;
        }

        //public static IfcClassificationNotationFacet GetOrCreateClassicifationNotationFacet(this IfcRoot root, IModel Model, string ClassificationFacet)

        /// <summary>
        /// Returns the Material Select or creates
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static void SetMaterial(this IfcRoot obj, IfcMaterialSelect matSel)
        {
            if (obj is IfcTypeProduct && matSel is IfcMaterialLayerSetUsage)
                throw new Exception("IfcElementType cannot have an IfcMaterialLayerSetUsage as it's associated material");

            IModel model = (obj as IPersistIfcEntity).ModelOf;

            IfcRelAssociatesMaterial relMat = model.InstancesWhere<IfcRelAssociatesMaterial>(r => r.RelatedObjects.Contains(obj)).FirstOrDefault();
            if (relMat == null)
            {
                relMat = model.New<IfcRelAssociatesMaterial>();
                relMat.RelatedObjects.Add_Reversible(obj);
            }
            relMat.RelatingMaterial = matSel;

        }

        public static IfcMaterialSelect GetMaterial(this IfcRoot obj)
        {
            IModel model = (obj as IPersistIfcEntity).ModelOf;

            IfcRelAssociatesMaterial relMat = model.InstancesWhere<IfcRelAssociatesMaterial>(r => r.RelatedObjects.Contains(obj)).FirstOrDefault();
            if (relMat != null)
                return relMat.RelatingMaterial;
            else
                return null;

        }

        /// <summary>
        ///   Returns the MaterialLayerSetUsage for the Building element, null if none exists
        /// </summary>
        /// <param name = "element"></param>
        /// <param name = "model"></param>
        public static IfcMaterialLayerSetUsage GetMaterialLayerSetUsage(this IfcRoot element, IModel model)
        {
            IfcRelAssociatesMaterial rel =
                model.InstancesWhere<IfcRelAssociatesMaterial>(r => r.RelatedObjects.Contains(element)).FirstOrDefault();
            if (rel != null)
                return (rel.RelatingMaterial is IfcMaterialLayerSetUsage)
                           ? (IfcMaterialLayerSetUsage)rel.RelatingMaterial
                           : null;
            else
                return null;
        }

        public static IfcMaterialLayerSetUsage GetOrCreateLayerSetUsage(this IfcRoot element, IModel model)
        {
            IfcMaterialLayerSetUsage result = GetMaterialLayerSetUsage(element, model);
            if (result != null) return result;

            result = model.New<IfcMaterialLayerSetUsage>();

            //create relation
            IfcRelAssociatesMaterial rel = model.New<IfcRelAssociatesMaterial>();
            rel.RelatedObjects.Add_Reversible(element);
            rel.RelatingMaterial = result;

            return result;
        }


        /// <summary>
        ///   Set Material set usage parameters and creates it if it doesn't exist.
        /// </summary>
        /// <param name = "model">Model of the element</param>
        /// <param name = "forLayerSet">Material layer set for the usage</param>
        /// <param name = "layerSetDirection">Direction of the material layer set in the usage</param>
        /// <param name = "directionSense">Sense of the direction of the usage</param>
        /// <param name = "offsetFromReferenceLine">Offset from the reference line of the element</param>
        public static void SetMaterialLayerSetUsage(this IfcRoot element, IModel model,
                                                    IfcMaterialLayerSet forLayerSet,
                                                    IfcLayerSetDirectionEnum layerSetDirection,
                                                    IfcDirectionSenseEnum directionSense,
                                                    IfcLengthMeasure offsetFromReferenceLine)
        {
            //if some input is not correct, material layer set usage is not created or changed
            if (element == null || forLayerSet == null) return;

            IfcMaterialLayerSetUsage materialUsage = element.GetOrCreateLayerSetUsage(model);
            materialUsage.ForLayerSet = forLayerSet;
            materialUsage.LayerSetDirection = layerSetDirection;
            materialUsage.DirectionSense = directionSense;
            materialUsage.OffsetFromReferenceLine = offsetFromReferenceLine;
        }

        /// <summary>
        ///   Set Material set usage and creates it if it doesn't exist.
        /// </summary>
        /// <param name = "forLayerSet">Material layer set for the usage</param>
        /// <param name = "layerSetDirection">Direction of the material layer set in the usage</param>
        /// <param name = "directionSense">Sense of the direction of the usage</param>
        /// <param name = "offsetFromReferenceLine">Offset from the reference line of the element</param>
        public static void SetMaterialLayerSetUsage(this IfcRoot element, IfcMaterialLayerSet forLayerSet,
                                                    IfcLayerSetDirectionEnum layerSetDirection,
                                                    IfcDirectionSenseEnum directionSense,
                                                    IfcLengthMeasure offsetFromReferenceLine)
        {
            IModel model = element.ModelOf;
            element.SetMaterialLayerSetUsage(model, forLayerSet, layerSetDirection, directionSense,
                                             offsetFromReferenceLine);
        }
    }
}