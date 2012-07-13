#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMaterial.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.RepresentationResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.MaterialResource
{
    /// <summary>
    ///   A homogenious substance that can be used to form elements.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: A homogenious substance that can be used to form elements. 
    ///   NOTE  In this IFC Release only isotropic materials are allowed for. In future releases also anistropic materials and their usage may be considered. 
    ///   HISTORY  New entity in IFC1.0
    ///   IFC2x Edition 3 CHANGE  The inverse attribute HasRepresentation has been added. Upward compatibility for file based exchange is guaranteed.
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcMaterial : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity, IfcMaterialSelect,
                               IfcObjectReferenceSelect, INotifyPropertyChanging
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

        #region Fields

        private IfcLabel _name;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Name of the matsel.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLabel Name
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _name;
            }
            set { this.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
            {
                _name = value.StringVal;
            }
            else
                this.HandleUnexpectedAttribute(propIndex, value);
        }

        #endregion

        #region Inverse Relationships

        /// <summary>
        ///   Reference to the IfcMaterialDefinitionRepresentation that provides presentation information to a representation common to this matsel in style definitions.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcMaterialDefinitionRepresentation> HasRepresentation
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcMaterialDefinitionRepresentation>(
                        m => m.RepresentedMaterial == this);
            }
        }

        /// <summary>
        ///   Reference to the relationship pointing to the classification(s) of the matsel.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcMaterialClassificationRelationship> ClassifiedAs
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcMaterialClassificationRelationship>(
                        c => c.ClassifiedMaterial == this);
            }
        }

        #endregion

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

        public string WhereRule()
        {
            return "";
        }

        #endregion

        #region MaterialSelect Members

        string IfcMaterialSelect.Name
        {
            get { return Name; }
        }

        #endregion
    }
}