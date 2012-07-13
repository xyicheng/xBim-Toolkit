using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions;
using System.Diagnostics;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace Xbim.DOM
{
    public abstract class XbimSpatialStructureElement : IXbimRoot, IBimSpatialStructureElement
    {
        protected IfcSpatialStructureElement _spatialElement;
        protected XbimDocument _document;

        internal IfcSpatialStructureElement SpatialStructureElement { get { return _spatialElement; } }
        internal IEnumerable<IfcSpatialStructureElement> ContainingIfcStructuralElements { get { return SpatialStructureElement.GetContainingStructuralElements(); } }

        public IEnumerable<XbimSpatialStructureElement> ContainingStructuralElements 
        {
            get
            {
                List<XbimSpatialStructureElement> result = new List<XbimSpatialStructureElement>();
                foreach (IfcSpatialStructureElement element in ContainingIfcStructuralElements)
                {
                    XbimSpatialStructureElement newElement = null;
                    if (element is IfcBuilding) newElement = new XbimBuilding(_document, element as IfcBuilding);
                    if (element is IfcBuildingStorey) newElement = new XbimBuildingStorey(_document, element as IfcBuildingStorey);
                    if (element is IfcSpace) newElement = new XbimSpace(_document, element as IfcSpace);
                    if (element is IfcSite) newElement = new XbimSite(_document, element as IfcSite);
                    result.Add(newElement);
                }
                return result;
            }
        }

        //public properties common for all spatial structure elements
        public IXbimSingleProperties SingleProperties { get { return new XbimSingleProperties(this._spatialElement); } }
        public string Name { get { return _spatialElement.Name; } set { _spatialElement.Name = value; } }
        public string LongName { get { return _spatialElement.LongName; } set { _spatialElement.LongName = value; } }
        public XbimDocument Document { get { return _document; } }
        public Guid Guid { get { return _spatialElement.GlobalId; } set { _spatialElement.GlobalId = new IfcGloballyUniqueId(value); } }

        internal XbimSpatialStructureElement(XbimDocument document, IfcSpatialStructureElement element)
        {
            if (document == null) throw new ArgumentNullException("XbimSpatialStructureElement constructor can not have null arguments.");
            _document = document;
            _spatialElement = element;
        }

        public void AddToSpatialDecomposition(XbimSpatialStructureElement child)
        {
            _spatialElement.AddToSpatialDecomposition(child.SpatialStructureElement);
        }

        /// <summary>
        /// Sets new local placement of the object. Existing placement is overwritten.
        /// Setting of the placement like direction of X and Z axes must be done using methods of the XbimLocalPlacement.
        /// </summary>
        /// <param name="X">X coordinate of the placement</param>
        /// <param name="Y">Y coordinate of the placement</param>
        /// <param name="Z">Z coordinate of the placement</param>
        /// <returns>XbimLocalPlacement object representing local placement of the object</returns>
        public XbimLocalPlacement SetNewLocalPlacement(double X, double Y, double Z)
        {
            //local placement is assigned to this object when created
            //setting of the placement like direction of X and Z axes must be done using methods of the XbimLocalPlacement
            return new XbimLocalPlacement(_document, this, X, Y, Z);
        }


        /// <summary>
        /// Sets local placement of the object to the information 
        /// specified in the object of XbimLocalPlacement.
        /// </summary>
        /// <param name="XbimLocalPlacement">Xbim local placement</param>
        public void SetLocalPlacement(XbimLocalPlacement XbimLocalPlacement)
        {
            _spatialElement.ObjectPlacement = XbimLocalPlacement.IfcLocalPlacement;
        }

        internal IfcObjectPlacement GetObjectPlacement()
        {
            return _spatialElement.ObjectPlacement;
        }
        /// <summary>
        /// Adds geometry to set of the first IfcRepresentationItems. If it does not exist it is created.
        /// </summary>
        /// <param name="Geometry">Xbim geometry to be added to the element.</param>
        public void AddGeometry(IXbimGeometry Geometry)
        {
            IfcGeometricRepresentationItem ifcGeometry = Geometry.GetIfcGeometricRepresentation();

            if (Geometry == null)
            {
                Debug.WriteLine("XbimBuildingElement: No geometry to be set.");
                return;
            }

            if (_spatialElement.GetFirstShapeRepresentation() == null)
            {
                _spatialElement.GetNewBrepShapeRepresentation(((IfcProject)_document.Model.IfcProject).ModelContext()).Items.Add_Reversible(ifcGeometry);
            }
            else
            {
                _spatialElement.GetFirstShapeRepresentation().Items.Add_Reversible(ifcGeometry);
            }
        }

        public void AddGeometry(IEnumerable<IXbimGeometry> GeometryList)
        {
            foreach (IXbimGeometry geom in GeometryList)
            {
                AddGeometry(geom);
            }
        }

        public Ifc2x3.Kernel.IfcRoot AsRoot
        {
            get { return _spatialElement; }
        }

        public string GlobalId
        {
            get { return _spatialElement.GlobalId; }
            set { _spatialElement.GlobalId = new IfcGloballyUniqueId(value); }
        }

        public void SetGlobalId(Guid guid)
        {
            _spatialElement.GlobalId = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
        }

        protected IfcElementCompositionEnum GeIfcElementCompositionEnum(XbimElementCompositionEnum enu)
        {
            switch (enu)
            {
                case XbimElementCompositionEnum.COMPLEX: return IfcElementCompositionEnum.COMPLEX;
                case XbimElementCompositionEnum.ELEMENT: return IfcElementCompositionEnum.ELEMENT;
                case XbimElementCompositionEnum.PARTIAL: return IfcElementCompositionEnum.PARTIAL;
            }
            return IfcElementCompositionEnum.ELEMENT;
        }

        /// <summary>
        /// Adds element as element contained in the spatial structure element.
        /// </summary>
        /// </summary>
        /// <param name="element">Building element to be added</param>
        public void AddContainedBuildingElement(XbimBuildingElement element)
        {
            _spatialElement.AddElement(element.IfcBuildingElement);
        }

        /// <summary>
        /// Adds element with specified GUID as element contained in the spatial structure element.
        /// If the element with that GUID does not exist, function returns false and nothing is added.
        /// </summary>
        /// <param name="guid">GUID of the element</param>
        /// <returns>TRUE if the element is added, FALSE otherwise</returns>
        public bool AddContainedBuildingElement(Guid guid)
        {
            IfcGloballyUniqueId globID = new IfcGloballyUniqueId(guid);
            IfcProduct product = _document.Model.InstancesWhere<IfcProduct>(prod => prod.GlobalId == guid).FirstOrDefault();
            if (product != null)
            {
                _spatialElement.AddElement(product);
                return true;
            }
            return false;
        }

        

        /// <summary>
        /// Adds geometry to set of the first IfcRepresentationItems. If it does not exist it is created.
        /// </summary>
        /// <param name="Geometry">Xbim geometry to be added to the element.</param>
        public virtual void AddGeometrySweptSolid(IXbimGeometry Geometry)
        {
            IfcGeometricRepresentationItem ifcGeometry = Geometry.GetIfcGeometricRepresentation();

            if (Geometry == null)
            {
                Debug.WriteLine("XbimBuildingElement: No geometry to be set.");
                return;
            }

            IfcShapeRepresentation shape = _spatialElement.GetOrCreateSweptSolidShapeRepresentation(((IfcProject)_document.IfcModel().IfcProject).ModelContext());
            shape.Items.Add_Reversible(ifcGeometry);
        }

        



        #region IBimSpatialStructureElement
        bool IBimSpatialStructureElement.AddContainedBuildingElement(Guid guid)
        {
            IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            XbimBuildingElement element = Document.AllBuildingElements.Where(el => el.GlobalId == id).FirstOrDefault();
            if (element != null)
            {
                AddContainedBuildingElement(element);
                return true;
            }
            return false;
        }

        void IBimSpatialStructureElement.AddContainedBuildingElement(IBimBuildingElement element)
        {
            XbimBuildingElement elem = element as XbimBuildingElement;
            if (elem == null) throw new ArgumentException();
            AddContainedBuildingElement(elem);
        }

       

        void IBimSpatialStructureElement.AddGeometrySweptSolid(IBimSweptSolid Geometry)
        {
            if (Geometry is IXbimGeometry)
            {
                AddGeometrySweptSolid(Geometry as IXbimGeometry);
            }
        }

        void IBimSpatialStructureElement.AddToSpatialDecomposition(IBimSpatialStructureElement child)
        {
            XbimSpatialStructureElement element = child as XbimSpatialStructureElement;
            if (element == null) throw new ArgumentException();
            AddToSpatialDecomposition(element);
        }

        object IBimSpatialStructureElement.GlobalId
        {
            get
            {
                return GlobalId;
            }
            set
            {
                if (value is string) GlobalId = value as string;
                if (value is Guid) SetGlobalId((Guid)value);
            }
        }

        void IBimSpatialStructureElement.SetLocalPlacement(IBimLocalPlacement xbimLocalPlacement)
        {
            XbimLocalPlacement placement = xbimLocalPlacement as XbimLocalPlacement;
            if (placement == null) throw new ArgumentException();
            SetLocalPlacement(placement);
        }

        IBimSingleProperties IBimSpatialStructureElement.SingleProperties
        {
            get { return SingleProperties as IBimSingleProperties; }
        }

        void IBimSpatialStructureElement.AddBoundingElementToSpace(IBimSpatialStructureElement space, IBimBuildingElement element, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external)
        {
            XbimSpace xSpace = space as XbimSpace;
            XbimBuildingElement xElement = element as XbimBuildingElement;
            if (xSpace == null || xElement == null) throw new ArgumentException();
            xSpace.AddBoundingElement(xElement, type, external);
        }

        void IBimSpatialStructureElement.AddBoundingElementToSpace(IBimSpatialStructureElement space, Guid elementGuid, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external)
        {
            XbimSpace xSpace = space as XbimSpace;
            if (xSpace == null ) throw new ArgumentException();
            xSpace.AddBoundingElement(elementGuid, type, external);
        }

        #endregion
    }

    public enum XbimElementCompositionEnum
    {
        COMPLEX,
        ELEMENT,
        PARTIAL
    }

    
}
