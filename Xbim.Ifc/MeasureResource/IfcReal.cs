#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcReal.cs
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
    public struct IfcReal : IPersistIfc, IfcSimpleValue
    {
        private double _theValue;

        Type ExpressType.UnderlyingSystemType
        {
            get { return _theValue.GetType(); }
        }

        public IfcReal(double val)
        {
            _theValue = val;
        }


        public IfcReal(string val)
        {
            _theValue = Convert.ToDouble(val);
        }

        #region ExpressType Members

        public string ToPart21
        {
            get { return AsPart21(_theValue); }
        }

        #endregion

        public object Value
        {
            get { return _theValue; }
        }

        public override string ToString()
        {
            return AsPart21(_theValue);
        }

        public static string AsPart21(double real)
        {
            string str = real.ToString();
            if (!str.Contains("."))
            {
                if (str.Contains("E"))
                    str = str.Replace("E", ".E");
                else
                    str += ".";
            }
            return str;
        }


        public static implicit operator IfcReal(double value)
        {
            return new IfcReal(value);
        }

        public static implicit operator double(IfcReal obj)
        {
            return (obj._theValue);
        }


        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;
            return ((IfcReal) obj)._theValue == _theValue;
        }

        public static bool operator ==(IfcReal obj1, IfcReal obj2)
        {
            return Equals(obj1, obj2);
        }

        public static bool operator !=(IfcReal obj1, IfcReal obj2)
        {
            return !Equals(obj1, obj2);
        }

        public override int GetHashCode()
        {
            return _theValue.GetHashCode();
        }

        public static explicit operator StepP21Token(IfcReal? value)
        {
            if (value.HasValue)
                return new StepP21Token(((IfcReal) value).ToString());
            else
                return new StepP21Token("$");
        }

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

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}