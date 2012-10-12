#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcClassificationItem.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ExternalReferenceResource
{
    /// <summary>
    ///   An IfcClassificationItem is a class of classification notations used.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcClassificationItem is a class of classification notations used. 
    ///   HISTORY: New entity in IFC Release 2x. 
    ///   Use Definitions
    ///   The term 'classification item' is used in preference to the term 'table' for improved flexibility. For example, the classification item "L681" in Uniclass may be used to contain all subsequent notation facets within that class of classifications which has the title "Proofings, insulation" (e.g. L6811, L6812, L6813 etc.).
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcClassificationItem : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                         INotifyPropertyChanging
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


        #region Fields

        private IfcClassificationNotationFacet _notation;
        private IfcClassification _itemOf;
        private IfcLabel _title;

        #endregion

        /// <summary>
        ///   The notations from within a classification item that are used within the project.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcClassificationNotationFacet Notation
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _notation;
            }
            set { this.SetModelValue(this, ref _notation, value, v => Notation = v, "Notation"); }
        }

        /// <summary>
        ///   The classification that is the source for the uppermost level of the classification item hierarchy used.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcClassification ItemOf
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _itemOf;
            }
            set { this.SetModelValue(this, ref _itemOf, value, v => ItemOf = v, "ItemOf"); }
        }

        /// <summary>
        ///   The name of the classification item.
        /// </summary>
        /// <remarks>
        ///   NOTE: Examples of the above attributes from Uniclass: 
        ///   A classification item in Uniclass has a notation "L6814" which has the title "Tanking".
        ///   It has a parent notation "L681" which has the title "Proofings, insulation".
        /// </remarks>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcLabel Title
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _title;
            }
            set { this.SetModelValue(this, ref _title, value, v => Title = v, "Title"); }
        }

        /// <summary>
        ///   Identifies the relationship in which the role of ClassifiedItem is taken.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcClassificationItemRelationship> IsClassifiedItemIn
        {
            get
            {
                return
                    ModelOf.Instances.Where<IfcClassificationItemRelationship>(
                        c => c.RelatedItems.Contains(this));
            }
        }

        /// <summary>
        ///   Identifies the relationship in which the role of ClassifyingItem is taken.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcClassificationItemRelationship> IsClassifyingItemIn
        {
            get
            {
                return
                    ModelOf.Instances.Where<IfcClassificationItemRelationship>(
                        c => c.RelatingItem == this);
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
                    _notation = (IfcClassificationNotationFacet) value.EntityVal;
                    break;
                case 1:
                    _itemOf = (IfcClassification) value.EntityVal;
                    break;
                case 2:
                    _title = value.StringVal;
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