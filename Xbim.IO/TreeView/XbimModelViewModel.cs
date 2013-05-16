using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.Ifc2x3.Extensions;
using Xbim.XbimExtensions.Interfaces;
using System.ComponentModel;

namespace Xbim.IO.TreeView
{
    /// <summary>
    /// Model view for display top level Xbim Model contents and referenced models
    /// </summary>
    public class XbimModelViewModel : IXbimViewModel
     {
        XbimModel xbimModel;
        IfcProject _project;
        private bool _isSelected;
        private bool _isExpanded;
        private List<IXbimViewModel> children;

        public string Name
        {
            get
            {                
                return _project.Name;
            }
        }
        
        public XbimModelViewModel(IfcProject project)
        {
            xbimModel = project.ModelOf as XbimModel;
            _project=project;
            IEnumerable subs = this.Children; //call this once to preload first level of hierarchy   
        }



        public IEnumerable<IXbimViewModel> Children
        {
            get
            {
                if (children == null)
                {
                    children = new List<IXbimViewModel>();
                    foreach (var item in _project.GetSpatialStructuralElements())
                    {
                        children.Add(new SpatialViewModel(item));
                    }
                    foreach (var refModel in xbimModel.RefencedModels)
                    {
                        children.Add(new XbimModelViewModel(refModel.Model.IfcProject));
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
            get { return _project.EntityLabel; }
        }


        public IPersistIfcEntity Entity
        {
            get { return _project; }
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


        public void AddRefModel(XbimModelViewModel xbimModelViewModel)
        {
            //children.Add(xbimModelViewModel);
            //NotifyPropertyChanged("Children");
        }
     }
}
