#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcResourceApprovalRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ApprovalResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcResourceApprovalRelationship : IPersistIfcEntity, ISupportChangeNotification, INotifyPropertyChanged,
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

        public IfcResourceApprovalRelationship()
        {
            _approvedResources = new XbimSet<IfcResource>(this);
        }

        #region Fields

        private XbimSet<IfcResource> _approvedResources;
        private IfcApproval _approval;

        #endregion

        #region Properties

        /// <summary>
        ///   Resources approved by the approval.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, 1)]
        public XbimSet<IfcResource> ApprovedResources
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _approvedResources;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _approvedResources, value, v => ApprovedResources = v,
                                           "ApprovedResources");
            }
        }

        /// <summary>
        ///   The approval for the resources selected.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcApproval Approval
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _approval;
            }
            set { ModelManager.SetModelValue(this, ref _approval, value, v => Approval = v, "Approval"); }
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

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _approvedResources.Add((IfcResource) value.EntityVal);
                    break;
                case 1:
                    _approval = (IfcApproval) value.EntityVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("propIndex",
                                                          string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}