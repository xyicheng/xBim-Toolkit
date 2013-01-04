using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using System.Collections.ObjectModel;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions.Interfaces;
using System.Collections;

namespace Xbim.Presentation
{
    public class SpatialViewModel : IXbimViewModel
    {
        XbimModel xbimModel;
        int spatialStructureLabel;
        private ObservableCollection<IXbimViewModel> children;

        public string Name
        {
            get
            {
                return xbimModel.Instances[spatialStructureLabel].ToString();
            }
            set
            {
            }
        }
        
        public SpatialViewModel(IfcSpatialStructureElement spatialStructure)
        {
            xbimModel = spatialStructure.ModelOf as XbimModel;
            this.spatialStructureLabel = Math.Abs(spatialStructure.EntityLabel);
            IEnumerable subs = this.Children; //call this once to preload first level of hierarchy   
        }

        public SpatialViewModel(IfcProject project)
        {
            xbimModel = project.ModelOf as XbimModel;
            this.spatialStructureLabel = Math.Abs(project.EntityLabel);
            IEnumerable subs = this.Children; //call this once to preload first level of hierarchy          
        }

        public IEnumerable Children
        {
            get
            {
                if (children == null)
                {
                    children = new ObservableCollection<IXbimViewModel>();
                    IfcObjectDefinition space = xbimModel.Instances[spatialStructureLabel] as IfcObjectDefinition;
                    if (space != null)
                    {
                        IEnumerable<IfcRelAggregates> aggregate = space.IsDecomposedBy.OfType<IfcRelAggregates>();
                        foreach (IfcRelAggregates rel in aggregate)
                        {
                            foreach (IfcSpatialStructureElement subSpace in rel.RelatedObjects.OfType<IfcSpatialStructureElement>())
                                children.Add(new SpatialViewModel(subSpace));
                        }
                        //now add any contained elements
                        IfcSpatialStructureElement spatialElem = space as IfcSpatialStructureElement;
                        if (spatialElem != null)
                        {
                            //Select all the disting type names of elements for this
                            foreach (var type in spatialElem.ContainsElements.SelectMany(container=>container.RelatedElements).Select(r=>r.GetType()).Distinct())
                            {
                                children.Add(new ContainedElementsViewModel(spatialElem, type));
                            }
                        }
                    }
                }
                return children;
            }
        }
        public bool HasItems
        {
            get
            {
                IEnumerable subs = this.Children; //call this once to preload first level of hierarchy          
                return children.Count > 0;
            }
        }

        public bool IsSelected { get; set; }

        public bool IsExpanded { get; set; }



        public int EntityLabel
        {
            get { return spatialStructureLabel; }
        }


        public IPersistIfcEntity Entity
        {
            get { return xbimModel.Instances[spatialStructureLabel]; }
        }
    }


}
