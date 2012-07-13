#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMaterialList.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Text;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.MaterialResource
{
    /// <summary>
    ///   A list of the different materials that are used in an element.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: A list of the different materials that are used in an element. 
    ///   NOTE: The class IfcMaterialList will normally be used where an element is described at a more abstract level. For example, for an architectural specification writer, the only information that may be needed about a concrete column is that it contains concrete, reinforcing steel and mild steel ligatures. It shall not be used for elements consisting of matsel layers when the different layers can be defined and the class IfcMaterialLayerSet can be used. Also, IfcMaterialList shall not be used for elements consisting of a single identifiable matsel, (e.g. to represent anisotropic matsel).
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcMaterialList : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                   IfcMaterialSelect, IfcObjectReferenceSelect, INotifyPropertyChanging
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

        public IfcMaterialList()
        {
            _materials = new XbimList<IfcMaterial>(this);
        }

        #region Fields

        private XbimList<IfcMaterial> _materials;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Materials used in a composition of substances.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.List, 1)]
        public XbimList<IfcMaterial> Materials
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _materials;
            }
            set { this.SetModelValue(this, ref _materials, value, v => Materials = v, "Materials"); }
        }

        #endregion

        #region ISupportIfcParser Members

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
            {
                _materials.Add((IfcMaterial)value.EntityVal);
            }
            else
                this.HandleUnexpectedAttribute(propIndex, value);
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

        public string Name
        {
            get
            {
                StringBuilder str = new StringBuilder();
                foreach (IfcMaterial mat in Materials)
                {
                    str.AppendFormat("{0} ", mat.Name);
                }
                return str.ToString();
            }
        }

        #endregion
    }
}