#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMaterialClassificationRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.MaterialResource
{
    /// <summary>
    ///   Relationship assigning classifications to materials.
    /// </summary>
    /// <remarks>
    ///   HISTORY: New entity in Release IFC2x.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcMaterialClassificationRelationship : INotifyPropertyChanged, ISupportChangeNotification,
                                                         IPersistIfcEntity, INotifyPropertyChanging
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

        public IfcMaterialClassificationRelationship()
        {
            _materialClassifications = new XbimSet<IfcClassificationNotationSelect>(this);
        }

        #region Fields

        private XbimSet<IfcClassificationNotationSelect> _materialClassifications;
        private IfcMaterial _classifiedMaterial;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The material classifications identifying the type of material.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1)]
        public XbimSet<IfcClassificationNotationSelect> MaterialClassifications
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _materialClassifications;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _materialClassifications, value, v => MaterialClassifications = v,
                                           "MaterialClassifications");
            }
        }

        /// <summary>
        ///   Material being classified.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcMaterial ClassifiedMaterial
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _classifiedMaterial;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _classifiedMaterial, value, v => ClassifiedMaterial = v,
                                           "ClassifiedMaterial");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _materialClassifications.Add((IfcClassificationNotationSelect) value.EntityVal);
                    break;
                case 1:
                    _classifiedMaterial = (IfcMaterial) value.EntityVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
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
    }
}