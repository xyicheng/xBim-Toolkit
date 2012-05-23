#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SpatialStructureElementExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class SpatialStructureElementExtensions
    {
        /// <summary>
        ///   Returns all the  elements that decomposes this
        /// </summary>
        /// <param name = "se"></param>
        /// <param name = "model"></param>
        /// <returns></returns>
        public static IEnumerable<IfcProduct> GetContainedElements(this IfcSpatialStructureElement se, IModel model)
        {
            return
                model.InstancesWhere<IfcRelContainedInSpatialStructure>(r => r.RelatingStructure == se).SelectMany(
                    subrel => subrel.RelatedElements);
        }

        /// <summary>
        ///   Returns all the elements that decomposes this
        /// </summary>
        /// <param name = "se"></param>
        /// <returns></returns>
        public static IEnumerable<IfcProduct> GetContainedElements(this IfcSpatialStructureElement se)
        {
            return
                ModelManager.ModelOf(se).InstancesWhere<IfcRelContainedInSpatialStructure>(
                    r => r.RelatingStructure == se).SelectMany(subrel => subrel.RelatedElements);
        }

        /// <summary>
        ///   Returns  the first spatial structural element that this decomposes
        /// </summary>
        /// <param name = "se"></param>
        /// <returns></returns>
        public static IfcSpatialStructureElement GetContainingStructuralElement(this IfcSpatialStructureElement se)
        {
            IModel model = ModelManager.ModelOf(se);
            IEnumerable<IfcRelContainedInSpatialStructure> rels =
                model.InstancesWhere<IfcRelContainedInSpatialStructure>(r => r.RelatedElements.Contains(se));
            return rels.Select(r => r.RelatingStructure).FirstOrDefault();
            // return  ModelManager.ModelOf(se).InstancesWhere<RelContainedInSpatialStructure>(r => r.RelatedElements.Contains(se)).Select(r=>r.RelatingStructure).FirstOrDefault();
        }

        /// <summary>
        ///   Returns  the spatial structural elements that this decomposes
        /// </summary>
        /// <param name = "se"></param>
        /// <returns></returns>
        public static IEnumerable<IfcSpatialStructureElement> GetContainingStructuralElements(
            this IfcSpatialStructureElement se)
        {
            IModel model = ModelManager.ModelOf(se);
            IEnumerable<IfcRelContainedInSpatialStructure> rels =
                model.InstancesWhere<IfcRelContainedInSpatialStructure>(r => r.RelatedElements.Contains(se));
            return rels.Select(r => r.RelatingStructure);
            // return  ModelManager.ModelOf(se).InstancesWhere<RelContainedInSpatialStructure>(r => r.RelatedElements.Contains(se)).Select(r=>r.RelatingStructure).FirstOrDefault();
        }

        /// <summary>
        ///   Adds the  element to the set of  elements which are contained in this spatialstructure
        /// </summary>
        /// <param name = "se"></param>
        /// <param name = "prod"></param>
        public static void AddElement(this IfcSpatialStructureElement se, IfcProduct prod)
        {
            IEnumerable<IfcRelContainedInSpatialStructure> relatedElements = se.ContainsElements;
            if (relatedElements.Count() == 0) //none defined create the relationship
            {
                IfcRelContainedInSpatialStructure relSe =
                    ModelManager.ModelOf(se).New<IfcRelContainedInSpatialStructure>();
                relSe.RelatingStructure = se;
                relSe.RelatedElements.Add_Reversible(prod);
            }
            else
            {
                relatedElements.First().RelatedElements.Add_Reversible(prod);
            }
        }

        /// <summary>
        ///   Adds specified IfcSpatialStructureElement to the decomposition of this spatial structure element.
        /// </summary>
        /// <param name = "se"></param>
        /// <param name = "child">Child spatial structure element.</param>
        public static void AddToSpatialDecomposition(this IfcSpatialStructureElement se,
                                                     IfcSpatialStructureElement child)
        {
            IEnumerable<IfcRelDecomposes> decomposition = se.IsDecomposedBy;
            if (decomposition.Count() == 0) //none defined create the relationship
            {
                IfcRelAggregates relSub = ModelManager.ModelOf(se).New<IfcRelAggregates>();
                relSub.RelatingObject = se;
                relSub.RelatedObjects.Add_Reversible(child);
            }
            else
            {
                decomposition.First().RelatedObjects.Add_Reversible(child);
            }
        }
    }
}