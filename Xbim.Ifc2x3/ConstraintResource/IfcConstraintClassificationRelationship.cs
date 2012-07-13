#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcConstraintClassificationRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ConstraintResource
{
    /// <summary>
    ///   An IfcClassificationConstraintRelationship is an objectified relationship that enables assigning 
    ///   classifications to instances of IfcConstraint and its subtypes.
    /// </summary>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcConstraintClassificationRelationship : INotifyPropertyChanged, ISupportChangeNotification,
                                                           IPersistIfcEntity, INotifyPropertyChanging
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


        public IfcConstraintClassificationRelationship()
        {
            _relatedClassifications = new XbimSet<IfcClassificationNotationSelect>(this);
        }

        #region Fields

        private IfcConstraint _classifiedConstraint;
        private XbimSet<IfcClassificationNotationSelect> _relatedClassifications;

        #endregion

        /// <summary>
        ///   Constraint being classified
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcConstraint ClassifiedConstraint

        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _classifiedConstraint;
            }
            set
            {
                this.SetModelValue(this, ref _classifiedConstraint, value, v => ClassifiedConstraint = v,
                                           "ClassifiedConstraint");
            }
        }

        /// <summary>
        ///   Classifications of the constraint. .
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1)]
        public XbimSet<IfcClassificationNotationSelect> RelatedClassifications
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _relatedClassifications;
            }
            set
            {
                this.SetModelValue(this, ref _relatedClassifications, value, v => RelatedClassifications = v,
                                           "RelatedClassifications");
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
                    _classifiedConstraint = (IfcConstraint) value.EntityVal;
                    break;
                case 1:
                    _relatedClassifications.Add_Reversible((IfcClassificationNotationSelect) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}