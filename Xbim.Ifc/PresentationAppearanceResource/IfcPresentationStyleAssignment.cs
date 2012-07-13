#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcPresentationStyleAssignment.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    /// <summary>
    ///   The presentation style assignment is a set of styles which are assigned to styled items for the purpose of presenting these styled items.
    /// </summary>
    /// <remarks>
    ///   Definition from ISO/CD 10303-46:1992: The presentation style assignment is a set of styles which are assigned to styled items for the purpose of presenting these styled items.
    ///   NOTE Corresponding STEP name: presentation_style_assignment. Please refer to ISO/IS 10303-46:1994 for the final definition of the formal standard. 
    ///   HISTORY New entity in Release IFC2x 2nd Edition.
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcPresentationStyleAssignment : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                                  INotifyPropertyChanging
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

        public IfcPresentationStyleAssignment()
        {
            _styles = new XbimSet<IfcPresentationStyleSelect>(this);
        }

        #region Fields

        private XbimSet<IfcPresentationStyleSelect> _styles;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   A set of presentation styles that are assigned to styled items.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1)]
        public XbimSet<IfcPresentationStyleSelect> Styles
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _styles;
            }
            set { this.SetModelValue(this, ref _styles, value, v => Styles = v, "Styles"); }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _styles.Add((IfcPresentationStyleSelect) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
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