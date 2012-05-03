using System;
namespace Xbim.DOM
{
    public interface IBimBuildingElementType
    {
        void AddMaterialLayer(IBimMaterial material, double thickness);
        void AddMaterialLayer(IBimMaterial material, double thickness, bool isVentilated);
        void AddMaterialLayer(IBimMaterial material, double thickness, bool isVentilated, XbimMaterialFunctionEnum function);
        string Description { get; set; }
        Guid GlobalId { get; set; }
        string Name { get; set; }
        IBimSingleProperties Properties { get; }
        //INRMQuantities NRMQuantities { get; }
    }
}
