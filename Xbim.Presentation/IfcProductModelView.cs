using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using System.Collections;
using Xbim.Ifc2x3.Kernel;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace Xbim.Presentation
{
    public class IfcProductModelView : IXbimViewModel
    {
        private IfcProduct product;
        private bool _isSelected;
        private bool _isExpanded;
        private ObservableCollection<IXbimViewModel> children;

        public IfcProductModelView(IfcProduct prod)
        { 
            this.product = prod;
        }

        public IEnumerable<IXbimViewModel> Children
        {
            get
            {
                if (children == null)
                {
                    children = new ObservableCollection<IXbimViewModel>();
                    List<IfcRelDecomposes> breakdown = product.IsDecomposedBy.ToList();
                    if (breakdown.Any())
                        foreach (var rel in breakdown)
                            foreach (var prod in rel.RelatedObjects.OfType<IfcProduct>())
                                children.Add(new IfcProductModelView(prod));

                }
                return children;
            }
        }

        public string Name
        {
            get { return product.ToString(); }
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
            get { return Math.Abs(product.EntityLabel); }
        }


        public XbimExtensions.Interfaces.IPersistIfcEntity Entity
        {
            get { return product; }
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
