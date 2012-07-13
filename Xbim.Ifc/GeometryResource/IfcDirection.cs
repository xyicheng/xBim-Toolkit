#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDirection.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Media;
using System.Xml.Serialization;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using WinVector = System.Windows.Vector;
using WinVector3D = System.Windows.Media.Media3D.Vector3D;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    public interface IDirectionRatioList<T> : IList<double>, ExpressEnumerable
    {
    }


    /// <summary>
    ///   This entity defines a general direction vector in two or three dimensional space.
    /// </summary>
    /// <remarks>
    ///   Definition from ISO/CD 10303-42:1992: This entity defines a general direction vector in two or three dimensional space. The actual magnitudes of the components have no effect upon the direction being defined, only the ratios X:Y:Z or X:Y are significant. 
    ///   NOTE: The components of this entity are not normalized. If a unit vector is required it should be normalized before use. 
    ///   NOTE: Corresponding STEP entity: direction. Please refer to ISO/IS 10303-42:1994, p.26 for the final definition of the formal standard. The derived attribute Dim has been added (see also note at IfcGeometricRepresentationItem). 
    ///   HISTORY: New entity in IFC Release 1.0
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcDirection : IfcGeometricRepresentationItem, IfcOrientationSelect, IDirectionRatioList<double>, IVector3D
    {
        #region Fields

        private double _x;
        private double _y;
        private double _z;

        #endregion

        private class DirectionListEnumerator : IEnumerator<double>
        {
            private int pos = -1;
            private readonly IDirectionRatioList<double> _ratios;
            private double _current = double.NaN;

            public DirectionListEnumerator(IDirectionRatioList<double> ratios)
            {
                _ratios = ratios;
            }

            #region IEnumerator<double> Members

            public double Current
            {
                get { return _ratios[pos]; }
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
            }

            #endregion

            #region IEnumerator Members

            object IEnumerator.Current
            {
                get { return _current; }
            }

            public bool MoveNext()
            {
                pos++;
                if (pos > 2 || _ratios.Count == 0) return false;
                _current = _ratios[pos];
                return !double.IsNaN(_current);
            }

            public void Reset()
            {
                pos = -1;
            }

            #endregion
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            ((IPersistIfcEntity) this).Activate(false);
            return new DirectionListEnumerator(this);
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            ((IPersistIfcEntity) this).Activate(false);
            return new DirectionListEnumerator(this);
        }

        #endregion

        #region Constructors

        /// <summary>
        ///   Constructs a 3D direction
        /// </summary>
        public IfcDirection()
        {
            _x = double.NaN;
            _y = double.NaN;
            _z = double.NaN;
        }

        public IfcDirection(WinVector3D vect3D)
            : this(vect3D.X, vect3D.Y, vect3D.Z)
        {
        }

        public IfcDirection(double x, double y)
        {
            _x = x;
            _y = y;
            _z = double.NaN;
        }

        public IfcDirection(double x, double y, double z)
        {
            _x = x;
            _y = y;
            _z = z;
        }

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The components in the direction of X axis (DirectionRatios[1]), of Y axis (DirectionRatios[2]), and of Z axis (DirectionRatios[3])
        /// </summary>

        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.List, 2, 3)]
        public IDirectionRatioList<double> DirectionRatios
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return this;
            }
            set
            {
                for (int i = 0; i < value.Count; i++)
                {
                    switch (i)
                    {
                        case 0:
                            this.SetModelValue(this, ref _x, value[0], v => _x = v, "X");
                            break;
                        case 1:
                            this.SetModelValue(this, ref _y, value[1], v => _y = v, "Y");
                            break;
                        case 2:
                            this.SetModelValue(this, ref _z, value[2], v => _z = v, "Z");
                            break;
                    }
                }
            }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
            {
                ((IDirectionRatioList<double>) this).Add(value.RealVal);
            }
            else
                this.HandleUnexpectedAttribute(propIndex, value);
        }

        #endregion

        /// <summary>
        ///   Returns a non-normalized Windows Vector
        /// </summary>
        /// <returns></returns>
        public WinVector WVector()
        {
            ((IPersistIfcEntity) this).Activate(false);
            return new WinVector(_x, _y);
        }

        public WinVector3D WVector3D()
        {
            ((IPersistIfcEntity) this).Activate(false);
            return new WinVector3D(_x, _y, _z);
        }

        public double this[int axis]
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                switch (axis)
                {
                    case 0:
                        return _x;
                    case 1:
                        return _y;
                    case 2:
                        return _z;
                    default:
                        throw new Exception("Index out of bounds for CartesianPoint");
                }
            }
            set
            {
                switch (axis)
                {
                    case 0:
                        this.SetModelValue(this, ref _x, value, v => _x = v, "X");
                        break;
                    case 1:
                        if (double.IsNaN(_x)) this.SetModelValue(this, ref _x, 0, v => _x = v, "X");
                        this.SetModelValue(this, ref _y, value, v => _y = v, "Y");
                        break;
                    case 2:
                        if (double.IsNaN(_x)) this.SetModelValue(this, ref _x, 0, v => _x = v, "X");
                        if (double.IsNaN(_y)) this.SetModelValue(this, ref _y, 0, v => _y = v, "Y");
                        this.SetModelValue(this, ref _z, value, v => _z = v, "Z");
                        break;
                    default:
                        throw new Exception("Index out of bounds for CartesianPoint");
                }
            }
        }

        /// <summary>
        ///   Derived. The space dimensionality of this class, defined by the number of real in the list of DirectionRatios.
        /// </summary>
        [XmlIgnore]
        public IfcDimensionCount Dim
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                if (double.IsNaN(_x)) return 0;
                if (double.IsNaN(_y)) return 1;
                if (double.IsNaN(_z)) return 2;
                return 3;
            }
        }

        public double X
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _x;
            }
            set { this.SetModelValue(this, ref _x, value, v => _x = v, "X"); }
        }

        public double Y
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _y;
            }
            set { this.SetModelValue(this, ref _y, value, v => _y = v, "X"); }
        }

        public double Z
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _z;
            }
            set { this.SetModelValue(this, ref _z, value, v => _z = v, "Z"); }
        }

        public void SetXYZ(double x, double y, double z)
        {
                ((IPersistIfcEntity) this).Activate(false);
            this.SetModelValue(this, ref _x, x, v => _x = v, "X");
            this.SetModelValue(this, ref _y, y, v => _y = v, "Y");
            this.SetModelValue(this, ref _z, z, v => _z = v, "Z");
        }

        public bool IsInvalid()
        {
            if (Dim == 3)
                return (_x == 0 && _y == 0 && _z == 0);
            else if (Dim == 2)
                return (_x == 0 && _y == 0);
            else
                return true;
        }

        /// <summary>
        ///   Sets X and Y changes dimension to 2D
        /// </summary>
        /// <param name = "x"></param>
        /// <param name = "y"></param>
        public void SetXY(double x, double y)
        {
            ((IPersistIfcEntity)this).Activate(false);
            this.SetModelValue(this, ref _x, x, v => _x = v, "X");
            this.SetModelValue(this, ref _y, y, v => _y = v, "Y");
            this.SetModelValue(this, ref _z, double.NaN, v => _z = v, "Z");
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", X, Y, Z);
        }

        public IfcDirection Normalise()
        {
            ((IPersistIfcEntity) this).Activate(false);

            if (Dim == 3)
            {
                WinVector3D v3D = new WinVector3D(_x, _y, _z);
                v3D.Normalize();
                return new IfcDirection(v3D.X, v3D.Y, v3D.Z);
            }
            if (Dim == 2)
            {
                WinVector v2D = new WinVector(_x, _y);
                v2D.Normalize();
                return new IfcDirection(v2D.X, v2D.Y);
            }
            else
                throw new ArgumentException("Only 2D and 3D Directions can be normalised");
        }

        public static IfcVector CrossProduct(IfcDirection d1, IfcDirection d2)
        {
            if (d1.Dim == 3 && d2.Dim == 3)
            {
                WinVector3D v3D = WinVector3D.CrossProduct(d1.WVector3D(), d2.WVector3D());
                return new IfcVector(v3D.X, v3D.Y, v3D.Z, v3D.Length);
            }
            else
            {
                throw new ArgumentException("CrossProduct: Both Vectors must have the same dimensionality");
            }
        }


        public override string WhereRule()
        {
            return "";
        }

        #region IList<double> Members

        public int IndexOf(double item)
        {
            ((IPersistIfcEntity) this).Activate(false);
            if (double.IsNaN(item)) throw new Exception("Cannot treat a NAN as a coordinate value");
            if (_x == item) return 0;
            if (_y == item) return 1;
            if (_z == item) return 2;
            return -1;
        }

        public void Insert(int index, double item)
        {
            throw new Exception("Insert operations not supported on Direction Coordinates");
        }

        public void RemoveAt(int index)
        {
            throw new Exception("RemoveAt operations not supported on Direction Coordinates");
        }

        #endregion

        #region ICollection<double> Members

        public void Add(double item)
        {
            if (double.IsNaN(_x)) _x = item;
            else if (double.IsNaN(_y)) _y = item;
            else if (double.IsNaN(_z)) _z = item;
            else throw new Exception("Index out of bounds for Direction in Add");
        }

        public void Clear()
        {
            _x = double.NaN;
            _y = double.NaN;
            _z = double.NaN;
        }

        public bool Contains(double item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                if (double.IsNaN(_x)) return 0;
                if (double.IsNaN(_y)) return 1;
                if (double.IsNaN(_z)) return 2;
                return 3;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(double item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<double> Members

        IEnumerator<double> IEnumerable<double>.GetEnumerator()
        {
            ((IPersistIfcEntity) this).Activate(false);
            return new DirectionListEnumerator(this);
        }

        #endregion

        #region ExpressEnumerable Members

        public string ListType
        {
            get { return "list"; }
        }

        #endregion

        #region ExpressEnumerable Members

        public void Add(object o)
        {
            if (double.IsNaN(_x)) _x = (double) o;
            else if (double.IsNaN(_y)) _y = (double) o;
            else if (double.IsNaN(_z)) _z = (double) o;
            else throw new Exception("Index out of bounds for Direction in Add");
        }

        #endregion

        #region ExpressEnumerable Members

        public IPersistIfcEntity OwningEntity
        {
            get { return this; }
            set
            {
                // throw new NotImplementedException("You can set an owning entity for Direction");
            }
        }

        #endregion
    }

    #region Converter

    public class DirectionConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof (string))
                return true;
            else
                return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value,
                                         Type destinationType)
        {
            IfcDirection dir = value as IfcDirection;
            if (dir != null && destinationType == typeof (string))
            {
                return dir.ToString();
            }
            else
                return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof (string))
                return true;
            else
                return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;
            if (str != null)
            {
                DoubleCollection dbls = DoubleCollection.Parse(str);
                if (dbls.Count == 2)
                    return new IfcDirection(dbls[0], dbls[1]);
                else if (dbls.Count == 3)
                    return new IfcDirection(dbls[0], dbls[1], dbls[2]);
                else
                    return new IfcDirection();
            }
            else
                return base.ConvertFrom(context, culture, value);
        }
    }

    #endregion
}