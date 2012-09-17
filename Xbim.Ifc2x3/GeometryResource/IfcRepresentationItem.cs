#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRepresentationItem.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.GeometryResource
{
    /// <summary>
    ///   A representation item is an element of product data that participates in one or more representations or contributes to the definition of another representation item.
    /// </summary>
    /// <remarks>
    ///   Definition from ISO/CD 10303-43:1992: A representation item is an element of product data that participates in one or more representations or contributes to the definition of another representation item. A representation item contributes to the definition of another representation item when it is referenced by that representation item.
    ///   Definition from IAI  The IfcRepresentationItem is used within (and only within - directly or indirectly through other IfcRepresentationItem's or IfcShapeAspect's) an IfcRepresentation to represent an IfcProductRepresentation. Most commonly these IfcRepresentationItem's are geometric or topological representation items, that can (but not need to) have presentation style infomation assigned. 
    ///   NOTE  Corresponding STEP entity: representation_item. Please refer to ISO/IS 10303-43:1994, for the final definition of the formal standard. The following changes have been made: The attribute 'name' and the WR1 have not been incorporated. 
    ///   HISTORY  New entity in IFC Release 2x.
    ///   IFC2x Edition 3 CHANGE  The inverse attributes StyledByItem and LayerAssignments have been added. Upward compatibility for file based exchange is guaranteed. 
    ///   EXPRESS specification:
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public abstract class IfcRepresentationItem : ISupportChangeNotification, INotifyPropertyChanged, IfcLayeredItem,
                                                  IPersistIfcEntity, INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private int _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public int EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        #region Constructors

        #endregion

        #region Inverse Relationships

        /// <summary>
        ///   Inverse. Assignment of the representation item to a single or multiple layer(s). The LayerAssigments can override a LayerAssigments of the IfcRepresentation it is used within the list of Items.
        /// </summary>
        /// <remarks>
        ///   NOTE  Implementation agreements can restrict the maximum number of layer assignments to 1.
        ///   IFC2x Edition 3 CHANGE  The inverse attribute LayerAssignments has been add
        /// </remarks>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcPresentationLayerAssignment> LayerAssignments
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcPresentationLayerAssignment>(
                        las => las.AssignedItems.Contains(this));
            }
        }

        /// <summary>
        ///   Inverse. Reference to the IfcStyledItem that provides presentation information to the representation, e.g. a curve style, including colour and thickness to a geometric curve.
        /// </summary>
        /// <remarks>
        ///   IFC2x Edition 3 CHANGE  The inverse attribute StyledByItem has been added.
        /// </remarks>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcStyledItem> StyledByItem
        {
            get { return ModelOf.InstancesWhere<IfcStyledItem>(i => i.Item == this); }
        }

        #region ISupportIfcParser Members

        public abstract void IfcParse(int propIndex, IPropertyValue value);

        #endregion

        #endregion

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
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

        void ISupportChangeNotification.NotifyPropertyChanging(string propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        [field: NonSerialized] //don't serialize event
            private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion

        #region ISupportIfcParser Members

        public abstract string WhereRule();

        #endregion
    }
}