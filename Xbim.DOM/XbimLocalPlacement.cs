using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.Kernel;
using System.Diagnostics;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM
{
    public class XbimLocalPlacement : IBimLocalPlacement
    {
        private IfcLocalPlacement _localPlacement;
        private XbimDocument _document;

        internal IfcLocalPlacement IfcLocalPlacement { get { return _localPlacement; } set { _localPlacement = value; } }
        internal IfcAxis2Placement3D IfcLocalPlacement3D { get { return _localPlacement.RelativePlacement as IfcAxis2Placement3D; } }

        internal XbimLocalPlacement(XbimDocument Document, IXbimRoot ForObject, double placementX, double placementY, double placementZ)
        {
            if (Document == null || ForObject == null) throw new ArgumentNullException();

            _document = Document;
            if (!(ForObject.AsRoot is IfcProduct))
            {
                throw new Exception("Object is not descendant of the Product object and can not have a placement.");
            }

            IfcProduct product = ForObject.AsRoot as IfcProduct;
            if (product.ObjectPlacement != null)
            {
                Debug.WriteLine("XbimLocalPlacement: Object already contains placement. It is going to be replaced with new placement.");
            }

            product.SetNewObjectLocalPlacement(placementX, placementY, placementZ);
        }

        internal XbimLocalPlacement(XbimDocument Document, IfcLocalPlacement LocalPlacement)
        {
            if (Document == null || LocalPlacement == null) throw new ArgumentNullException();

            _document = Document;
            _localPlacement = LocalPlacement;
        }

        internal XbimLocalPlacement(XbimDocument Document, double placementX, double placementY, double placementZ)
        {
            _document = Document;
            _localPlacement = _document.Model.New<IfcLocalPlacement>();
            _localPlacement.RelativePlacement = _document.Model.New<IfcAxis2Placement3D>();
            IfcAxis2Placement3D placenemt = _localPlacement.RelativePlacement as IfcAxis2Placement3D;
            placenemt.SetNewLocation(placementX, placementY, placementZ);
        }

        internal XbimLocalPlacement(XbimDocument Document, XbimAxis2Placement3D axisToPlacement)
        {
            _document = Document;
            _localPlacement = _document.Model.New<IfcLocalPlacement>();
            _localPlacement.RelativePlacement = axisToPlacement._ifcAxis2Placement;
        }

        /// <summary>
        /// Returns existing local placement of the object. It ignores grid placement if exists.
        /// </summary>
        /// <param name="Document">Document for Xbim object</param>
        /// <param name="ForObject">Xbim object for which the placement should be find</param>
        /// <returns>Xbim object for local placement</returns>
        public static XbimLocalPlacement GetExistingLocalPlacement(XbimDocument Document, IXbimRoot ForObject)
        {
            if (ForObject == null) throw new ArgumentNullException();

            IfcProduct product = ForObject.AsRoot as IfcProduct;
            if (product == null) throw new Exception("This type of object can not contain placement");

            IfcLocalPlacement placement = product.ObjectPlacement as IfcLocalPlacement;
            if (placement == null) return null;

            return new XbimLocalPlacement(Document, placement);
        }

        /// <summary>
        /// Sets relative placement of the object.
        /// </summary>
        /// <param name="LocalPlacement">Local placement of the object which this object is relative to.</param>
        public void SetPlacementRelTo(XbimLocalPlacement LocalPlacement)
        {
            _localPlacement.PlacementRelTo = LocalPlacement.IfcLocalPlacement;
        }

        public void SetLocation(double X, double Y, double Z)
        {
            IfcLocalPlacement3D.SetNewLocation(X, Y, Z);
        }

        public void SetDirectionOf_XZ(double X_axisDirection_X, double X_axisDirection_Y, double X_axisDirection_Z, double Z_axisDirection_X, double Z_axisDirection_Y, double Z_axisDirection_Z)
        {
            IfcLocalPlacement3D.SetNewDirectionOf_XZ(X_axisDirection_X, X_axisDirection_Y, X_axisDirection_Z, Z_axisDirection_X, Z_axisDirection_Y, Z_axisDirection_Z);
        }

        void IBimLocalPlacement.SetPlacementRelTo(IBimLocalPlacement LocalPlacement)
        {
            XbimLocalPlacement placement = LocalPlacement as XbimLocalPlacement;
            if (placement == null) throw new ArgumentException();
            SetPlacementRelTo(placement);
        }
    }
}
