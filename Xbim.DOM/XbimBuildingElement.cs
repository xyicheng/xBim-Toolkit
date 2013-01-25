using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.XbimExtensions;
using System.Diagnostics;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace Xbim.DOM
{
    public abstract class XbimBuildingElement: IXbimRoot, IBimBuildingElement
    {
        //core field of this object
        protected IfcBuildingElement _ifcBuildingElement;

        //Xbim fields of this object
        protected XbimDocument _document;

        public IfcBuildingElement IfcBuildingElement { get { return _ifcBuildingElement; } }
        internal IfcMaterialLayerSetUsage IfcMaterialLayerSetUsage { get { return _ifcBuildingElement.GetMaterialLayerSetUsage(_document.Model); } }
        internal IfcTypeObject IfcTypeObject { 
            get { return _ifcBuildingElement.GetDefiningType(_document.Model); }
            set { _ifcBuildingElement.SetDefiningType(value, _document.Model); }
        }

        internal XbimBuildingElement(XbimDocument document)
        {
            if (document == null) throw new ArgumentNullException("XbimBuildingElement constructor can not have null arguments.");
            _document = document;
        }

        public XbimDocument Document { get { return _document; } }
        public Ifc2x3.Kernel.IfcRoot AsRoot { get { return _ifcBuildingElement; } }
        public XbimMaterialQuantities MaterialQuantities { get { return new XbimMaterialQuantities(_ifcBuildingElement, _document); } }
        public XbimSingleProperties SingleProperties { get { return new XbimSingleProperties(_ifcBuildingElement); } }
        public string GlobalId { get { return _ifcBuildingElement.GlobalId; } set { _ifcBuildingElement.GlobalId = new IfcGloballyUniqueId(value); } }
        public abstract XbimBuildingElementType ElementType { get; }
        public string Name { get { return IfcBuildingElement.Name; } set { IfcBuildingElement.Name = value; } }
        public INRMQuantities NRMQuantities { get { return new NRMQuantities(this); } }
        public Guid Guid { get { return _ifcBuildingElement.GlobalId; } set { _ifcBuildingElement.GlobalId = new IfcGloballyUniqueId(value); } }
        public long EntityLabel { get { return _ifcBuildingElement.EntityLabel; } }

        public static implicit operator IfcBuildingElement(XbimBuildingElement elem)
        {
            return elem._ifcBuildingElement;
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
            IfcShapeRepresentation shape = _ifcBuildingElement.GetOrCreateSweptSolidShapeRepresentation(((IfcProject)_document.IfcModel().IfcProject).ModelContext());
            shape.Items.Add_Reversible(ifcGeometry);
        }


        

        ///// <summary>
        ///// Sets new local placement of the object. Existing placement is overwritten.
        ///// Setting of the placement like direction of X and Z axes must be done using methods of the XbimLocalPlacement.
        ///// </summary>
        ///// <param name="X">X coordinate of the placement</param>
        ///// <param name="Y">Y coordinate of the placement</param>
        ///// <param name="Z">Z coordinate of the placement</param>
        ///// <returns>XbimLocalPlacement object representing local placement of the object</returns>
        //public void SetLocalPlacement(double X, double Y, double Z)
        //{
        //    //local placement is assigned to this object when created
        //    //setting of the placement like direction of X and Z axes must be done using methods of the XbimLocalPlacement
        //    XbimLocalPlacement placement = new XbimLocalPlacement(_document, this, X, Y, Z);
        //    SetLocalPlacement(placement);
        //}

        public void SetLocalPlacement(XbimLocalPlacement XbimLocalPlacement)
        {
            if (_ifcBuildingElement.ObjectPlacement != null)
            {
                IfcLocalPlacement placement = _ifcBuildingElement.ObjectPlacement as IfcLocalPlacement;
                if (placement != null)
                {
                    placement.RelativePlacement = XbimLocalPlacement.IfcLocalPlacement.RelativePlacement;
                    placement.PlacementRelTo = XbimLocalPlacement.IfcLocalPlacement.PlacementRelTo;
                    Document.Model.Delete(XbimLocalPlacement.IfcLocalPlacement);
                    XbimLocalPlacement.IfcLocalPlacement = placement;
                }

            }
            _ifcBuildingElement.ObjectPlacement = XbimLocalPlacement.IfcLocalPlacement;
        }

        public void SetMaterialLayerSetUsage(XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum> conv1 = new EnumConvertor<XbimLayerSetDirectionEnum, IfcLayerSetDirectionEnum>();
            IfcLayerSetDirectionEnum direction = conv1.Conversion(MaterialLayersDirection);

            EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum> conv2 = new EnumConvertor<XbimDirectionSenseEnum, IfcDirectionSenseEnum>();
            IfcDirectionSenseEnum sense = conv2.Conversion(MaterialLayersDirectionSense);
            _ifcBuildingElement.SetMaterialLayerSetUsage(ElementType.IfcMaterialLayerSet, direction, sense, MaterialLayersOffsett);
        }

        public IBimLocalPlacement LocalPlacement { get { return new XbimLocalPlacement(Document, _ifcBuildingElement.ObjectPlacement as IfcLocalPlacement); } }

        public void SetGlobalId(Guid guid)
        {
            _ifcBuildingElement.GlobalId = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
        }

        public bool IsPartOfOtherBuildingElement
        {
            get
            {
                IEnumerable<IfcRelDecomposes> decomposes = IfcBuildingElement.Decomposes;
                foreach (IfcRelDecomposes rel in decomposes)
                {
                    foreach (IfcObjectDefinition obj in rel.RelatedObjects)
                    {
                        if (obj is IfcBuildingElement) return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Adds the specified element to the decomposition of this element
        /// </summary>
        /// <param name="element"></param>
        public void AddDecomposingElement(XbimBuildingElement element)
        {
            IfcBuildingElement.AddDecomposingObjectToFirstAggregation(Document.Model, element.IfcBuildingElement);
        }


        public void RefreshMaterialLayerSetUsage()
        {
            if (_ifcBuildingElement.GetMaterialLayerSetUsage(Document.Model) == null)
            {
                _ifcBuildingElement.SetMaterialLayerSetUsage(this.ElementType.IfcMaterialLayerSet, IfcLayerSetDirectionEnum.AXIS2, IfcDirectionSenseEnum.POSITIVE, 0);
            }
        }

        #region IBimBuildingElement
        void IBimBuildingElement.AddDecomposingElement(IBimBuildingElement element)
        {
            XbimBuildingElement el = element as XbimBuildingElement;
            if (el != null) AddDecomposingElement(el);
        }

       

        void IBimBuildingElement.AddGeometrySweptSolid(IBimSweptSolid Geometry)
        {
            if (Geometry is IXbimGeometry)
            {
                AddGeometrySweptSolid(Geometry as IXbimGeometry);
            }
        }

        IBimBuildingElementType IBimBuildingElement.ElementType
        {
            get { return ElementType as IBimBuildingElementType; }
        }

        Guid IBimBuildingElement.GlobalId
        {
            get
            {
                return _ifcBuildingElement.GlobalId ;
            }
            set
            {
                SetGlobalId(value);
            }
        }

        void IBimBuildingElement.SetLocalPlacement(IBimLocalPlacement localPlacement)
        {
            XbimLocalPlacement placement = localPlacement as XbimLocalPlacement;
            if (placement == null) throw new ArgumentException();
            SetLocalPlacement(placement);
        }
        

        IBimSingleProperties IBimBuildingElement.Properties
        {
            get { return SingleProperties; }
        }

        void IBimBuildingElement.AddMaterialVolume(string materialName, double volume)
        {
            double oldValue = MaterialQuantities.GetMaterialVolume(materialName) ?? 0;
            MaterialQuantities.SetMaterialVolume(materialName, volume + oldValue);
        }

        void IBimBuildingElement.AddMaterialArea(string materialName, double area)
        {
            double oldValue = MaterialQuantities.GetMaterialArea(materialName) ?? 0;
            MaterialQuantities.SetMaterialVolume(materialName, area + oldValue);
        }


        #endregion
    }

    
}
