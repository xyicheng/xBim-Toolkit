using System;
namespace Xbim.DOM
{
    /// <summary>
    /// Interface for document to be used to creation of the BIM objects.
    /// This interface is intended for usage in conversion between diferent 
    /// BIM platform with focus on IFC structure. Object created by this 
    /// functions are supposed to be created in the document represented by 
    /// this interface.
    /// </summary>
    public interface IBimTarget
    {
        #region spatial structure creation
        /// <summary>
        /// Creates object representing site
        /// </summary>
        /// <param name="name">name of the site</param>
        /// <param name="parentElement">parent spatial structure element (another site)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New site object</returns>
        IBimSpatialStructureElement NewSite(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum);

        /// <summary>
        /// Creates object representing building
        /// </summary>
        /// <param name="name">name of building</param>
        /// <param name="parentElement">parent spatial structure element (another building or site)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New building object</returns>
        IBimSpatialStructureElement NewBuilding(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum);

        /// <summary>
        /// Creates object representing space
        /// </summary>
        /// <param name="name">name of the space</param>
        /// <param name="parentElement">parent spatial structure element (storey)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>new space object</returns>
        IBimSpatialStructureElement NewSpace(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum);

        /// <summary>
        /// Creates object representing storey
        /// </summary>
        /// <param name="name">name of the storey</param>
        /// <param name="parentElement">parent spatial structure element (building)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New storey object</returns>
        IBimSpatialStructureElement NewStorey(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum);
        #endregion

        #region geometry creation
        /// <summary>
        /// Creation of the extruded solid with rectangle profile.
        /// It is not possible to add external boundary then, but you can
        /// define internal boundary using functions of the SweptGeometry.
        /// </summary>
        /// <param name="depth">Depth of the extrusion</param>
        /// <param name="width">Width of the rectangle profile</param>
        /// <param name="length">Lenght of the rectangle profile</param>
        /// <param name="direction">Direction of the extrusion</param>
        /// <returns>Extruded solid geometry with rectangle profile</returns>
        IBimSweptSolid NewExtrudedAreaSolid(double depth, double width, double length, XbimXYZ direction);

        /// <summary>
        /// Creation of the extruded solid with general profile.
        /// Profile must be set on the existing object using its functions.
        /// </summary>
        /// <param name="depth">Depth of the extrusion</param>
        /// <param name="direction">Direction of the extrusion</param>
        /// <returns>Extruded solid geometry with general profile</returns>
        IBimSweptSolid NewExtrudedAreaSolid(double depth, XbimXYZ direction);

        /// <summary>
        /// Creation of the revolved solid with general profile.
        /// Profile must be set on the existing object using its functions.
        /// </summary>
        /// <param name="angle">Angle of revolution</param>
        /// <param name="spindleDirection">Spindle direction (rotation axis direction)</param>
        /// <param name="spindleLocation">Spindle location (rotation axis location)</param>
        /// <returns>Revolved solid with general profile</returns>
        IBimSweptSolid NewRevolvedAreaSolid(double angle, XbimXYZ spindleDirection, XbimXYZ spindleLocation);

        /// <summary>
        /// Creates empty faceted B-Rep. You must use its functions to define geometry
        /// (AddBoundaryPoint(), AddInnerLoop(), ...)
        /// </summary>
        /// <returns>Faceted boundary representation</returns>
        IBimFacetedBrep NewFacetedBrep();

        /// <summary>
        /// Creates local placement object in specified location. 
        /// You have to use its functions to define direction of X and Z axis.
        /// (Y axis direction is considered to be supplement to the 
        /// right oriented cartessian coordinate system)
        /// </summary>
        /// <param name="locationX">X location</param>
        /// <param name="locationY">Y location</param>
        /// <param name="locationZ">Z location</param>
        /// <returns>Local placement object</returns>
        IBimLocalPlacement NewLocalPlacement(double locationX, double locationY, double locationZ);

        /// <summary>
        /// Creates local placement object in specified location. 
        /// You have to use its functions to define relative placement etc.
        /// </summary>
        /// <param name="relPlacement">Placement and orientation</param>
        /// <returns>Local placement object</returns>
        IBimLocalPlacement NewLocalPlacement(IBimAxis2Placement3D relPlacement);

        /// <summary>
        /// Creates new axis to placement 3D object useful for handling orientation and placement of
        /// objects (it is internaly part of the local placement). You have to use its functions to 
        /// set directions of the axes.
        /// </summary>
        /// <param name="locationX">X location</param>
        /// <param name="locationY">Y location</param>
        /// <param name="locationZ">Z location</param>
        /// <returns>Axis to placement 3D object</returns>
        IBimAxis2Placement3D NewAxis2Placement3D(double locationX, double locationY, double locationZ);
        #endregion


