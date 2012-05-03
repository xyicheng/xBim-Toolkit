#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDocumentInformation.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ExternalReferenceResource
{
    /// <summary>
    ///   An IfcDocumentInformation captures "metadata" of an external document.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An IfcDocumentInformation captures "metadata" of an external document. The actual content of the document is 
    ///   not defined in IFC ; instead, it can be found following the reference given to IfcDocumentReference. 
    ///   HISTORY: New entity in IFC 2x.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcDocumentInformation : IfcDocumentSelect, INotifyPropertyChanged, ISupportChangeNotification,
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

        #region Fields

        private IfcIdentifier _documentId;
        private IfcLabel _name;
        private IfcText? _description;
        private XbimSet<IfcDocumentReference> _documentReferences;
        private IfcText? _purpose;
        private IfcText? _intendedUse;
        private IfcText? _scope;
        private IfcLabel? _revision;
        private IfcActorSelect _documentOwner;
        private XbimSet<IfcActorSelect> _editors;
        private IfcDateAndTime _creationTime;
        private IfcDateAndTime _lastRevisionTime;
        private IfcDocumentElectronicFormat _electronicFormat;
        private IfcCalendarDate _validFrom;
        private IfcCalendarDate _validUntil;
        private IfcDocumentConfidentialityEnum? _confidentiality;
        private IfcDocumentStatusEnum? _status;

        #endregion

        /// <summary>
        ///   Identifier that uniquely identifies a document.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcIdentifier DocumentId
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _documentId;
            }
            set { ModelManager.SetModelValue(this, ref _documentId, value, v => DocumentId = v, "DocumentId"); }
        }

        /// <summary>
        ///   File name or document name assigned by owner.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLabel Name
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
        ///   Optional. Description of document and its content.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
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
        ///   Optional. Information on the referenced document.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public XbimSet<IfcDocumentReference> DocumentReferences
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _documentReferences;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _documentReferences, value, v => DocumentReferences = v,
                                           "DocumentReferences");
            }
        }

        /// <summary>
        ///   Optional. Purpose for this document.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcText? Purpose
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _purpose;
            }
            set { ModelManager.SetModelValue(this, ref _purpose, value, v => Purpose = v, "Purpose"); }
        }

        /// <summary>
        ///   Optional. Intended use for this document.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcText? IntendedUse
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _intendedUse;
            }
            set { ModelManager.SetModelValue(this, ref _intendedUse, value, v => IntendedUse = v, "IntendedUse"); }
        }

        /// <summary>
        ///   Optional. Scope for this document.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcText? Scope
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _scope;
            }
            set { ModelManager.SetModelValue(this, ref _scope, value, v => Scope = v, "Scope"); }
        }

        /// <summary>
        ///   Optional. Document revision designation
        /// </summary>
        [IfcAttribute(8, IfcAttributeState.Optional)]
        public IfcLabel? Revision
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _revision;
            }
            set { ModelManager.SetModelValue(this, ref _revision, value, v => Revision = v, "Revision"); }
        }

        /// <summary>
        ///   Optional. Information about the person and/or organization acknowledged as the 'owner' of this document. In some contexts, the document owner determines who has access to or editing right to the document.
        /// </summary>
        [IfcAttribute(9, IfcAttributeState.Optional)]
        public IfcActorSelect DocumentOwner
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _documentOwner;
            }
            set { ModelManager.SetModelValue(this, ref _documentOwner, value, v => DocumentOwner = v, "DocumentOwner"); }
        }

        /// <summary>
        ///   Optional. The persons and/or organizations who have created this document or contributed to it.
        /// </summary>
        [IfcAttribute(10, IfcAttributeState.Optional)]
        public XbimSet<IfcActorSelect> Editors
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _editors;
            }
            set { ModelManager.SetModelValue(this, ref _editors, value, v => Editors = v, "Editors"); }
        }

        /// <summary>
        ///   Optional. Date and time stamp when the document was originally created.
        /// </summary>
        [IfcAttribute(11, IfcAttributeState.Optional)]
        public IfcDateAndTime CreationTime
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _creationTime;
            }
            set { ModelManager.SetModelValue(this, ref _creationTime, value, v => CreationTime = v, "CreationTime"); }
        }

        /// <summary>
        ///   Optional. Date and time stamp when this document version was created.
        /// </summary>
        [IfcAttribute(12, IfcAttributeState.Optional)]
        public IfcDateAndTime LastRevisionTime
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _lastRevisionTime;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _lastRevisionTime, value, v => LastRevisionTime = v,
                                           "LastRevisionTime");
            }
        }

        /// <summary>
        ///   Optional. Describes the electronic format of the document being referenced, providing the file extension and the manner in which the content is provided.
        /// </summary>
        [IfcAttribute(13, IfcAttributeState.Optional)]
        public IfcDocumentElectronicFormat ElectronicFormat
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _electronicFormat;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _electronicFormat, value, v => ElectronicFormat = v,
                                           "ElectronicFormat");
            }
        }

        /// <summary>
        ///   Optional. Date, when the document becomes valid.
        /// </summary>
        [IfcAttribute(14, IfcAttributeState.Optional)]
        public IfcCalendarDate ValidFrom
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _validFrom;
            }
            set { ModelManager.SetModelValue(this, ref _validFrom, value, v => ValidFrom = v, "ValidFrom"); }
        }

        /// <summary>
        ///   Optional. Date until which the document remains valid.
        /// </summary>
        [IfcAttribute(15, IfcAttributeState.Optional)]
        public IfcCalendarDate ValidUntil
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _validUntil;
            }
            set { ModelManager.SetModelValue(this, ref _validUntil, value, v => ValidUntil = v, "ValidUntil"); }
        }

        /// <summary>
        ///   Optional. The level of confidentiality of the document.
        /// </summary>
        [IfcAttribute(16, IfcAttributeState.Optional)]
        public IfcDocumentConfidentialityEnum? Confidentiality
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _confidentiality;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _confidentiality, value, v => Confidentiality = v,
                                           "Confidentiality");
            }
        }

        /// <summary>
        ///   Optional. The current status of the document. Examples of status values that might be used for a document information status include:
        /// </summary>
        [IfcAttribute(17, IfcAttributeState.Optional)]
        public IfcDocumentStatusEnum? Status
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _status;
            }
            set { ModelManager.SetModelValue(this, ref _status, value, v => Status = v, "Status"); }
        }

        /// <summary>
        ///   An inverse relationship from the IfcDocumentInformationRelationship to the related documents.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcDocumentInformationRelationship> IsPointedTo
        {
            get
            {
                return
                    ModelManager.ModelOf(this).InstancesWhere<IfcDocumentInformationRelationship>(
                        di => di.RelatedDocuments.Contains(this));
            }
        }

        /// <summary>
        ///   An inverse relationship from the IfcDocumentInformationRelationship to the relating document.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcDocumentInformationRelationship> IsPointer
        {
            get
            {
                return
                    ModelManager.ModelOf(this).InstancesWhere<IfcDocumentInformationRelationship>(
                        di => di.RelatingDocument == this);
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
                    _documentId = value.StringVal;
                    break;
                case 1:
                    _name = value.StringVal;
                    break;
                case 2:
                    _description = value.StringVal;
                    break;
                case 3:
                    if (_documentReferences == null) _documentReferences = new XbimSet<IfcDocumentReference>(this);
                    _documentReferences.Add_Reversible((IfcDocumentReference) value.EntityVal);
                    break;
                case 4:
                    _purpose = value.StringVal;
                    break;
                case 5:
                    _intendedUse = value.StringVal;
                    break;
                case 6:
                    _scope = value.StringVal;
                    break;
                case 7:
                    _revision = value.StringVal;
                    break;
                case 8:
                    _documentOwner = (IfcActorSelect) value.EntityVal;
                    break;
                case 9:
                    if (_editors == null) _editors = new XbimSet<IfcActorSelect>(this);
                    _editors.Add_Reversible((IfcActorSelect) value.EntityVal);
                    break;
                case 10:
                    _creationTime = (IfcDateAndTime) value.EntityVal;
                    break;
                case 11:
                    _lastRevisionTime = (IfcDateAndTime) value.EntityVal;
                    break;
                case 12:
                    _electronicFormat = (IfcDocumentElectronicFormat) value.EntityVal;
                    break;
                case 13:
                    _validFrom = (IfcCalendarDate) value.EntityVal;
                    break;
                case 14:
                    _validUntil = (IfcCalendarDate) value.EntityVal;
                    break;
                case 15:
                    _confidentiality =
                        (IfcDocumentConfidentialityEnum)
                        Enum.Parse(typeof (IfcDocumentConfidentialityEnum), value.StringVal, true);
                    break;
                case 16:
                    _status = (IfcDocumentStatusEnum) Enum.Parse(typeof (IfcDocumentStatusEnum), value.EnumVal, true);
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
            return "";
        }

        #endregion
    }
}