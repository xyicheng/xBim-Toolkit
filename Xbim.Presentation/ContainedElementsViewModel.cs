using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using System.Collections.ObjectModel;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using System.Collections;

namespace Xbim.Presentation
{
    public class ContainedElementsViewModel:IXbimViewModel
    {
        XbimModel xbimModel;
        Type type;
        int spatialContainerLabel;
        private ObservableCollection<IXbimViewModel> children;

        public string Name
        {
            get
            {
                return type.Name;
            }
        }


        public ContainedElementsViewModel(IfcSpatialStructureElement container)
        {
            xbimModel = container.ModelOf as XbimModel;
            IEnumerable subs = this.Children; //call this once to preload first level of hierarchy          
        }

        public ContainedElementsViewModel(IfcSpatialStructureElement spatialElem, Type type)
        {

            this.spatialContainerLabel = Math.Abs(spatialElem.EntityLabel);
            this.type = type;
            this.xbimModel = (XbimModel) spatialElem.ModelOf;
        }


        public IEnumerable Children
        {
            get
            {
                if (children == null)
                {
                    children = new ObservableCollection<IXbimViewModel>();
                    IfcSpatialStructureElement space = xbimModel.Instances[spatialContainerLabel] as IfcSpatialStructureElement;
                    foreach (var rel in space.ContainsElements)
                    {
                        foreach (IfcProduct prod in rel.RelatedElements.Where(e => e.GetType() == type))
                            children.Add(new IfcProductModelView(prod));
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
            get { return 0; }
        }


        public XbimExtensions.Interfaces.IPersistIfcEntity Entity
        {
            get { return xbimModel.Instances[spatialContainerLabel]; }
        }
    }
}
