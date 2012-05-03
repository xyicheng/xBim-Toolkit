#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCurveStyleFont.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Text;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcCurveStyleFont : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                     IfcCurveStyleFontSelect, IfcCurveFontOrScaledCurveFontSelect,
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

        public IfcCurveStyleFont()
        {
            _patternList = new XbimList<IfcCurveStyleFontPattern>(this);
        }

        #region Fields

        private IfcLabel? _name;
        private XbimList<IfcCurveStyleFontPattern> _patternList;

        #endregion

        #region Part 21 Step file Parse routines

        [IfcAttribute(1, IfcAttributeState.Optional)]
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


        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.List, 1)]
        public XbimList<IfcCurveStyleFontPattern> PatternList
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _patternList;
            }
            set { ModelManager.SetModelValue(this, ref _patternList, value, v => PatternList = v, "PatternList"); }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _name = value.StringVal;
                    break;
                case 1:
                    _patternList.Add((IfcCurveStyleFontPattern) value.EntityVal);
                    break;

                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            foreach (IfcCurveStyleFontPattern item in PatternList)
            {
                if (str.Length > 0)
                    str.Append(",");
                str.AppendFormat("{0},{1}", item.VisibleSegmentLength, item.InvisibleSegmentLength);
            }
            return str.ToString();
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

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}