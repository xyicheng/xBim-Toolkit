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
using System.ComponentModel;

namespace Xbim.Presentation
{
    public class SpatialViewModel : IXbimViewModel
    {
        XbimModel xbimModel;
        int spatialStructureLabel;
        private bool _isSelected;
        private bool _isExpanded;
        private ObservableCollection<IXbimViewModel> children;

        public string Name
        {
            get
            {
                IPersistIfcEntity ent = xbimModel.Instances[spatialStructureLabel];
                return ent.ToString();
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

        public IEnumerable<IXbimViewModel> Children
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

       


        public int EntityLabel
        {
            get { return spatialStructureLabel; }
        }


        public IPersistIfcEntity Entity
        {
            get { return xbimModel.Instances[spatialStructureLabel]; }
        }

        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public bool IsExpanded
        {
            get
            {
                return _isExpanded;
            }
            set
            {
                _isExpanded = value;
                NotifyPropertyChanged("IsExpanded");
            }
        }
        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
        private event PropertyChangedEventHandler PropertyChanged;


        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }
        void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }


}
