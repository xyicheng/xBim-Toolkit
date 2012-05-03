#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRepresentationContext.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.RepresentationResource
{
    [IfcPersistedEntity, Serializable]
    public class RepresentationContextSet : XbimSet<IfcRepresentationContext>
    {
        internal RepresentationContextSet(IPersistIfcEntity owner)
            : base(owner)
        {
        }

        public override bool Remove_Reversible(IfcRepresentationContext item)
        {
            if (item.RepresentationsInContext.Count() != 0)
                throw new Exception("RepresentationContext cannot be removed, Representations still reference it");
            return base.Remove_Reversible(item);
        }

        public override void Clear_Reversible()
        {
            foreach (IfcRepresentationContext rc in this)
            {
                if (rc.RepresentationsInContext.Count() != 0)
                    throw new Exception(
                        "RepresentationContext cannot be cleared, Representations still reference some of the contexts it");
            }
            base.Clear_Reversible();
        }

        /// <summary>
        ///   Returns the Mandatory Model 3DView of ContextType = "Model"
        /// </summary>
        public IfcRepresentationContext ModelView
        {
            get { return this.FirstOrDefault(inst => inst.ContextType == "Model"); }
        }

        public IfcRepresentationContext this[string contextIdentifier]
        {
            get
            {
                foreach (IfcRepresentationContext context in this)
                {
                    if (context.ContextIdentifier == contextIdentifier)
                        return context;
                }
                return null;
            }
        }
    }

    /// <summary>
    ///   A representation context is a context in which a set of representation items are related.
    /// </summary>
    /// <remarks>
    ///   Definition from ISO/CD 10303-42:1992: A representation context is a context in which a set of representation items are related. 
    ///   Definition from IAI: The IfcRepresentationContext defines the context to which the IfcRepresentation of a product is related. 
    ///   NOTE  The definition of this class relates to the STEP entity representation_context. Please refer to ISO/IS 10303-43:1994 for the final definition of the formal standard.
    ///   HISTORY  New entity in IFC Release 1.5. 
    ///   IFC2x Edition 3 NOTE  Users should not instantiate the entity IfcRepresentationContext from IFC2x Edition 2 onwards. It will be changed into an ABSTRACT supertype in future releases of IFC.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcRepresentationContext : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        private IfcLabel? _contextIdentifier;
        private IfcLabel? _contextType;

        #endregion

        #region Events

        [field: NonSerialized] //don't serialize events
            public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The optional identifier of the representation context as used within a project.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcLabel? ContextIdentifier
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _contextIdentifier;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _contextIdentifier, value, v => ContextIdentifier = v,
                                           "ContextIdentifier");
            }
        }

        /// <summary>
        ///   The description of the type of a representation context. The supported values for context type are to be specified by implementers agreements.
        /// </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcLabel? ContextType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _contextType;
            }
            set { ModelManager.SetModelValue(this, ref _contextType, value, v => ContextType = v, "ContextType"); }
        }

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _contextIdentifier = value.StringVal;
                    break;
                case 1:
                    _contextType = value.StringVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        #region Inverse functions

        /// <summary>
        ///   Inverse. All shape representations that are defined in the same representation context.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcRepresentation> RepresentationsInContext
        {
            get { return ModelManager.ModelOf(this).InstancesWhere<IfcRepresentation>(r => r.ContextOfItems == this); }
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

        #region INotifyPropertyChanged Members

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

        #region ISupportIfcParser Members

        #endregion

        #region ISupportIfcParser Members

        public virtual string WhereRule()
        {
            return "";
        }

        #endregion
    }
}