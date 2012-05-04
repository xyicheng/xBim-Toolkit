﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMonetaryMeasure.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.MeasureResource
{
    [Serializable]
    public struct IfcMonetaryMeasure : IPersistIfc, IfcDerivedMeasureValue, IfcAppliedValueSelect
    {
        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
                _theValue = value.RealVal;
            else
                throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                  this.GetType().Name.ToUpper()));
        }

        #endregion

        #region ExpressType Members

        public string ToPart21
        {
            get { return IfcReal.AsPart21(_theValue); }
        }

        #endregion

        private double _theValue;

        Type ExpressType.UnderlyingSystemType
        {
            get { return _theValue.GetType(); }
        }

        public object Value
        {
            get { return _theValue; }
        }

        public override string ToString()
        {
            return IfcReal.AsPart21(_theValue);
            //string str = _theValue.ToString();
            //if (str.IndexOfAny(new[] {'.', 'E', 'e'}) == -1) str += ".";
            //return str;
        }

        public IfcMonetaryMeasure(double val)
        {
            _theValue = val;
        }


        public IfcMonetaryMeasure(string val)
        {
            _theValue = Convert.ToDouble(val);
        }

        public static implicit operator IfcMonetaryMeasure(double? value)
        {
            if (value.HasValue)
                return new IfcMonetaryMeasure((double) value);
            else
                return new IfcMonetaryMeasure();
        }

        public static implicit operator IfcMonetaryMeasure(double value)
        {
            return new IfcMonetaryMeasure(value);
        }

        public static implicit operator double(IfcMonetaryMeasure obj)
        {
            return (obj._theValue);
        }

        public static explicit operator double(IfcMonetaryMeasure? obj)
        {
            if (obj.HasValue)
                return ((IfcMonetaryMeasure) obj)._theValue;
            else
                return 0.0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;
            return ((IfcMonetaryMeasure) obj)._theValue == _theValue;
        }

        public static bool operator ==(IfcMonetaryMeasure obj1, IfcMonetaryMeasure obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(IfcMonetaryMeasure obj1, IfcMonetaryMeasure obj2)
        {
            return !Equals(obj1, obj2);
        }

        public override int GetHashCode()
        {
            return _theValue.GetHashCode();
        }

        public static explicit operator StepP21Token(IfcMonetaryMeasure? value)
        {
            if (value.HasValue)
                return new StepP21Token(((IfcMonetaryMeasure) value).ToString());
            else
                return new StepP21Token("$");
        }

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}