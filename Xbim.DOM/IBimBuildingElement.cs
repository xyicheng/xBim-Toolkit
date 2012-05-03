using System;
using Xbim.DOM.PropertiesQuantities;
namespace Xbim.DOM
{
    public interface IBimBuildingElement
    {
        void AddDecomposingElement(IBimBuildingElement element);
        
        void AddGeometrySweptSolid(IBimSweptSolid Geometry);
        IBimBuildingElementType ElementType { get; }
        Guid GlobalId { get; set; }
        string Name { get; set; }
        void SetLocalPlacement(IBimLocalPlacement localPlacement);
        IBimSingleProperties Properties { get; }
        void AddMaterialVolume(string materialName, double volume);
        void AddMaterialArea(string materialName, double area);
        INRMQuantities NRMQuantities { get; }
    }
}
