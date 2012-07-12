#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcProperty.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PropertyResource
{
    public class UniquePropertyNameComparer : IEqualityComparer<IfcProperty>
    {
        #region IEqualityComparer<Property> Members

        public bool Equals(IfcProperty x, IfcProperty y)
        {
            return x.Name == y.Name;
        }

        public int GetHashCode(IfcProperty obj)
        {
            return obj.Name.GetHashCode();
        }

        #endregion
    }

    /// <summary>
    ///   Set Of Properties, the Name of each property in the set must be unique
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class SetOfProperty : XbimSet<IfcProperty>
    {
        internal SetOfProperty(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    /// <summary>
    ///   Definition from IAI: An abstract generalization for all types of properties that can be associated with IFC objects through the property set mechanism.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public abstract class IfcProperty : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        #endregion

#endif

        #region Fields

        private IfcIdentifier _name;
        private IfcText? _description;

        #endregion

        #region Constructors

        internal IfcProperty(IfcIdentifier name)
        {
            _name = name;
        }

        public IfcProperty()
        {
        }

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Name for this property. This label is the significant name string that defines the semantic meaning for the property.
        /// </summary>
        [DataMember(Order = 0)]
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcIdentifier Name
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
        ///   Optional. Informative text to explain the property.
        /// </summary>
        [DataMember(Order = 1, EmitDefaultValue = false, IsRequired = false)]
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

        #endregion

        /// <summary>
        ///   Inverse. The property on whose value that of another property depends.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcPropertyDependencyRelationship> PropertyForDependance
        {
            get
            {
                return
                    ModelManager.ModelOf(this).InstancesWhere<IfcPropertyDependencyRelationship>(
                        p => p.DependingProperty == this);
            }
        }

        /// <summary>
        ///   Inverse. The relating property on which the value of the property depends.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcPropertyDependencyRelationship> PropertyDependsOn
        {
            get
            {
                return
                    ModelManager.ModelOf(this).InstancesWhere<IfcPropertyDependencyRelationship>(
                        p => p.DependantProperty == this);
            }
        }

        /// <summary>
        ///   Inverse. Reference to the IfcComplexProperty in which the IfcProperty is contained.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcComplexProperty> PartOfComplex
        {
            get { return ModelManager.ModelOf(this).InstancesWhere<IfcComplexProperty>(c => c.HasProperties.Contains(this)); }
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

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _name = value.StringVal;
                    break;
                case 1:
                    _description = value.StringVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public abstract string WhereRule();
    }
}