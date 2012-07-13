using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.ProductExtension;
using System.ComponentModel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Ifc2x3.GeometricConstraintResource
{


    [IfcPersistedEntityAttribute, Serializable]
    public class IfcGridAxis : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        IfcLabel? _axisTag;
        IfcCurve _axisCurve;
        bool _sameSense;
        /// <summary>
        ///   Optional. The tag or name for this grid axis.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcLabel? AxisTag
        {
            get
            {
                ((IPersistIfcEntity)this).Activate(false);
                return _axisTag;
            }
            set
            {
                this.SetModelValue(this, ref _axisTag, value, v => AxisTag = v, "AxisTag");
            }
        }

        /// <summary>
        ///   Underlying curve which provides the geometry for this grid axis.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcCurve AxisCurve
        {
            get
            {
                ((IPersistIfcEntity)this).Activate(false);
                return _axisCurve;
            }
            set
            {
                this.SetModelValue(this, ref _axisCurve, value, v => AxisCurve = v, "AxisCurve");
            }
        }

        /// <summary>
        ///   Defines whether the original sense of curve is used or whether it is reversed in the context of the grid axis.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public bool SameSense
        {
            get
            {
                ((IPersistIfcEntity)this).Activate(false);
                return _sameSense;
            }
            set
            {
                this.SetModelValue(this, ref _sameSense, value, v => SameSense = v, "SameSense");
            }
        }



        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the WAxes of IfcGrid.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcGrid> PartOfW
        {
            get
            {
                return ModelOf.InstancesWhere<IfcGrid>(g => g.WAxes.Contains(this));
            }
        }

        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the VAxes of IfcGrid.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcGrid> PartOfV
        {
            get
            {
                return ModelOf.InstancesWhere<IfcGrid>(g => g.VAxes.Contains(this));
            }
        }

        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the UAxes of IfcGrid.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 0, 1)]
        public IEnumerable<IfcGrid> PartOfU
        {
            get
            {
                return ModelOf.InstancesWhere<IfcGrid>(g => g.UAxes.Contains(this));
            }
        }
        /// <summary>
        ///   The reference to a set of 's, that connect other grid axes to this grid axis.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcVirtualGridIntersection> HasIntersections
        {
            get
            {
                return ModelOf.InstancesWhere<IfcVirtualGridIntersection>(vg => vg.IntersectingAxes.Contains(this));
            }
        }

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _axisTag = value.StringVal;
                    break;
                case 1:
                    _axisCurve = (IfcCurve)value.EntityVal;
                    break;
                case 2:
                    _sameSense = value.BooleanVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }


        public string WhereRule()
        {
            string err = "";
            if (AxisCurve.Dim != 2)
                err += "WR1 IfcGridAxis: The dimensionality of the grid axis must be 2\n";
            bool u = PartOfU.Count() > 0;
            bool v = PartOfV.Count() > 0;
            bool w = PartOfW.Count() > 0;
            if (!(u ^ v ^ w))
                err += "WR2 IfcGridAxis: The IfcGridAxis needs to be used by exactly one of the three attributes of IfcGrid: i.e. it can only refer to a single instance of IfcGrid in one of the three list of axes.\n";
            return err;
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

        [field: NonSerialized] //don't serialize events
        private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion



    }
}
