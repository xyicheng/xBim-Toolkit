using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.GeometryResource;
using System.Diagnostics;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.DOM.PropertiesQuantities;
namespace Xbim.DOM
{
    public class XbimRoof :XbimBuildingElement
    {

        #region constructors
        internal XbimRoof(XbimDocument document, XbimRoofTypeEnum roofType)
            : base(document)
        {
            _document.Roofs.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcRoof>();
            IfcRoof.ShapeType = GetIfcRoofTypeEnum(roofType);
        }


        private IfcRoofTypeEnum GetIfcRoofTypeEnum(XbimRoofTypeEnum roofType)
        {
            return (IfcRoofTypeEnum)Enum.Parse(typeof(IfcRoofTypeEnum), Enum.GetName(typeof(XbimRoofTypeEnum), roofType));
        }


        internal XbimRoof(XbimDocument document, IfcRoof roof)
            : base(document)
        {
            _ifcBuildingElement = roof;
        }
        #endregion

        #region helpers
        internal IfcRoof IfcRoof { get { return IfcBuildingElement as IfcRoof; } }
        //control to create valid roof. If it is decomposed by slabs then it can not have a geometry representation and material layers usage does not have a sense
        private bool CanBeDecomposedByElements
        {
            get
            {
                if (IfcRoof.GetFirstShapeRepresentation() != null) return false;
                return true;
            }
        }
        private bool CanHaveOwnGeometry
        {
            get
            {
                IEnumerable<IfcRelAggregates> rels = IfcRoof.IsDecomposedBy.OfType<IfcRelAggregates>().Where<IfcRelAggregates>(rel => rel.RelatedObjects.OfType<IfcBuildingElement>().FirstOrDefault() != null);
                if (rels.FirstOrDefault() != null) return false;
                return true;
            }
        }
        #endregion

        public void AddMaterialLayer(XbimMaterial material, double thickness, bool isVentilated, XbimMaterialFunctionEnum function)
        {
            if (!CanHaveOwnGeometry) throw new Exception("Roof has decomposing elements so it can not have its own material layers specified");

            IfcMaterialLayer matLayer = _document.Model.Instances.New<IfcMaterialLayer>();
            matLayer.Material = material.Material;
            matLayer.LayerThickness = thickness;

            if (IfcMaterialLayerSetUsage == null)
            {
                IfcMaterialLayerSet matLayerSet = _document.Model.Instances.New<IfcMaterialLayerSet>();
                //create new material layer set and set its usage
                _ifcBuildingElement.SetMaterialLayerSetUsage(matLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
            }

            IfcMaterialLayerSet layerSet = IfcMaterialLayerSetUsage.ForLayerSet;
            layerSet.MaterialLayers.Add_Reversible(matLayer);
        }



        new public void AddDecomposingElement(XbimBuildingElement element)
        {
            if (!CanBeDecomposedByElements) throw new Exception("Roof has its own geometry or material layers usage. So it can not be decomposed.");
            IfcRoof.AddDecomposingObjectToFirstAggregation(_document.Model, element.IfcBuildingElement);
        }

        public IEnumerable<XbimBuildingElement> GetDecomposingElements()
        {
            IEnumerable<IfcRelAggregates> rels = IfcRoof.IsDecomposedBy.OfType<IfcRelAggregates>().Where<IfcRelAggregates>(rel => rel.RelatedObjects.OfType<IfcBuildingElement>().FirstOrDefault() != null);
            IList<XbimBuildingElement> elements = new List<XbimBuildingElement>();
            foreach (IfcRelAggregates relation in rels)
            {
                foreach (IfcObjectDefinition obj in relation.RelatedObjects)
                {
                    if (obj is IfcSlab) { elements.Add(new XbimSlab(_document, obj as IfcSlab)); continue; }
                    if (obj is IfcPlate) { elements.Add(new XbimPlate(_document, obj as IfcPlate));continue ;}
                    if (obj is IfcWall)  { elements.Add(new XbimWall(_document, obj as IfcWall));  continue ;}
                    if (obj is IfcRoof) { elements.Add(new XbimRoof(_document, obj as IfcRoof)); continue; }
#if Debug
                    throw new NotImplementedException("This type of object is not supperted to be returned as part of the roof.");
#endif
                }
            }


            return elements;
        }

        #region Get and set material functions
        public void SetMaterialFunction(XbimMaterial material, XbimMaterialFunctionEnum function)
        {
            IfcBuildingElement.SetPropertyTableItemValue("xbim_MaterialFunctionAssignment", "MaterialFunctionAssignment", (IfcLabel)material.Name, (IfcLabel)Enum.GetName(typeof(XbimMaterialFunctionEnum), function));
        }
        public XbimMaterialFunctionEnum GetMaterialFunction(XbimMaterial material)
        {
            object funObj = IfcBuildingElement.GetPropertyTableItemValue("xbim_MaterialFunctionAssignment", "MaterialFunctionAssignment", (IfcLabel)material.Name);
            if (funObj != null)
            {
                string function = (IfcLabel)funObj;
                XbimMaterialFunctionEnum result = (XbimMaterialFunctionEnum)Enum.Parse(typeof(XbimMaterialFunctionEnum), function, true);
                return result;
            }
            return XbimMaterialFunctionEnum.None;

        }
        #endregion

        #region public properties
        public XbimRoofCommonProperties CommonProperties { get { return new XbimRoofCommonProperties(IfcRoof); } }
        public XbimRoofQuantities Quantities { get { return new XbimRoofQuantities(IfcRoof); } }
        public XbimRoofTypeEnum Shapetype { get { return (XbimRoofTypeEnum)Enum.Parse(typeof(XbimRoofTypeEnum), Enum.GetName(typeof(IfcRoofTypeEnum), IfcRoof.ShapeType)); } }

        public override XbimBuildingElementType ElementType { get { return null; } }
        
        /// <summary>
        /// Roof does not have its own element type but if it is decomposed by slabs it contains appropriate information
        /// </summary>
        public XbimSlabType SlabType
        {
            get
            {
                IEnumerable<IfcRelAggregates> rels = IfcRoof.IsDecomposedBy.OfType<IfcRelAggregates>().Where<IfcRelAggregates>(rel => rel.RelatedObjects.OfType<IfcSlab>().FirstOrDefault() != null);
                IfcRelAggregates relation = rels.FirstOrDefault();
                if (relation == null) return null;
                IfcSlab slab = relation.RelatedObjects.OfType<IfcSlab>().FirstOrDefault();
                return new XbimSlabType(_document, slab.GetDefiningType() as IfcSlabType);
            }
        }

        //public NRMRoofQuantities NRMQuantities { get { return new NRMRoofQuantities(this); } }

        #endregion
    }

    
}
