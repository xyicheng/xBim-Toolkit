#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcClassification.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ExternalReferenceResource
{
    /// <summary>
    ///   An IfcClassification is used for the arrangement of objects into a class or category according to a common purpose or their possession of common characteristics.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcClassification is used for the arrangement of objects into a class or category according to a common purpose or their possession of common characteristics. 
    ///   HISTORY: New class in IFC Release 1.5. Modified in IFC 2x 
    ///   Use Definitions
    ///   IfcClassification identifies the classification system or source from which a classification notation is derived. The objective is to minimize the number of IfcClassification objects contained within an IFC model. Ideally, each classification system or source used should have only one IfcClassification object. However, because multiple classification is allowed, there may be many IfcClassification objects used, each identifying a different classification system or source.
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcClassification : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                     INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private long _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, long entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public long EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        private IfcLabel _source;
        private IfcLabel _edition;
        private IfcCalendarDate _editionDate;
        private IfcLabel _name;

        /// <summary>
        ///   Source (or publisher) for this classification.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLabel Source
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _source;
            }
            set { this.SetModelValue(this, ref _source, value, v => Source = v, "Source"); }
        }

        /// <summary>
        ///   The edition or version of the classification system from which the classification notation is derived.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLabel Edition
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _edition;
            }
            set { this.SetModelValue(this, ref _edition, value, v => Edition = v, "Edition"); }
        }

        /// <summary>
        ///   Optional. The date on which the edition of the classification used became valid.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcCalendarDate EditionDate
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _editionDate;
            }
            set { this.SetModelValue(this, ref _editionDate, value, v => EditionDate = v, "EditionDate"); }
        }

        /// <summary>
        ///   The name or label by which the classification used is normally known.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcLabel Name
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _name;
            }
            set { this.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }

        /// <summary>
        ///   Inverse. Classification items that are classified by the classification.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcClassificationItem> Contains
        {
            get { return ModelOf.InstancesWhere<IfcClassificationItem>(ci => ci.ItemOf == this); }
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
                    _source = value.StringVal;
                    break;
                case 1:
                    _edition = value.StringVal;
                    break;
                case 2:
                    _editionDate = (IfcCalendarDate) value.EntityVal;
                    break;
                case 3:
                    _name = value.StringVal;
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