using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Collections.ObjectModel;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM
{
    public class XbimClassificationItem
    {

        IfcClassificationItem _ifcClassificationItem;
        List<XbimClassificationItem> _children;

        public List<XbimClassificationItem> Children
        {
            get { return _children; }
            
        }
        XbimClassificationItem _parent = null;

        public XbimClassificationItem Parent
        {
            get { return _parent; }
            internal set { _parent = value; }
        }
        public XbimClassificationItem(XbimDocument document, XbimClassification system,  string notation, string title )
        {
            _ifcClassificationItem = document.Model.New<IfcClassificationItem>();
            IfcClassificationNotationFacet facet = document.Model.New<IfcClassificationNotationFacet>(f => f.NotationValue = notation);
            _ifcClassificationItem.Notation = facet;
            _ifcClassificationItem.Title = title;
            system.AllItems.Add(this);

        }

        public void AddChildItem(XbimClassificationItem item)
        {
            if (_children == null) _children = new List<XbimClassificationItem>();
            _children.Add(item);
            item.Parent = this;
        }
        public string Notation
        {
            get { return _ifcClassificationItem.Notation.NotationValue; }
        }

        public string Title
        {
            get { return _ifcClassificationItem.Title; }
        }

        public void Classify(IXbimRoot obj)
        {
            IModel model = _ifcClassificationItem.ModelOf;
            IfcRelAssociatesClassification rel = model.InstancesWhere<IfcRelAssociatesClassification>(r => r.RelatingClassification == this).FirstOrDefault();
            if (rel == null)
            {
                rel = model.New<IfcRelAssociatesClassification>();
               
            }
            IfcClassificationNotation notation = model.New<IfcClassificationNotation>();
            notation.NotationFacets.Add_Reversible(_ifcClassificationItem.Notation);
            rel.RelatingClassification = notation;
            rel.RelatedObjects.Add_Reversible(obj.AsRoot);
        }
    }


    public class XbimClassificationItemCollection : KeyedCollection<string, XbimClassificationItem>
    {
        protected override string GetKeyForItem(XbimClassificationItem item)
        {
            return item.Notation;
        }
    }
}
