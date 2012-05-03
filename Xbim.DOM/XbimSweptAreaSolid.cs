using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.GeometricModelResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ProfileResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;

namespace Xbim.DOM
{
    public abstract class XbimSweptAreaSolid : IXbimGeometry, Xbim.DOM.IBimSweptSolid
    {
        private IfcSweptAreaSolid _ifcSweptAreaSolid;
        private XbimDocument _document;
        private CompositeCurveSegmentList _profileCurveSegments;
        private bool _initialized;
        private bool _compositCurve;

        public XbimDocument Document { get { return _document; } }

        internal IfcSweptAreaSolid IfcSweptAreaSolid { get { return _ifcSweptAreaSolid; } }
        internal XbimSweptAreaSolid(XbimDocument document)
        {
            _document = document;
            _initialized = false;
        }

        protected void BaseInit<T>() where T : IfcSweptAreaSolid
        {
            if (typeof(T) == typeof(IfcExtrudedAreaSolid)) _ifcSweptAreaSolid = _document.Model.New<IfcExtrudedAreaSolid>();
            if (typeof(T) == typeof(IfcRevolvedAreaSolid)) _ifcSweptAreaSolid = _document.Model.New<IfcRevolvedAreaSolid>();
            if (typeof(T) == typeof(IfcSurfaceCurveSweptAreaSolid)) _ifcSweptAreaSolid = _document.Model.New<IfcSurfaceCurveSweptAreaSolid>();

            //placement inside of the opening
            XbimAxis2Placement3D placement = new XbimAxis2Placement3D(_document);
            placement.SetDirections(new XbimXYZ(1, 0, 0), new XbimXYZ(0, 0, 1));
            placement.SetLocation(new XbimXYZ(0, 0, 0));

            if (_ifcSweptAreaSolid == null) throw new Exception("Init was not successfull");
            IfcSweptAreaSolid.Position = placement._ifcAxis2Placement;


        }


        protected void InitToCompositCurveProfile()
        {
            if (_initialized) throw new Exception("Object has been already initialized");

            IfcCompositeCurve compositeCurve = _document.Model.New<IfcCompositeCurve>();
            IfcArbitraryClosedProfileDef profile = _document.Model.New<IfcArbitraryClosedProfileDef>(prof => prof.OuterCurve = compositeCurve);
            profile.ProfileType = IfcProfileTypeEnum.AREA;
            _profileCurveSegments = compositeCurve.Segments;

            IfcSweptAreaSolid.SweptArea = profile;
            _initialized = true;
            _compositCurve = true;
        }

        /// <summary>
        /// Set base profile of the object to the specified rectangle. 
        /// It is not posible to add any other curves to this type of profile;
        /// </summary>
        /// <param name="Ydim">Width of the profile</param>
        /// <param name="Xdim">Length of the profile</param>
        protected void InitToRectangleProfile(double Ydim, double Xdim)
        {
            //represent wall as a rectangular profile
            IfcRectangleProfileDef rectProf = _document.Model.New<IfcRectangleProfileDef>();
            rectProf.ProfileType = IfcProfileTypeEnum.AREA;
            rectProf.XDim = Xdim;
            rectProf.YDim = Ydim;
            IfcCartesianPoint insertPoint = _document.Model.New<IfcCartesianPoint>();
            insertPoint.SetXY(Xdim/2, Ydim/2); //insert at arbitrary position
            rectProf.Position = _document.Model.New<IfcAxis2Placement2D>(); //default values should be OK (normal direction)
            rectProf.Position.Location = insertPoint;

            IfcSweptAreaSolid.SweptArea = rectProf;
            _initialized = true;
            _compositCurve = false;
        }

        /// <summary>
        /// Changes the IfcArbitraryClosedProfileDef to IfcArbitraryProfileDefWithVoids which can contain voids in the profile
        /// From now on, curves are going to the inner loop of the profile. If it is called again new inner curve is created.
        /// </summary>
        public void AddInnerCurves()
        {
            IfcArbitraryClosedProfileDef oldProfile = IfcSweptAreaSolid.SweptArea as IfcArbitraryClosedProfileDef;
            //if oldProfile is IfcArbitraryClosedProfileDef it cannot contain voids and must be changed
            if (oldProfile != null)
            {
                //use old outer curve in the new profile
                IfcCurve curve = oldProfile.OuterCurve;
                IfcArbitraryProfileDefWithVoids newProfile = _document.Model.New<IfcArbitraryProfileDefWithVoids>(prof => { prof.OuterCurve = curve; prof.ProfileType = oldProfile.ProfileType; });
                _document.Model.Delete(oldProfile);
                IfcSweptAreaSolid.SweptArea = newProfile;

                //change target of adding of new curves into the profile to the new compositeCurve
                IfcCompositeCurve compositeCurve = _document.Model.New<IfcCompositeCurve>();
                newProfile.InnerCurves.Add_Reversible(compositeCurve);
                _profileCurveSegments = compositeCurve.Segments;
            }
            else if (IfcSweptAreaSolid.SweptArea is IfcArbitraryProfileDefWithVoids)
            {
                //change target of adding of new curves into the profile to the new compositeCurve
                IfcCompositeCurve compositeCurve = _document.Model.New<IfcCompositeCurve>();
                (oldProfile as IfcArbitraryProfileDefWithVoids).InnerCurves.Add_Reversible(compositeCurve);
                _profileCurveSegments = compositeCurve.Segments;
            }
            else
            {
                throw new NotSupportedException();
            }
            
        }

