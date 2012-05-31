#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcReferencesValueDocument.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.CostResource
{
    /// <summary>
    ///   An IfcReferencesValueDocument is a means of referencing many instances of IfcAppliedValue to a single document where the document is a price list, quotation, list of environmental impact values or other source of information.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcReferencesValueDocument is a means of referencing many instances of IfcAppliedValue to a single document where the document is a price list, quotation, list of environmental impact values or other source of information. 
    ///   HISTORY: New class in IFC Release 2x. Name changed from IfcReferencesCostDocument in IFC 2x2
    ///   Use Definitions
    ///   The purpose of this class is to be able to identify a reference source from which applied values are obtained. Since many objects may be obtain such values from the same referenced document, use of a relationship class allows the document to be identified once only when information is exchanged or shared rather than many times.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcReferencesValueDocument : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        public IfcReferencesValueDocument()
        {
            _referencingValues = new XbimSet<IfcAppliedValue>(this);
        }

        #region Fields

        private IfcDocumentSelect _referencedDocument;
        private XbimSet<IfcAppliedValue> _referencingValues;
        private IfcLabel? _name;
        private IfcText? _description;

        #endregion

        /// <summary>
        ///   A document such as a price list or quotation from which costs are obtained.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcDocumentSelect ReferencedDocument
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _referencedDocument;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _referencedDocument, value, v => ReferencedDocument = v,
                                           "ReferencedDocument");
            }
        }

        /// <summary>
        ///   Costs obtained from a single document such as a price list or quotation.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public XbimSet<IfcAppliedValue> ReferencingValues
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _referencingValues;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _referencingValues, value, v => ReferencingValues = v,
                                           "ReferencingValues");
            }
        }

        /// <summary>
        ///   Optional. A name used to identify or qualify the relationship to the document from which values may be referenced..
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
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
        ///   Optional. A description of the relationship to the document from which values may be referenced.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
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

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        #endregion

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
                    _referencedDocument = (IfcDocumentSelect) value.EntityVal;
                    break;
                case 1:
                    _referencingValues.Add_Reversible((IfcAppliedValue) value.EntityVal);
                    break;
                case 2:
                    _name = value.StringVal;
                    break;
                case 3:
                    _description = value.StringVal;
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