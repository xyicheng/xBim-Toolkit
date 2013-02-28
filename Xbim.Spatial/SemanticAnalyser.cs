using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.StructuralAnalysisDomain;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Spatial
{
    public class SemanticAnalyser: ISpatialRelationsAnalyser
    {
        IModel _model;

        public SemanticAnalyser(IModel model)
        {
            _model = model;
        }

        public bool Equals(IfcProduct first, IfcProduct second)
        {
            if (first == second) return true;
            return false;
        }

        public bool Disjoint(IfcProduct first, IfcProduct second)
        {
            return false;
        }
        
        public bool Intersects(IfcProduct first, IfcProduct second)
        {
            return false;
        }

        public bool Touches(IfcProduct first, IfcProduct second)
        {
            //connects elements
            IEnumerable<IfcRelConnectsElements> connElemRels = _model.Instances.Where<IfcRelConnectsElements>
                (r => (r.RelatedElement == first && r.RelatingElement == second) || (r.RelatedElement == second && r.RelatingElement == first));
            if (connElemRels.FirstOrDefault() != null) return true;

            //Connects Path Elements
            IEnumerable<IfcRelConnectsPathElements> connPathElemRels = _model.Instances.Where<IfcRelConnectsPathElements>
                           (r => (r.RelatedElement == first && r.RelatingElement == second) || (r.RelatedElement == second && r.RelatingElement == first));
            if (connPathElemRels.FirstOrDefault() != null) return true;

            //Connects Port To Element
            IEnumerable<IfcRelConnectsPortToElement> connPortElemRels = _model.Instances.Where<IfcRelConnectsPortToElement>
            (r => (r.RelatedElement == first && r.RelatingPort == second) || (r.RelatedElement == second && r.RelatingPort == first));
            if (connPortElemRels.FirstOrDefault() != null) return true;

            //Connects Ports
            IEnumerable<IfcRelConnectsPorts> connPortsRels = _model.Instances.Where<IfcRelConnectsPorts>
                (r => (r.RelatedPort == first && r.RelatingPort == second) || (r.RelatedPort == second && r.RelatingPort == first));
            if (connPortsRels.FirstOrDefault() != null) return true;

            //Connects Structural Element
            IEnumerable<IfcRelConnectsStructuralElement> connStructRels = _model.Instances.Where<IfcRelConnectsStructuralElement>
                (r => (r.RelatedStructuralMember == first && r.RelatingElement == second) || (r.RelatedStructuralMember == second && r.RelatingElement == first));
            if (connStructRels.FirstOrDefault() != null) return true;

            //Connects Structural Member
            IEnumerable<IfcRelConnectsStructuralMember> connStructMemRels = _model.Instances.Where<IfcRelConnectsStructuralMember>
                (r => (r.RelatedStructuralConnection == first && r.RelatingStructuralMember == second) || (r.RelatedStructuralConnection == second && r.RelatingStructuralMember == first));
            if (connStructMemRels.FirstOrDefault() != null) return true;
            
            //Covers Bldg Elements
            IEnumerable<IfcRelCoversBldgElements> coversRels = _model.Instances.Where<IfcRelCoversBldgElements>
                (r => (r.RelatedCoverings.Contains(first) && r.RelatingBuildingElement == second) || (r.RelatedCoverings.Contains(second) && r.RelatingBuildingElement == first));
            if (coversRels.FirstOrDefault() != null) return true;

            //Covers Spaces
            IEnumerable<IfcRelCoversSpaces> coversSpacesRels = _model.Instances.Where<IfcRelCoversSpaces>
                (r => (r.RelatedCoverings.Contains(first) && r.RelatedSpace == second) || (r.RelatedCoverings.Contains(second) && r.RelatedSpace == first));
            if (coversSpacesRels.FirstOrDefault() != null) return true;
            
            //Space Boundary
            IEnumerable<IfcRelSpaceBoundary> spaceBoundRels = _model.Instances.Where<IfcRelSpaceBoundary>
                (r => (r.RelatedBuildingElement == first && r.RelatingSpace == second) || (r.RelatedBuildingElement == second && r.RelatingSpace == first));
            if (spaceBoundRels.FirstOrDefault() != null) return true;

            return false;
        }

        public bool Crosses(IfcProduct first, IfcProduct second)
        {
            return false;
        }

        public bool Within(IfcProduct first, IfcProduct second)
        {
            return Contains(second, first);
        }

        public bool Contains(IfcProduct first, IfcProduct second)
        {
            //this type of relation is always recursive
            if (first == second) return true;

            //check the case of spatial strucure element (specific relations)
            IfcSpatialStructureElement spatStruct = first as IfcSpatialStructureElement;
            if (spatStruct != null)
            {
                IEnumerable<IfcProduct> prods = GetProductsInSpatStruct(spatStruct);
                foreach (var prod in prods)
                {
                    if (Contains(prod, second)) return true;
                }
            }

            {
                IEnumerable<IfcProduct> prods = GetProductsInProds(first);
                foreach (var prod in prods)
                {
                    if (Contains(prod, second)) return true;
                }
            }

            return false;
        }

        private IEnumerable<IfcProduct> GetProductsInSpatStruct(IfcSpatialStructureElement spatialStruct)
        {
            //contained in spatial structure
            IEnumerable<IfcRelContainedInSpatialStructure> prodRels = 
                _model.Instances.Where<IfcRelContainedInSpatialStructure>(r => r.RelatingStructure == spatialStruct);
            foreach (var rel in prodRels)
            {
                foreach (var prod in rel.RelatedElements)
                {
                    yield return prod;
                }
            }

            //referenced in spatial structure
            IEnumerable<IfcRelReferencedInSpatialStructure> prodRefs = 
                _model.Instances.Where<IfcRelReferencedInSpatialStructure>(r => r.RelatingStructure == spatialStruct);
            foreach (var rel in prodRefs)
            {
                foreach (var prod in rel.RelatedElements)
                {
                    yield return prod;
                }
            }
        }

        private IEnumerable<IfcProduct> GetProductsInProds(IfcProduct prod)
        {
            throw new NotImplementedException("Not finished implementation");

            //aggregates

            //must be recursive

            //decomposes
            IEnumerable<IfcRelDecomposes> decompRels = _model.Instances.Where<IfcRelDecomposes>(r => r.RelatingObject == prod);
            foreach (var item in decompRels)
            {
                foreach (var p in item.RelatedObjects)
                {
                    IfcProduct product = p as IfcProduct;
                    yield return product;
                }
            }

            //fills
            IfcOpeningElement opening = prod as IfcOpeningElement;
            if (opening != null)
            {
                IEnumerable<IfcRelFillsElement> fillsRels = _model.Instances.Where<IfcRelFillsElement>(r => r.RelatingOpeningElement == opening);
                foreach (var rel in fillsRels)
                {
                    yield return rel.RelatedBuildingElement;
                }
            }


            //voids
            IfcElement element = prod as IfcElement;
            if (element != null)
            {
                IEnumerable<IfcRelVoidsElement> voidsRels = _model.Instances.Where<IfcRelVoidsElement>(r => r.RelatingBuildingElement == element);
                foreach (var rel in voidsRels)
                {
                    yield return rel.RelatedOpeningElement;
                }
            }
        }

        /// <summary>
        /// This cannot be established from the semantic relations
        /// </summary>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public bool Overlaps(IfcProduct first, IfcProduct second)
        {
            return false;
        }

        public bool Relate(IfcProduct first, IfcProduct second)
        {
            if (Equals(first, second)) return true;
            if (Intersects(first, second)) return true;
            if (Touches(first, second)) return true;
            if (Crosses(first, second)) return true;
            if (Contains(first, second)) return true;
            if (Within(first, second)) return true;
            if (Overlaps(first, second)) return true;
            return false;
        }

       
    }
}
