using System;
namespace Xbim.DOM
{
    public interface IBimSpatialStructureElement
    {
        bool AddContainedBuildingElement(Guid guid);
        void AddContainedBuildingElement(IBimBuildingElement element);
        void AddGeometrySweptSolid(IBimSweptSolid Geometry);
        void AddToSpatialDecomposition(IBimSpatialStructureElement child);
        object GlobalId { get; set; }
        string LongName { get; set; }
        string Name { get; set; }
        void SetLocalPlacement(IBimLocalPlacement XbimLocalPlacement);
        IBimSingleProperties SingleProperties { get; }
        void AddBoundingElementToSpace(IBimSpatialStructureElement space, IBimBuildingElement element, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external);
        void AddBoundingElementToSpace(IBimSpatialStructureElement space, Guid elementGuid, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external);
    }
}
