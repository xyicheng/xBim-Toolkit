using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public abstract class XbimBuildingElementType : IXbimRoot, IBimBuildingElementType
    {
        protected XbimDocument _document;
        protected IfcTypeProduct _ifcTypeProduct;


        //properties to access some of properties of IFC objects:
        public string Name { get { return _ifcTypeProduct.Name; } set { _ifcTypeProduct.Name = value; } }
        public string Description { get { return _ifcTypeProduct.Description; } set { _ifcTypeProduct.Description = value; } }
        public XbimSingleTypeProperties Properties { get { return new XbimSingleTypeProperties(_ifcTypeProduct); } }
        public string GlobalId { get { return _ifcTypeProduct.GlobalId; } set { _ifcTypeProduct.GlobalId = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(value); } }
        public Guid Guid { get { return _ifcTypeProduct.GlobalId; } set { _ifcTypeProduct.GlobalId = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(value); } }
        public bool HasMaterials { get { return IfcMaterialLayerSet != null; } }
        public XbimMaterialLayers MaterialLayers { get { return  IfcElementType == null ? null : new XbimMaterialLayers(IfcMaterialLayerSet, this); } }
        public XbimMaterialQuantities MaterialQuantities { get { return new XbimMaterialQuantities(_ifcTypeProduct, _document); } }
        public long EntityLabel { get { return _ifcTypeProduct.EntityLabel; } }

        public XbimDocument Document { get { return _document; } }

        public IfcTypeProduct IfcTypeProduct { get { return _ifcTypeProduct; } }
        internal IfcElementType IfcElementType { get { return _ifcTypeProduct as IfcElementType; } }
        public IfcMaterialLayerSet IfcMaterialLayerSet { get { return (IfcTypeProduct is IfcElementType)?(_ifcTypeProduct as IfcElementType).GetMaterial() as IfcMaterialLayerSet: null; } }
        internal IfcClassificationNotation IfcClassificationNotation { get { return _ifcTypeProduct.GetFirstClassificationNotation(_document.Model); } }



       
        internal XbimBuildingElementType(XbimDocument document)
        {
            _document = document;
        }


        public void AddMaterialLayer(XbimMaterial material, double thickness)
        {
            AddMaterialLayer(material, thickness, false);
        }

        

        public void AddMaterialLayer(XbimMaterial material, double thickness, bool isVentilated)
        {
            if (IfcElementType == null) return;
            if (material == null) return;
            IfcMaterialLayerSet materialLayerSet = IfcMaterialLayerSet;
            if (materialLayerSet == null)
            {
                materialLayerSet = _document.Model.New<IfcMaterialLayerSet>(set => set.LayerSetName = _ifcTypeProduct.Name);
                IfcElementType.SetMaterial(materialLayerSet);
            }
            IfcMaterialLayer matLayer = _document.Model.New<IfcMaterialLayer>();
            matLayer.LayerThickness = thickness;
            matLayer.Material = material;
            matLayer.IsVentilated = isVentilated;
            materialLayerSet.MaterialLayers.Add_Reversible(matLayer);

        }

        public void AddMaterialLayer(XbimMaterial material, double thickness, bool isVentilated, XbimMaterialFunctionEnum function)
        {
            if (!HasMaterials)
            {
                _ifcTypeProduct.SetMaterial(_document.Model.New<IfcMaterialLayerSet>());
            }
            XbimMaterialLayer layer = MaterialLayers.AddMaterialLayer(material, thickness, isVentilated);
            SetMaterialLayerFunction(layer, function);
        }

        public void SetMaterialLayerFunction(XbimMaterialLayer materialLayer, XbimMaterialFunctionEnum function)
        {
            if (materialLayer == null) return;
            int materialIndex = MaterialLayers.IndexOf(materialLayer);
            if (materialIndex < 0) return; //check if material exists in the material layer set
            IfcTypeProduct.SetPropertyTableItemValue("xbim_MaterialFunctionAssignment", "MaterialFunctionAssignment", (IfcInteger)materialIndex, (IfcLabel)Enum.GetName(typeof(XbimMaterialFunctionEnum), function));
        }

        public XbimMaterialFunctionEnum GetMaterialFunction(int materialLayerIndex)
        {
            object funObj = IfcTypeProduct.GetPropertyTableItemValue("xbim_MaterialFunctionAssignment", "MaterialFunctionAssignment", (IfcInteger)materialLayerIndex);
            if (funObj != null)
            {
                try
                {
                    string function = (IfcLabel)funObj;
                    XbimMaterialFunctionEnum result = (XbimMaterialFunctionEnum)Enum.Parse(typeof(XbimMaterialFunctionEnum), function, true);
                    return result;
                }
                catch (Exception)
                {
#if DEBUG
                    throw new Exception("Enumeration member not found.");
#endif
                }
            }
            return XbimMaterialFunctionEnum.None;

        }
        public XbimMaterialFunctionEnum GetMaterialFunction(XbimMaterialLayer materialLayer)
        {
            int materialIndex = MaterialLayers.IndexOf(materialLayer);
            return GetMaterialFunction(materialIndex);
        }
        

        public Ifc2x3.Kernel.IfcRoot AsRoot
        {
            get { return _ifcTypeProduct; }
        }


        public void SetGlobalId(Guid guid)
        {
            _ifcTypeProduct.GlobalId = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
        }

        public static implicit operator IfcBuildingElementType(XbimBuildingElementType elem)
        {
            if (elem.IfcTypeProduct is IfcBuildingElementType)
            return elem.IfcElementType as IfcBuildingElementType;
            return null;
        }

        //public INRMQuantities NRMQuantities { get { return new NRMQuantities(this); } }

        #region NRM quantities for presentation
        private IEnumerable<XbimBuildingElement> Instances { get { return Document.GetElementsOfType(this); } }
        public double? Area 
        {
            get
            {
                double? area = null;
                foreach (var element in Instances)
                {
                    double? elemArea = element.NRMQuantities.Area;
                    if (elemArea != null)
                    {
                        if (area == null) area = 0;
                        area += elemArea;
                    }
                }
                if (area == null)
                    return null;
                else
                {
                    double result = area ?? 0;
                    return Math.Round(result, 2);
                }
            } 
        }

        public double? Volume 
        { 
            get 
            {
                double? volume = null;
                foreach (var element in Instances)
                {
                    double? elemVolume = element.NRMQuantities.Volume;
                    if (elemVolume != null)
                    {
                        if (volume == null) volume = 0;
                        volume += elemVolume;
                    }
                }
                if (volume == null)
                    return null;
                else
                {
                    double result = volume ?? 0;
                    return Math.Round(result, 2);
                }
            }
        }
        public double? Length
        {
            get
            {
                double? length = null;
                foreach (var element in Instances)
                {
                    double? elemlen = element.NRMQuantities.Length;
                    if (elemlen != null)
                    {
                        if (length == null) length = 0;
                        length += elemlen;
                    }
                }
                if (length == null)
                    return null;
                else
                {
                    double result = length ?? 0;
                    return Math.Round(result, 2);
               }
            }
        }
        public double? Count
        {
            get
            {
                double? count = null;
                foreach (var element in Instances)
                {
                    double? elemCount = element.NRMQuantities.Count;
                    if (elemCount != null)
                    {
                        if (count == null) count = 0;
                        count += elemCount;
                    }
                }
                if (count == null)
                    return null;
                else
                {
                    double result = count ?? 0;
                    return Math.Round(result, 0);
                }
            }
        }
        public double? Number
        {
            get
            {
                double? num = null;
                foreach (var element in Instances)
                {
                    double? elemNum = element.NRMQuantities.Number;
                    if (elemNum != null)
                    {
                        if (num == null) num = 0;
                        num += elemNum;
                    }
                }
                if (num == null)
                    return null;
                else
                {
                    double result = num ?? 0;
                    return Math.Round(result, 0);
                }
            }
        }
        #endregion

        /// <summary>
        /// Adds the specified element to the decomposition of this element
        /// </summary>
        /// <param name="element"></param>
        public void AddDecomposingElement(XbimBuildingElement element)
        {
            IfcTypeProduct.AddDecomposingObjectToFirstAggregation(Document.Model, element.IfcBuildingElement);
        }

        #region IBimBuildingElementType

        private XbimMaterial GetXbimMaterial(IBimMaterial material)
        {
            XbimMaterial mat = material as XbimMaterial;
            if (mat == null) throw new ArgumentException();
            return mat;
        }
        void IBimBuildingElementType.AddMaterialLayer(IBimMaterial material, double thickness)
        {
            AddMaterialLayer(GetXbimMaterial(material), thickness);
        }

        void IBimBuildingElementType.AddMaterialLayer(IBimMaterial material, double thickness, bool isVentilated)
        {
            AddMaterialLayer(GetXbimMaterial(material), thickness, isVentilated);
        }

        void IBimBuildingElementType.AddMaterialLayer(IBimMaterial material, double thickness, bool isVentilated, XbimMaterialFunctionEnum function)
        {
            AddMaterialLayer(GetXbimMaterial(material), thickness, isVentilated, function);
        }

        Guid IBimBuildingElementType.GlobalId
        {
            get
            {
                return _ifcTypeProduct.GlobalId;
            }
            set
            {
                SetGlobalId(value);
            }
        }

        IBimSingleProperties IBimBuildingElementType.Properties
        {
            get { return Properties as IBimSingleProperties; }
        }
        #endregion

        public int NumberOfInst { get { return Document.GetElementsOfType(this).Count(); } }
    }

    public class XbimMaterialLayers : IList<XbimMaterialLayer>
    {
        private XbimDocument _document;
        private IfcElementType _ifcElementType;
        private IfcMaterialLayerSet _ifcMaterialLayerSet;

        private List<XbimMaterialLayer> _tempList;

        internal XbimMaterialLayers(IfcMaterialLayerSet ifcMaterialLayerSet, XbimBuildingElementType element)
        {
            
            _document = element.Document;
            if (element.IfcElementType == null) throw new Exception("This type of element can not contain layers");
            if (ifcMaterialLayerSet == null) throw new Exception("No material layer set specified.");

            _ifcElementType = element.IfcElementType;
            _ifcMaterialLayerSet = ifcMaterialLayerSet;
        }

        public void AddMaterialLayer(XbimMaterial material, double thickness)
        {
            AddMaterialLayer(material, thickness, false);
        }

        public XbimMaterialLayer AddMaterialLayer(XbimMaterial material, double thickness, bool isVentilated)
        {
            if (material == null) return null;
            if (_ifcMaterialLayerSet == null)
            {

                _ifcMaterialLayerSet = _document.Model.New<IfcMaterialLayerSet>(l => l.LayerSetName = _ifcElementType.Name);
                _ifcElementType.SetMaterial(_ifcMaterialLayerSet);
            }
            IfcMaterialLayer matLayer = _document.Model.New<IfcMaterialLayer>();
            matLayer.LayerThickness = thickness;
            matLayer.Material = material;
            matLayer.IsVentilated = isVentilated;
            _ifcMaterialLayerSet.MaterialLayers.Add_Reversible(matLayer);
            return new XbimMaterialLayer(_document, matLayer);

        }

        public void Add(XbimMaterialLayer layer)
        {
            _ifcMaterialLayerSet.MaterialLayers.Add_Reversible(layer._ifcMatLayer);
            
        }

        public void Insert(int index, XbimMaterialLayer layer)
        {
            _ifcMaterialLayerSet.MaterialLayers.Insert_Reversible(index, layer._ifcMatLayer);
        }

       

        public int IndexOf(XbimMaterialLayer item)
        {
            return _ifcMaterialLayerSet.MaterialLayers.IndexOf(item._ifcMatLayer);
        }

        public void RemoveAt(int index)
        {
            _ifcMaterialLayerSet.MaterialLayers.RemoveAt_Reversible(index);
        }

        public XbimMaterialLayer this[int index]
        {
            get
            {
                return new XbimMaterialLayer(_document, _ifcMaterialLayerSet.MaterialLayers[index]);
            }
            set
            {
                _ifcMaterialLayerSet.MaterialLayers.Insert_Reversible(index, value._ifcMatLayer);
            }
        }

        public void Clear()
        {
            _ifcMaterialLayerSet.MaterialLayers.Clear_Reversible();
        }

        public bool Contains(XbimMaterialLayer item)
        {
            return _ifcMaterialLayerSet.MaterialLayers.Contains(item._ifcMatLayer);
        }

        public void CopyTo(XbimMaterialLayer[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _ifcMaterialLayerSet.MaterialLayers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(XbimMaterialLayer item)
        {
            return _ifcMaterialLayerSet.MaterialLayers.Remove_Reversible(item._ifcMatLayer);
        }

        public IEnumerator<XbimMaterialLayer> GetEnumerator()
        {
            _tempList = new List<XbimMaterialLayer>();
            foreach (IfcMaterialLayer matLayer in _ifcMaterialLayerSet.MaterialLayers)
            {
                XbimMaterial material = _document.Materials.Where(mat => mat.Name == matLayer.Material.Name).FirstOrDefault();
                if (material == null) material = new XbimMaterial(_document, matLayer.Material);
                if (material == null) throw new Exception("Material is not defined in the XbimDocument.");
                _tempList.Add(new XbimMaterialLayer(_document, matLayer));
            }
            return _tempList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class XbimMaterialLayer
    {
        internal IfcMaterialLayer _ifcMatLayer;
        private XbimDocument _document;

        public XbimMaterial Material { 
            get { return new XbimMaterial(_document, _ifcMatLayer.Material); }
            set { _ifcMatLayer.Material = value.Material; }
        }
        public bool? IsVentilated { 
            get { return _ifcMatLayer.IsVentilated; }
            set { _ifcMatLayer.IsVentilated = value; }
        }
        public double Thickness { 
            get { return _ifcMatLayer.LayerThickness; }
            set { _ifcMatLayer.LayerThickness = value; }
        }

        internal XbimMaterialLayer(XbimDocument document, IfcMaterialLayer matLayer)
        {
            _ifcMatLayer = matLayer;
            _document = document;
        }
    }

    public enum XbimMaterialFunctionEnum
    {
        None,
        Structure,
        Substrate,
        Insulation,
        Finish1,
        Finish2,
        Membrane,
        StructuralDeck
       
    }

}
