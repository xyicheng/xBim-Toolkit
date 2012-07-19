using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc.ProductExtension;

namespace Xbim.IO.Tree
{
    public abstract class TreeQueryStrategy
    {
        protected IModel _model;
        protected Dictionary<IfcObjectDefinition, CompositionNode> _nodeMap;
        protected Type[] FamilyTypes = new Type[] 
        {
            typeof(Xbim.Ifc.SharedBldgElements.IfcWall),
            typeof(Xbim.Ifc.SharedBldgElements.IfcSlab),
            typeof(Xbim.Ifc.SharedBldgElements.IfcRoof),
            typeof(Xbim.Ifc.ProductExtension.IfcSpace),
            typeof(Xbim.Ifc.SharedBldgElements.IfcStair),
            typeof(Xbim.Ifc.SharedBldgElements.IfcDoor),
            typeof(Xbim.Ifc.SharedBldgElements.IfcWindow),
            typeof(Xbim.Ifc.SharedBldgElements.IfcBeam),
            typeof(Xbim.Ifc.SharedBldgElements.IfcColumn),
            typeof(Xbim.Ifc.ProductExtension.IfcElectricalElement),
            typeof(Xbim.Ifc.ProductExtension.IfcFurnishingElement),
            typeof(Xbim.Ifc.ProductExtension.IfcDistributionElement)
        };

        public Dictionary<IfcObjectDefinition, CompositionNode> NodeMap
        {
            get
            {
                return _nodeMap;
            }
        } 

        public virtual TreeNodes GetTreeStructure()
        {
            return new TreeNodes();
        }

        protected void Initialise()
        {
            _nodeMap = new Dictionary<IfcObjectDefinition, CompositionNode>
                    (_model.InstancesOfType<IfcRelDecomposes>().Count());
        }

        protected CompositionNode LocateProjectNode()
        {
            CompositionNode root = null;

            IfcProject project = _model.InstancesOfType<IfcProject>().FirstOrDefault();
            if (project != null)
            {
                CompositionNode projectNode;
                if (NodeMap.TryGetValue(project, out projectNode))
                {
                    projectNode.IsRoot = true;
                    root = projectNode;
                }
            }
            //if (root == null)
            //{
            //    Trace.TraceWarning("No IfcProject located in Model");
            //}
            return root;
        }

        protected void LoadFamilyElements(Type type, FamilyNode family)
        {
            var products = from prod in _model.InstancesWhere<IfcProduct>(p => type.IsAssignableFrom(p.GetType()))
                           //orderby prod.Name
                           select prod;

            foreach (IfcProduct product in products)
            {
                ElementNode element = new ElementNode(product);
                family.Children.Add(element);
            }
        }

        protected void AddRelComposes()
        {
            foreach (IfcRelDecomposes rel in _model.InstancesOfType<IfcRelDecomposes>())
            {
                if (rel.RelatingObject != null)
                {
                    // Get the subject of the decomposition from the relationship
                    var parentRel = rel.RelatingObject;
                    if (parentRel == null)
                    { continue; }
                    //var parent = parentRel.RelatingObject;

                    CompositionNode treeItem;
                    if (!NodeMap.TryGetValue(parentRel, out treeItem))
                    {
                        treeItem = new CompositionNode(parentRel);
                        NodeMap.Add(parentRel, treeItem);
                    }
                    AddRelatedObjects(rel, treeItem);
                }
            }
        }

        protected void AddRelatedObjects(IfcRelDecomposes rel, CompositionNode treeItem)
        {
            foreach (IfcObjectDefinition child in rel.RelatedObjects)
            {
                CompositionNode childItem;
                if (!NodeMap.TryGetValue(child, out childItem)) //already written
                {
                    childItem = new CompositionNode(child);
                    NodeMap.Add(child, childItem);

                }
                treeItem.Children.Add(childItem);

            }
        }

        protected void AddRelContained()
        {

            foreach (IfcRelContainedInSpatialStructure scRel in
                _model.InstancesOfType<IfcRelContainedInSpatialStructure>())
            {
                if (scRel.RelatingStructure != null)
                {
                    CompositionNode treeItem;
                    if (!NodeMap.TryGetValue(scRel.RelatingStructure, out treeItem)) //already written
                    {
                        treeItem = new CompositionNode(scRel.RelatingStructure);
                        NodeMap.Add(scRel.RelatingStructure, treeItem);

                    }
                    AddRelatedElements(scRel, treeItem);
                }
            }
        }

        protected void AddRelatedElements(IfcRelContainedInSpatialStructure scRel, CompositionNode treeItem)
        {
            var applicableTypes = scRel.RelatedElements
                .Where(t => !t.GetType().IsSubclassOf(typeof(IfcFeatureElementSubtraction)));
            foreach (IfcObjectDefinition child in applicableTypes)
            {
                CompositionNode childItem;
                if (!NodeMap.TryGetValue(child, out childItem)) //already written
                {
                    childItem = new CompositionNode(child);
                    NodeMap.Add(child, childItem);

                }
                //Node family = GetFamily(treeItem, child);
                // TODO: Add child to family, not treeItem
                treeItem.Children.Add(childItem);
            }
        }
    }
}
