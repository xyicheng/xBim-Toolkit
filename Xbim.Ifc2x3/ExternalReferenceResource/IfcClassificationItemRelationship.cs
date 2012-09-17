#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcClassificationItemRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ExternalReferenceResource
{
    /// <summary>
    ///   An IfcClassificationItemRelationship is a relationship class that enables the hierarchical structure of a classification system to be exposed through its ability to contain related classification items and to be contained by a relating classification item.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcClassificationItemRelationship is a relationship class that enables the hierarchical structure of a classification system to be exposed through its ability to contain related classification items and to be contained by a relating classification item.
    ///   HISTORY: New entity in IFC 2x. 
    ///   Use Definitions
    ///   IfcClassificationItem's can be progressively decomposed using the IfcClassificationItemRelationship such that the relationship always captures the information about the parent level (relating) item and the child level (related) items of which there can be many. The following example shows how this could be achieved for the Uniclass system.
    ///  
    ///   The inverse relationships from IfcClassificationItem to IfcClassificationRelationship enable information about the relationship to be recovered by the items concerned so that they are also aware of the decomposition. The cardinality of the inverse relationship is that an IfcClassificationItem can be the classifying item in only one relationship and can be a classified item in only one relationship. This implies that there is no overlap of IfcClassificationItem's. This reflects typical classification approaches which use strict hierarchical decomposition (or taxonomy) and do not have matrix relationships.
    ///   EXPRESS specification
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcClassificationItemRelationship : INotifyPropertyChanged, ISupportChangeNotification,
                                                     IPersistIfcEntity, INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private int _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public int EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        public IfcClassificationItemRelationship()
        {
            _relatedItems = new XbimSet<IfcClassificationItem>(this);
        }

        #region Fields

        private IfcClassificationItem _relatingItem;
        private XbimSet<IfcClassificationItem> _relatedItems;

        #endregion

        /// <summary>
        ///   The parent level item in a classification structure that is used for relating the child level items.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcClassificationItem RelatingItem
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _relatingItem;
            }
            set { this.SetModelValue(this, ref _relatingItem, value, v => RelatingItem = v, "RelatingItem"); }
        }

        /// <summary>
        ///   The child level items in a classification structure that are related to the parent level item.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1)]
        public XbimSet<IfcClassificationItem> RelatedItems
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _relatedItems;
            }
            set { this.SetModelValue(this, ref _relatedItems, value, v => RelatedItems = v, "RelatedItems"); }
        }

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        void ISupportChangeNotification.NotifyPropertyChanging(string propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        [field: NonSerialized] //don't serialize events
            private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion

        #region ISupportChangeNotification Members

        void ISupportChangeNotification.NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _relatingItem = (IfcClassificationItem) value.EntityVal;
                    break;
                case 1:
                    _relatedItems.Add_Reversible((IfcClassificationItem) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }


        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}