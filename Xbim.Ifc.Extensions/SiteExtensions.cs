#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SiteExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.RepresentationResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class SiteExtensions
    {
        #region Representation methods

        public static IfcShapeRepresentation GetFootPrintRepresentation(this IfcSite site)
        {
            if (site.Representation != null)
                return
                    site.Representation.Representations.OfType<IfcShapeRepresentation>().FirstOrDefault(
                        r => string.Compare(r.RepresentationIdentifier.GetValueOrDefault(), "FootPrint", true) == 0);
            return null;
        }

        public static void AddBuilding(this IfcSite site, IfcBuilding building)
        {
            IEnumerable<IfcRelDecomposes> decomposition = site.IsDecomposedBy;
            if (decomposition.Count() == 0) //none defined create the relationship
            {
                IfcRelAggregates relSub = ModelManager.ModelOf(site).New<IfcRelAggregates>();
                relSub.RelatingObject = site;
                relSub.RelatedObjects.Add_Reversible(building);
            }
            else
            {
                decomposition.First().RelatedObjects.Add_Reversible(building);
            }
        }

        public static void AddSite(this IfcSite site, IfcSite subSite)
        {
            IEnumerable<IfcRelDecomposes> decomposition = site.IsDecomposedBy;
            if (decomposition.Count() == 0) //none defined create the relationship
            {
                IfcRelAggregates relSub = ModelManager.ModelOf(site).New<IfcRelAggregates>();
                relSub.RelatingObject = site;
                relSub.RelatedObjects.Add_Reversible(subSite);
            }
            else
            {
                decomposition.First().RelatedObjects.Add_Reversible(subSite);
            }
        }

        public static void AddElement(this IfcSite site, IfcProduct element)
        {
            IEnumerable<IfcRelContainedInSpatialStructure> relatedElements = site.ContainsElements;
            if (relatedElements.Count() == 0) //none defined create the relationship
            {
                IfcRelContainedInSpatialStructure relSe =
                    ModelManager.ModelOf(site).New<IfcRelContainedInSpatialStructure>();
                relSe.RelatingStructure = site;
                relSe.RelatedElements.Add_Reversible(element);
            }
            else
            {
                relatedElements.First().RelatedElements.Add_Reversible(element);
            }
        }

        #endregion
    }
}