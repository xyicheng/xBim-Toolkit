using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using System.Collections;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Presentation
{
    public class IfcProductModelView : IXbimViewModel
    {
        private IfcProduct product;

        public IfcProductModelView(IfcProduct prod)
        { 
            this.product = prod;
        }

        public IEnumerable Children
        {
            get { return Enumerable.Empty<object>(); }
        }

        public string Name
        {
            get { return product.ToString(); }
        }

        public bool HasItems
        {
            get
            {
                return false;
            }
        }

        public bool IsSelected { get; set; }

        public bool IsExpanded { get; set; }


        public int EntityLabel
        {
            get { return Math.Abs(product.EntityLabel); }
        }


        public XbimExtensions.Interfaces.IPersistIfcEntity Entity
        {
            get { return product; }
        }
    }
}