        #region material creation
        /// <summary>
        /// Creates new material object of the specified name
        /// </summary>
        /// <param name="name">name of the material</param>
        /// <returns>new material object</returns>
        IBimMaterial NewMaterial(string name);

        /// <summary>
        /// Creates new material object of the specified name and use
        /// the lookUp material as template if available
        /// </summary>
        /// <param name="name">name of the material</param>
        /// <param name="lookUpName">name of the luuk up material</param>
        /// <returns>new material object</returns>
        IBimMaterial NewMaterial(string name, string lookUpName);
        #endregion

        #region building elements and types creation
        IBimBuildingElement NewBeam(IBimBuildingElementType beamType);
        IBimBuildingElementType NewBeamType(string name, string description, XbimBeamTypeEnum beamType);

        IBimBuildingElement NewBuildingElementProxy();
        IBimBuildingElement NewBuildingElementProxy(IBimBuildingElementType buildingElementProxyType);
        IBimBuildingElement NewBuildingElementProxy(IBimBuildingElementType buildingElementProxyType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElementType NewBuildingElementProxyType(string name, string description);

        IBimBuildingElement NewCeiling(IBimBuildingElementType ceilingType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewCeiling(IBimBuildingElementType ceilingType);
        IBimBuildingElementType NewCeilingType(string name, string description);

        IBimBuildingElement NewColumn(IBimBuildingElementType columnType);
        IBimBuildingElementType NewColumnType(string name, string description, XbimColumnTypeEnum type);

        IBimBuildingElement NewCurtainWall(IBimBuildingElementType curtainWallType);
        IBimBuildingElementType NewCurtainWallType(string name, string description);

        IBimBuildingElement NewDoor(IBimBuildingElementType doorType);
        IBimBuildingElementType NewDoorType(string name, string description, XbimDoorStyleConstructionEnum construction, XbimDoorStyleOperationEnum operation);

        IBimBuildingElement NewFloor(IBimBuildingElementType floorType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewFloor(IBimBuildingElementType floorType);
        IBimBuildingElementType NewFloorType(string name, string description);
        
        IBimBuildingElement NewPlate(IBimBuildingElementType plateType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewPlate(IBimBuildingElementType plateType);
        IBimBuildingElementType NewPlateType(string name, string description, XbimPlateTypeEnum plateType);

        IBimBuildingElement NewRailing(IBimBuildingElementType type);
        IBimBuildingElementType NewRailingType(string name, string description, XbimRailingTypeEnum type);

        IBimBuildingElement NewRampFlight(IBimBuildingElementType type);
        IBimBuildingElementType NewRampFlightType(string name, string description, XbimRampFlightTypeEnum type);

        IBimBuildingElement NewRoof(IBimBuildingElementType roofType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewRoof(IBimBuildingElementType roofType);
        IBimBuildingElementType NewRoofType(string name, string description);

        IBimBuildingElement NewSlab(IBimBuildingElementType SlabType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewSlab(IBimBuildingElementType SlabType);
        IBimBuildingElementType NewSlabType(string name, string description, XbimSlabTypeEnum predefinedType);

        IBimBuildingElement NewStairFlight(IBimBuildingElementType type);
        IBimBuildingElementType NewStairFlightType(string name, string description, XbimStairFlightTypeEnum stairType);

        IBimBuildingElement NewWall(IBimBuildingElementType wallType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett);
        IBimBuildingElement NewWall(IBimBuildingElementType wallType);
        IBimBuildingElementType NewWallType(string name, string description, XbimWallTypeEnum predefinedType);

        IBimBuildingElement NewWindow(IBimBuildingElementType type);
        IBimBuildingElementType NewWindowType(string name, string description, XbimWindowStyleConstructionEnum construction, XbimWindowStyleOperationEnum operation);
        #endregion

        #region  getter functions to get existing object from the model
        IBimBuildingElement GetBuildingElement(Guid guid);
        IBimBuildingElementType GetBuildingElementType(Guid guid);
        IBimBuildingElementType GetBuildingElementType(string name);
        IBimMaterial GetMaterial(string name);
        IBimSpatialStructureElement GetSpatialStructureElement(string name);
        IBimSpatialStructureElement GetSpatialStructureElement(Guid guid);
        IBimSpatialStructureElement GetDefaultBuilding();
        #endregion
    }
}
