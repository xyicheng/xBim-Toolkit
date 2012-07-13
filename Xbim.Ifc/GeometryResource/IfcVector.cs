#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcVector.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    [IfcPersistedEntity, Serializable]
    [TypeConverter(typeof (VectorConverter))]
    public class IfcVector : IfcGeometricRepresentationItem
    {
        #region Fields

        private IfcLengthMeasure _magnitude;
        private IfcDirection _orientation;

        #endregion

        #region Constructors

        public IfcVector()
        {
        }


        public IfcVector(IfcDirection dir, IfcLengthMeasure magnitude)
        {
            _orientation = dir;
            _magnitude = magnitude;
        }

        public IfcVector(double x, double y, double z, IfcLengthMeasure magnitude)
        {
            _orientation = new IfcDirection(x, y, z);
            _magnitude = magnitude;
        }

        public IfcVector(double x, double y, IfcLengthMeasure magnitude)
        {
            _orientation = new IfcDirection(x, y);
            _magnitude = magnitude;
        }

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The direction of the vector.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcDirection Orientation
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _orientation;
            }
            set { ModelHelper.SetModelValue(this, ref _orientation, value, v => Orientation = v, "Orientation"); }
        }

        /// <summary>
        ///   The magnitude of the vector. All vectors of Magnitude 0.0 are regarded as equal in value regardless of the orientation attribute.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure Magnitude
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _magnitude;
            }
            set { ModelHelper.SetModelValue(this, ref _magnitude, value, v => Magnitude = v, "Magnitude"); }
        }

        /// <summary>
        ///   Derived. The space dimensionality of this class, it is derived from Orientation
        /// </summary>
        public IfcDimensionCount Dim
        {
            get { return Orientation.Dim; }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _orientation = (IfcDirection) value.EntityVal;
                    break;
                case 1:
                    _magnitude = value.RealVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        /// <summary>
        ///   Converts a 2D Vector to a Windows Vector
        /// </summary>
        /// <returns></returns>
        public Vector WVector()
        {
            Vector vec = new Vector(Orientation.X, Orientation.Y);
            vec.Normalize(); //orientation is not normalized
            vec *= Magnitude;
            return vec;
        }

        /// <summary>
        ///   Converts a 3D vector to Windows Vector3D
        /// </summary>
        /// <returns></returns>
        public Vector3D WVector3D()
        {
            Vector3D vec = new Vector3D(Orientation.X, Orientation.Y, Orientation.Z);
            vec.Normalize(); //orientation is not normalized
            vec *= Magnitude;
            return vec;
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", Magnitude, Orientation);
        }

        public override string WhereRule()
        {
            if (Magnitude < 0)
                return "WR1 Vector : The magnitude shall be positive or zero.\n";
            else
                return "";
        }


        public static IfcVector CrossProduct(IfcVector v1, IfcVector v2)
        {
            if (v1.Dim == 3 && v2.Dim == 3)
            {
                Vector3D v3D = Vector3D.CrossProduct(v1.WVector3D(), v2.WVector3D());
                return new IfcVector(v3D.X, v3D.Y, v3D.Z, v3D.Length);
            }
            else
            {
                throw new ArgumentException("CrossProduct: Both Vectors must have the same dimensionality");
            }
        }
    }

    #region Converter

    public class VectorConverter : TypeConverter
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
            IfcVector vec = value as IfcVector;
            if (vec != null && destinationType == typeof (string))
            {
                return vec.ToString();
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
                if (dbls.Count == 3) //2 Dimensional vector
                {
                    return new IfcVector(dbls[1], dbls[2], dbls[0]);
                }
                else if (dbls.Count == 4) //3 Dimensional vector
                    return new IfcVector(dbls[1], dbls[2], dbls[3], dbls[0]);
                else
                    return new IfcDirection();
            }
            else
                return base.ConvertFrom(context, culture, value);
        }
    }

    #endregion
}