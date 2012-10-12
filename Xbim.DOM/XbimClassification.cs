using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.XbimExtensions;

namespace Xbim.DOM
{
    public class XbimClassification
    {

        List<XbimClassificationItem> _roots = new List<XbimClassificationItem>(1);
        XbimClassificationItemCollection _allItems = new XbimClassificationItemCollection();

        internal XbimClassificationItemCollection AllItems
        {
            get { return _allItems; }

        }
      
        public XbimClassification(XbimDocument document, string publisherId, string name, string edition, DateTime? date)
        {
            IfcClassification _classification =  document.Model.Instances.New<IfcClassification>();
            _classification.Source = publisherId;
            _classification.Name = name;
            _classification.Edition = edition;
            if (date != null && date.HasValue)
            {
                _classification.EditionDate.DayComponent = date.Value.Day;
                _classification.EditionDate.MonthComponent = date.Value.Month;
                _classification.EditionDate.YearComponent = date.Value.Year;
            }

        }

        public XbimClassificationItem this [string notation]
        {
            get
            {
                if (_allItems.Contains(notation))
                    return _allItems[notation];
                else
                    return null;
            }

        }

        public void AddRootItem(XbimClassificationItem item)
        {
            if (item == null)
                throw new ArgumentNullException("AddRootItem", "XbimClassificationItem may not be null");
            _roots.Add(item);
            
        }
    }
}
