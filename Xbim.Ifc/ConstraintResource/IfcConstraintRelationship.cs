#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcConstraintRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ConstraintResource
{
    /// <summary>
    ///   An IfcConstraintRelationship is an objectified relationship that enables instances of 
    ///   IfcConstraint and its subtypes to be associated to each other.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcConstraintRelationship : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                             INotifyPropertyChanging
    {
#if SupportActivation

        #region IPersistIfcEntity Members

        private long _entityLabel;
        private IModel _model;

        IModel IPersistIfcEntity.ModelOf
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

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            _model.Activate(this, true);
        }

        #endregion

#endif

        public IfcConstraintRelationship()
        {
            _relatedConstraints = new XbimSet<IfcConstraint>(this);
        }

        #region Fields

        private IfcLabel? _name;
        private IfcText? _description;
        private IfcConstraint _relatingConstraint;
        private XbimSet<IfcConstraint> _relatedConstraints;

        #endregion

        /// <summary>
        ///   A name used to identify or qualify the constraint aggregation.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcLabel? Name
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _name;
            }
            set { ModelManager.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }

        /// <summary>
        ///   A description that may apply additional information about a constraint aggregation.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcText? Description
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _description;
            }
            set { ModelManager.SetModelValue(this, ref _description, value, v => Description = v, "Description"); }
        }

        /// <summary>
        ///   Constraint to which the other Constraints are associated.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcConstraint RelatingConstraint
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingConstraint;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatingConstraint, value, v => RelatingConstraint = v,
                                           "RelatingConstraint");
            }
        }

        /// <summary>
        ///   Constraints that are aggregated in using the LogicalAggregator.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory, IfcAttributeType.List, IfcAttributeType.Class, 1)]
        public XbimSet<IfcConstraint> RelatedConstraints
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);

#endif
                return _relatedConstraints;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatedConstraints, value, v => RelatedConstraints = v,
                                           "RelatedConstraints");
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
                    _name = value.StringVal;
                    break;
                case 1:
                    _description = value.StringVal;
                    break;
                case 2:
                    _relatingConstraint = (IfcConstraint) value.EntityVal;
                    break;
                case 3:
                    _relatedConstraints.Add_Reversible((IfcConstraint) value.EntityVal);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            if (_relatedConstraints.Contains(_relatingConstraint))
                return
                    "WR11 ConstraintRelationship : The instance to which the relation RelatingConstraint points shall not be the same as the RelatedConstraint\n";
            return "";
        }

        #endregion
    }
}