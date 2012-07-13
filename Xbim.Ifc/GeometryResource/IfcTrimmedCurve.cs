#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTrimmedCurve.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Linq;
using System.Text;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using WVector = System.Windows.Vector;
using WPoint = System.Windows.Point;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    [IfcPersistedEntity, Serializable]
    public class TrimmingSelectList : XbimListSet<IfcTrimmingSelect>
    {
        internal TrimmingSelectList(IPersistIfcEntity owner)
            : base(owner, 2)
        {
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            foreach (IfcTrimmingSelect item in this)
            {
                if (str.Length > 0)
                    str.Append(",");
                IfcCartesianPoint pt = item as IfcCartesianPoint;
                if (pt != null)
                    str.AppendFormat("C{0}", pt);
                else
                    str.AppendFormat("P{0}", item);
            }
            return str.ToString();
        }
    }

    //public class TrimmingSelectListConverter : TypeConverter
    //{
    //    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
    //    {
    //        if (destinationType == typeof(string))
    //            return true;
    //        else
    //            return base.CanConvertTo(context, destinationType);
    //    }

    //    public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
    //    {
    //        TrimmingSelectList lst = value as TrimmingSelectList;
    //        if (lst != null && destinationType == typeof(string))
    //            return lst.ToString();
    //        else
    //            return base.ConvertTo(context, culture, value, destinationType);
    //    }

    //    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
    //    {
    //        if (sourceType == typeof(string))
    //            return true;
    //        else
    //            return base.CanConvertFrom(context, sourceType);
    //    }

    //    public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
    //    {
    //        string str = value as string;
    //        if (str != null )
    //        {
    //            TrimmingSelectList lst = new TrimmingSelectList();
    //            string[] tokens = str.Split(new char[] { 'P', 'p', 'c', 'C' }, StringSplitOptions.RemoveEmptyEntries);
    //            foreach (string token in tokens)
    //            {
    //                DoubleCollection dblcoll = DoubleCollection.Parse(token);
    //                if (dblcoll.Count == 1) lst.Add_Reversible((IfcParameterValue) dblcoll[0]);
    //                else if (dblcoll.Count == 2) lst.Add_Reversible(new IfcCartesianPoint(dblcoll[0], dblcoll[1]));
    //                else if (dblcoll.Count == 3) lst.Add_Reversible(new IfcCartesianPoint(dblcoll[0], dblcoll[1], dblcoll[2]));
    //                else throw new ArgumentOutOfRangeException("Invalid TrimSelect coordinate");
    //            }
    //            return lst;
    //        }
    //        else
    //            return base.ConvertFrom(context, culture, value);
    //    }
    //}

    [IfcPersistedEntity, Serializable]
    public class IfcTrimmedCurve : IfcBoundedCurve
    {
        public IfcTrimmedCurve()
        {
            _trim1 = new TrimmingSelectList(this);
            _trim2 = new TrimmingSelectList(this);
        }

        #region Fields

        private IfcCurve _basisCurve;
        private TrimmingSelectList _trim1;
        private TrimmingSelectList _trim2;
        private IfcBoolean _senseAgreement = true;
        private IfcTrimmingPreference _masterRepresentation;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The curve to be trimmed. For curves with multiple representations any parameter values given as Trim1 or Trim2 refer to the master representation of the BasisCurve only.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcCurve BasisCurve
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _basisCurve;
            }
            set { this.SetModelValue(this, ref _basisCurve, value, v => BasisCurve = v, "BasisCurve"); }
        }


        /// <summary>
        ///   The first trimming point which may be specified as a Cartesian point, as a real parameter or both. Only really used for Ifc compatibility
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1, 2)]
        public TrimmingSelectList Trim1
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _trim1;
            }
            set { this.SetModelValue(this, ref _trim1, value, v => Trim1 = v, "Trim1"); }
        }


        /// <summary>
        ///   The second trimming point which may be specified as a Cartesian point, as a real parameter or both. Only really used for Ifc compatibility
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1, 2)]
        public TrimmingSelectList Trim2
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _trim2;
            }
            set { this.SetModelValue(this, ref _trim2, value, v => Trim2 = v, "Trim2"); }
        }


        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcBoolean SenseAgreement
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _senseAgreement;
            }
            set { this.SetModelValue(this, ref _senseAgreement, value, v => SenseAgreement = v, "SenseAgreement"); }
        }


        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcTrimmingPreference MasterRepresentation
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _masterRepresentation;
            }
            set
            {
                this.SetModelValue(this, ref _masterRepresentation, value, v => MasterRepresentation = v,
                                           "MasterRepresentation");
            }
        }

        public override IfcDimensionCount Dim
        {
            get { return BasisCurve.Dim; }
        }


        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _basisCurve = (IfcCurve) value.EntityVal;
                    break;
                case 1:
                    _trim1.Add((IfcTrimmingSelect)value.EntityVal);
                    break;
                case 2:
                    _trim2.Add((IfcTrimmingSelect)value.EntityVal);
                    break;
                case 3:
                    _senseAgreement = value.BooleanVal;
                    break;
                case 4:
                    _masterRepresentation =
                        (IfcTrimmingPreference) Enum.Parse(typeof (IfcTrimmingPreference), value.EnumVal, true);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region Access functions

        /// <summary>
        ///   Returns a Cartesian point for Trim1, if generate is true and a trim parameter exists for point 1 this is converted to a CartesianPoint
        /// </summary>
        /// <param name = "generate">True to generate a point from a parameter</param>
        /// <returns></returns>
        public IfcCartesianPoint Trim1Point(bool generate)
        {
            IfcParameterValue? trimParam1 = Trim1.OfType<IfcParameterValue>().FirstOrDefault();
            IfcCartesianPoint trimPoint1 = Trim1.OfType<IfcCartesianPoint>().FirstOrDefault();

            if (generate && trimParam1.HasValue &&
                (MasterRepresentation == IfcTrimmingPreference.PARAMETER || trimPoint1 == null))
                //override any cartesian value and calculate from Master preference
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) return ConvertCircleParamToPoint(circ, trimParam1.Value);
                else if (ellipse != null) return ConvertEllipseParamToPoint(ellipse, trimParam1.Value);
                else if (line != null) return ConvertLineParamToPoint(line, trimParam1.Value);
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
            else
                return trimPoint1;
        }


        /// <summary>
        ///   Returns a Cartesian point for Trim2, if generate is true and a trim parameter exists for point 2 this is converted to a CartesianPoint
        /// </summary>
        /// <param name = "generate">True to generate a point from a parameter</param>
        /// <returns></returns>
        public IfcCartesianPoint Trim2Point(bool generate)
        {
            IfcParameterValue? trimParam2 = Trim2.OfType<IfcParameterValue>().FirstOrDefault();
            IfcCartesianPoint trimPoint2 = Trim2.OfType<IfcCartesianPoint>().FirstOrDefault();
            if (generate && trimParam2.HasValue &&
                (MasterRepresentation == IfcTrimmingPreference.PARAMETER || trimPoint2 == null))
                //override any cartesian value and calculate from Master preference
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) return ConvertCircleParamToPoint(circ, trimParam2.Value);
                else if (ellipse != null) return ConvertEllipseParamToPoint(ellipse, trimParam2.Value);
                else if (line != null) return ConvertLineParamToPoint(line, trimParam2.Value);
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
            else
                return trimPoint2;
        }

        /// <summary>
        ///   Returns a Parameter for Trim1, if generate is true and a caresianpoint exists for trim 1 this is converted to a parameter
        /// </summary>
        /// <returns></returns>
        public IfcParameterValue? Trim1Param(bool generate)
        {
            IfcParameterValue? trimParam1 = Trim1.OfType<IfcParameterValue>().FirstOrDefault();
            IfcCartesianPoint trimPoint1 = Trim1.OfType<IfcCartesianPoint>().FirstOrDefault();
            if (generate && trimPoint1 != null &&
                (MasterRepresentation == IfcTrimmingPreference.CARTESIAN || !trimParam1.HasValue))
                //override any parameter value and calculate from Master preference
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) return ConvertCirclePointToParam(circ, trimPoint1);
                else if (ellipse != null) return ConvertEllipsePointToParam(ellipse, trimPoint1);
                else if (line != null) return ConvertLinePointToParam(line, trimPoint1);
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
            return trimParam1;
        }

        /// <summary>
        ///   Returns a Parameter for Trim2, if generate is true and a cartesianpoint exists for trim 2 this is converted to a parameter
        /// </summary>
        /// <returns></returns>
        public double? Trim2Param(bool generate)
        {
            IfcParameterValue? trimParam2 = Trim2.OfType<IfcParameterValue>().FirstOrDefault();
            IfcCartesianPoint trimPoint2 = Trim2.OfType<IfcCartesianPoint>().FirstOrDefault();
            if (generate && trimPoint2 != null &&
                (MasterRepresentation == IfcTrimmingPreference.CARTESIAN || !trimParam2.HasValue))
                //override any parameter value and calculate from Master preference
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) return ConvertCirclePointToParam(circ, trimPoint2);
                else if (ellipse != null) return ConvertEllipsePointToParam(ellipse, trimPoint2);
                else if (line != null) return ConvertLinePointToParam(line, trimPoint2);
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
            return trimParam2;
        }

        /// <summary>
        ///   Sets the value of Trim one to the specified parameter value, for a circle or ellipse this is the degrees rotation, for a line it is the distance from the start, if the setPoint value is true then the Cartesian Point value is set and stored, otherwise it is cleared
        /// </summary>
        /// <param name = "paramValue"></param>
        public void SetTrim1(IfcParameterValue paramValue, bool setPoint)
        {

            Trim1.Clear_Reversible();
            Trim1.Add_Reversible(paramValue);
            if (setPoint)
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) Trim1.Add_Reversible(ConvertCircleParamToPoint(circ, paramValue));
                else if (ellipse != null) Trim1.Add_Reversible(ConvertEllipseParamToPoint(ellipse, paramValue));
                else if (line != null) Trim1.Add_Reversible(ConvertLineParamToPoint(line, paramValue));
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
        }


        /// <summary>
        ///   Sets the value of Trim 1 to the Cartesian Point, if  the setParam value is true then the parameter value for Trim 1 is calculated and set, otherwise it is cleared
        /// </summary>
        /// <param name = "pointValue"></param>
        public void SetTrim1(IfcCartesianPoint pointValue, bool setParam)
        {

            Trim1.Clear_Reversible();
            Trim1.Add_Reversible(pointValue);
            if (setParam)
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) Trim1.Add_Reversible(ConvertCirclePointToParam(circ, pointValue));
                else if (ellipse != null) Trim1.Add_Reversible(ConvertEllipsePointToParam(ellipse, pointValue));
                else if (line != null) Trim1.Add_Reversible(ConvertLinePointToParam(line, pointValue));
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
        }


        /// <summary>
        ///   Sets the value of Trim 1 to the Windows Point, if  the setParam value is true then the parameter value for Trim 1 is calculated and set, otherwise it is cleared
        /// </summary>
        /// <param name = "pointValue"></param>
        public void SetTrim1(WPoint pointValue, bool setParam)
        {
            SetTrim1(new IfcCartesianPoint(pointValue), setParam);
        }


        /// <summary>
        ///   Sets the value of Trim 2 to the specified parameter value, for a circle or ellipse this is the degrees rotation, for a line it is the distance from the start, if the setPoint value is true then the Cartesian Point value is set and stored, otherwise it is cleared
        /// </summary>
        /// <param name = "paramValue"></param>
        public void SetTrim2(IfcParameterValue paramValue, bool setPoint)
        {

            Trim2.Clear_Reversible();
            Trim2.Add_Reversible(paramValue);
            if (setPoint)
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) Trim2.Add_Reversible(ConvertCircleParamToPoint(circ, paramValue));
                else if (ellipse != null) Trim2.Add_Reversible(ConvertEllipseParamToPoint(ellipse, paramValue));
                else if (line != null) Trim2.Add_Reversible(ConvertLineParamToPoint(line, paramValue));
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
        }


        /// <summary>
        ///   Sets the value of Trim 2 to the Cartesian Point, if  the setParam value is true then the parameter value for Trim 2 is calculated and set, otherwise it is cleared
        /// </summary>
        /// <param name = "pointValue"></param>
        public void SetTrim2(IfcCartesianPoint pointValue, bool setParam)
        {

            Trim2.Clear_Reversible();
            Trim2.Add_Reversible(pointValue);
            if (setParam)
            {
                IfcCircle circ = BasisCurve as IfcCircle;
                IfcEllipse ellipse = BasisCurve as IfcEllipse;
                IfcLine line = BasisCurve as IfcLine;
                if (circ != null) Trim2.Add_Reversible(ConvertCirclePointToParam(circ, pointValue));
                else if (ellipse != null) Trim2.Add_Reversible(ConvertEllipsePointToParam(ellipse, pointValue));
                else if (line != null) Trim2.Add_Reversible(ConvertLinePointToParam(line, pointValue));
                else
                    throw new Exception(
                        "The BasisCurve of the TrimmedCurve must be of type Conic or Line to comply with Ifc Schema");
            }
        }


        /// <summary>
        ///   Sets the value of Trim 2 to the Windows Point, if  the setParam value is true then the parameter value for Trim 2 is calculated and set, otherwise it is cleared
        /// </summary>
        /// <param name = "pointValue"></param>
        public void SetTrim2(WPoint pointValue, bool setParam)
        {
            SetTrim2(new IfcCartesianPoint(pointValue), setParam);
        }


        private IfcParameterValue ConvertCirclePointToParam(IfcCircle circ, IfcCartesianPoint _trimPoint1)
        {
            throw new NotImplementedException();
        }

        private IfcParameterValue ConvertLinePointToParam(IfcLine line, IfcCartesianPoint pointValue)
        {
            throw new NotImplementedException();
        }

        private IfcParameterValue ConvertEllipsePointToParam(IfcEllipse ellipse, IfcCartesianPoint pointValue)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Given a parameter suitable for a circle returns the Cartesian point on the circle that matches the param
        /// </summary>
        private IfcCartesianPoint ConvertCircleParamToPoint(IfcCircle circ, double param)
        {
            double angle = Math.IEEERemainder(param, 360.0);
            if (angle < 0) angle += 360; //
            double radians = angle*Math.PI/180;
            double x = circ.Radius*Math.Cos(radians);
            double y = circ.Radius*Math.Sin(radians);
            IfcAxis2Placement2D ax2 = circ.Position as IfcAxis2Placement2D;
            if (ax2 != null) //it's a 2 D circle
            {
                if (ax2.RefDirection != null)
                {
                    WVector xv = ax2.RefDirection.WVector(); //direction of X axis
                    xv.Normalize(); //ifc directions are not normalized
                    WVector yv = new WVector(-xv.Y, xv.X); //direction of Y axis                               
                    xv *= x; //give it magnitude
                    yv *= y;
                    WPoint o = ax2.Location.WPoint2D();
                    o += xv;
                    o += yv;
                    return new IfcCartesianPoint(o.X, o.Y);
                }
                else
                {
                    return new IfcCartesianPoint(ax2.Location.X + x, (ax2.Location.Y + y));
                }
            }
            else
            {
                IfcAxis2Placement3D ax3 = circ.Position as IfcAxis2Placement3D;
                throw new NotImplementedException("3D not yet implemented");
            }
        }

        private IfcCartesianPoint ConvertEllipseParamToPoint(IfcEllipse ellipse, double paramValue)
        {
            double angle = Math.IEEERemainder(paramValue, 360.0);
            if (angle < 0) angle += 360; //
            double radians = angle*Math.PI/180;
            double x = ellipse.SemiAxis1*Math.Cos(radians);
            double y = ellipse.SemiAxis2*Math.Sin(radians);
            IfcAxis2Placement2D ax2 = ellipse.Position as IfcAxis2Placement2D;
            if (ax2 != null) //it's a 2 D circle
            {
                if (ax2.RefDirection != null)
                {
                    WVector xv = ax2.RefDirection.WVector(); //direction of X axis
                    xv.Normalize(); //ifc directions are not normalized
                    WVector yv = new WVector(-xv.Y, xv.X); //direction of Y axis                               
                    xv *= x; //give it magnitude
                    yv *= y;
                    WPoint o = ax2.Location.WPoint2D();
                    o += xv;
                    o += yv;
                    return new IfcCartesianPoint(o.X, o.Y);
                }
                else
                {
                    return new IfcCartesianPoint(ax2.Location.X + x, (ax2.Location.Y + y));
                }
            }
            else
            {
                IfcAxis2Placement3D ax3 = ellipse.Position as IfcAxis2Placement3D;
                throw new NotImplementedException("3D not yet implemented");
            }
        }

        private IfcCartesianPoint ConvertLineParamToPoint(IfcLine line, double paramValue)
        {
            if (line.Dim == 2)
            {
                WPoint pt = line.Pnt.WPoint2D();
                WVector vec = line.Dir.WVector();
                vec *= paramValue;
                pt += vec;
                return new IfcCartesianPoint(pt);
            }
            else
                throw new NotImplementedException("3D Trimmed Line not yet implemented");
        }

        /// <summary>
        ///   Returns true if succeeds, if the Arc is over 180 deg, IsLargeArc = True, if the trim points are reversed IsReversed = True
        /// </summary>
        /// <returns></returns>
        public bool ArcStatus(out bool isLargeArc, out bool isReversed, out double rotate)
        {

            isLargeArc = false;
            isReversed = false;
            rotate = 0;
            IfcConic conic = BasisCurve as IfcConic;
            if (conic != null)
            {
                double? angle1 = Trim1Param(true);
                double? angle2 = Trim2Param(true);
                if (angle1.HasValue && angle2.HasValue)
                {
                    angle1 = Math.IEEERemainder(angle1.Value, 360.0);
                    angle2 = Math.IEEERemainder(angle2.Value, 360.0);
                    if (angle1.Value < 0) angle1 += 360; //sometimes this method returns a negative result
                    if (angle2.Value < 0) angle2 += 360;
                    isReversed = (angle1.Value > angle2.Value);
                    if (isReversed) //the trim points are reversed
                    {
                        if (SenseAgreement)
                            isLargeArc = (360 - Math.Abs(angle1.Value - angle2.Value)) > 180;
                        else
                            isLargeArc = Math.Abs(angle1.Value - angle2.Value) > 180;
                    }
                    else
                    {
                        if (SenseAgreement)
                            isLargeArc = Math.Abs(angle2.Value - angle1.Value) > 180;
                        else
                            isLargeArc = (360 - Math.Abs(angle2.Value - angle1.Value)) > 180;
                    }
                    IfcAxis2Placement2D ax2 = conic.Position as IfcAxis2Placement2D;
                    if (ax2 != null) //it's a 2 D circle
                    {
                        if (ax2.RefDirection != null)
                        {
                            WVector xv = ax2.RefDirection.WVector(); //direction of X axis
                            xv.Normalize(); //ifc directions are not normalized
                            WVector xAxis = new WVector(1, 0);
                            rotate = WVector.AngleBetween(xAxis, xv);
                        }
                    }
                    return true;
                }
                else
                    return false;
            }
            else //lines cannot be arcs
                return false;
        }

        #endregion

        #region Ifc Schema Validation Methods

        public override string WhereRule()
        {
            string err = "";

            if (Trim1.Count > 1 && Trim1[0].GetType() == Trim1[1].GetType())
                err +=
                    "WR41: TrimmedCurve: Either a single value is specified for Trim1, or the two trimming values are of different type (point and parameter).";
            if (Trim2.Count > 1 && Trim2[0].GetType() == Trim2[1].GetType())
                err +=
                    "WR42: TrimmedCurve: Either a single value is specified for Trim2, or the two trimming values are of different type (point and parameter).";
            if (!(BasisCurve is IfcLine || BasisCurve is IfcConic))
                err +=
                    "WR43: TrimmedCurve:   Only line and conic curves should be trimmed, not other bounded curves. NOTE: This is an additional constraint of IFC.";
            return err;
        }

        #endregion
    }
}