        public void AddProfileCurveLine(XbimXYZ start, XbimXYZ end)
        {
            CheckInitialization();

            IfcPolyline line = _document.Model.New<IfcPolyline>();
            line.Points.Add_Reversible(start.CreateIfcCartesianPoint2D(_document));
            line.Points.Add_Reversible(end.CreateIfcCartesianPoint2D(_document));

            CreateCompositeCurveSegment(line);
        }

        public void AddProfileCurveCircleSegment(IBimAxis2Placement3D placement, XbimXYZ startPoint, XbimXYZ endPoint, double radius)
        {
            CheckInitialization();
            if (radius < 0) throw new Exception("Negative radius is not allowed.");
            XbimAxis2Placement3D ax2place = placement as XbimAxis2Placement3D;
            if (ax2place == null) throw new NotSupportedException();

            IfcAxis2Placement axis2placement = ax2place._ifcAxis2Placement;
            IfcCircle circle = _document.Model.New<IfcCircle>(cr => { cr.Position = axis2placement; cr.Radius = radius; });
            IfcCartesianPoint point1 = startPoint.CreateIfcCartesianPoint2D(_document);
            IfcCartesianPoint point2 = endPoint.CreateIfcCartesianPoint2D(_document);
            IfcTrimmedCurve trimmedCurve = _document.Model.New<IfcTrimmedCurve>(crv => { crv.BasisCurve = circle; crv.Trim1.Add_Reversible(point1); crv.Trim2.Add_Reversible(point2); crv.SenseAgreement = true; crv.MasterRepresentation = IfcTrimmingPreference.CARTESIAN; });

            CreateCompositeCurveSegment(trimmedCurve);
        }

        public void AddProfileCurveCircle(IBimAxis2Placement3D placement, double radius)
        {
            CheckInitialization();
            if (radius < 0) throw new Exception("Negative radius is not allowed.");

            XbimAxis2Placement3D ax2place = placement as XbimAxis2Placement3D;
            if (ax2place == null) throw new NotSupportedException();

            IfcAxis2Placement axis2placement = ax2place._ifcAxis2Placement;
            IfcCircle circle = _document.Model.New<IfcCircle>(cr => { cr.Position = axis2placement; cr.Radius = radius; });

            CreateCompositeCurveSegment(circle);
        }

        public void AddProfileCurveEllipse(IBimAxis2Placement3D position, double semiAxis1, double semiAxis2)  //todo: Not sure about usage of Axis2Placement3D. It might be Axis2Placement2D
        {
            CheckInitialization();
            if (semiAxis1 <= 0 || semiAxis2 <= 0) throw new Exception("Semi axes must be greater than 0.");

            XbimAxis2Placement3D ax2place = position as XbimAxis2Placement3D;
            if (ax2place == null) throw new NotSupportedException();

            IfcAxis2Placement axis2placement = ax2place._ifcAxis2Placement;
            IfcEllipse ellipse = _document.Model.New<IfcEllipse>(el => { el.Position = axis2placement; el.SemiAxis1 = semiAxis1; el.SemiAxis2 = semiAxis2; });

            CreateCompositeCurveSegment(ellipse);
        }

        public void AddProfileCurveEllipseSegment(IBimAxis2Placement3D position, double semiAxis1, double semiAxis2, //ellipse parameters
            XbimXYZ startPoint, XbimXYZ endPoint) //trimming parameters
        {
            CheckInitialization();
            if (semiAxis1 <= 0 || semiAxis2 <= 0) throw new Exception("Semi axes must be greater than 0.");

            XbimAxis2Placement3D ax2place = position as XbimAxis2Placement3D;
            if (ax2place == null) throw new NotSupportedException();

            IfcAxis2Placement placement = ax2place._ifcAxis2Placement;
            IfcEllipse ellipse = _document.Model.New<IfcEllipse>(el => { el.Position = placement; el.SemiAxis1 = semiAxis1; el.SemiAxis2 = semiAxis2; });
            IfcCartesianPoint point1 = startPoint.CreateIfcCartesianPoint(_document);
            IfcCartesianPoint point2 = endPoint.CreateIfcCartesianPoint(_document);
            IfcTrimmedCurve trimmedCurve = _document.Model.New<IfcTrimmedCurve>(crv => { crv.BasisCurve = ellipse; crv.Trim1.Add_Reversible(point1); crv.Trim2.Add_Reversible(point2); crv.SenseAgreement = true; crv.MasterRepresentation = IfcTrimmingPreference.CARTESIAN; });

            CreateCompositeCurveSegment(trimmedCurve);
        }

        private void CreateCompositeCurveSegment(IfcCurve curve)
        {
            CheckInitialization();

            IfcCompositeCurveSegment segment = _document.Model.New<IfcCompositeCurveSegment>(seg => 
            {
                seg.SameSense = true; 
                seg.Transition = IfcTransitionCode.CONTINUOUS;
                seg.ParentCurve = curve;
            });

            _profileCurveSegments.Add_Reversible(segment);
        }

        private void CheckInitialization()
        {
            if (!_initialized) throw new Exception("Parameters not initialized properly");
            if (!_compositCurve) throw new Exception("Profile is not a composite curve");
        }

        public IfcGeometricRepresentationItem GetIfcGeometricRepresentation()
        {
            return _ifcSweptAreaSolid;
        }
    }

    public enum ProfileTypeEnum
    {
        COMPOSITE_CURVE,
        RECTANGLE
    }
}
