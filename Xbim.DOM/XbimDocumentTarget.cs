using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.DOM
{
    public class XbimDocumentTarget:IBimTarget
    {
        #region Constructor and private fields
        private XbimDocument _document;
        private XbimObjectCreator Create { get { return _document.Create; } }

        public XbimDocumentTarget(XbimDocument document)
        {
            _document = document;
        }
        #endregion

        public XbimDocument Document { get { return _document; } }

        #region spatial structure elements

        /// <summary>
        /// Creates object representing site
        /// </summary>
        /// <param name="name">name of the site</param>
        /// <param name="parentElement">parent spatial structure element (another site)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New site object</returns>
        public IBimSpatialStructureElement NewSite(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            XbimSpatialStructureElement spatialStruct = parentElement as XbimSpatialStructureElement;
            //it is possible that spatialStruct (parentElement) is null as site could be on the highest level of the hierarchy

            return Create.Site(name, spatialStruct, compositionEnum);
        }

        /// <summary>
        /// Creates object representing building
        /// </summary>
        /// <param name="name">name of building</param>
        /// <param name="parentElement">parent spatial structure element (another building or site)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New building object</returns>
        public IBimSpatialStructureElement NewBuilding(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            XbimSpatialStructureElement spatialStruct = parentElement as XbimSpatialStructureElement;
            if (spatialStruct == null) throw new ArgumentException();

            return Create.Building(name, spatialStruct, compositionEnum);
        }

        /// <summary>
        /// Creates object representing space
        /// </summary>
        /// <param name="name">name of the space</param>
        /// <param name="parentElement">parent spatial structure element (storey)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>new space object</returns>
        public IBimSpatialStructureElement NewSpace(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            XbimSpatialStructureElement spatialStruct = parentElement as XbimSpatialStructureElement;
            if (spatialStruct == null) throw new ArgumentException();

            return Create.Space(name, spatialStruct, compositionEnum);
        }

        /// <summary>
        /// Creates object representing storey
        /// </summary>
        /// <param name="name">name of the storey</param>
        /// <param name="parentElement">parent spatial structure element (building)</param>
        /// <param name="compositionEnum">type of composition</param>
        /// <returns>New storey object</returns>
        public IBimSpatialStructureElement NewStorey(string name, IBimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            XbimSpatialStructureElement spatialStruct = parentElement as XbimSpatialStructureElement;
            if (spatialStruct == null) throw new ArgumentException();

            return Create.Storey(name, spatialStruct, compositionEnum);
        }
        #endregion

        #region creation of the geometry
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
        public IBimSweptSolid NewExtrudedAreaSolid(double depth, double width, double length, XbimXYZ direction)
        {
            return Create.ExtrudedAreaSolid(depth, width, length, direction);
        }

        /// <summary>
        /// Creation of the extruded solid with general profile.
        /// Profile must be set on the existing object using its functions.
        /// </summary>
        /// <param name="depth">Depth of the extrusion</param>
        /// <param name="direction">Direction of the extrusion</param>
        /// <returns>Extruded solid geometry with general profile</returns>
        public IBimSweptSolid NewExtrudedAreaSolid(double depth, XbimXYZ direction)
        {
            return Create.ExtrudedAreaSolid(depth, direction);
        }

        /// <summary>
        /// Creation of the revolved solid with general profile.
        /// Profile must be set on the existing object using its functions.
        /// </summary>
        /// <param name="angle">Angle of revolution</param>
        /// <param name="spindleDirection">Spindle direction (rotation axis direction)</param>
        /// <param name="spindleLocation">Spindle location (rotation axis location)</param>
        /// <returns>Revolved solid with general profile</returns>
        public IBimSweptSolid NewRevolvedAreaSolid(double angle, XbimXYZ spindleDirection, XbimXYZ spindleLocation)
        {
            return Create.RevolvedAreaSolid(angle, spindleDirection, spindleLocation);
        }

        /// <summary>
        /// Creates empty faceted B-Rep. You must use its functions to define geometry
        /// (AddBoundaryPoint(), AddInnerLoop(), ...)
        /// </summary>
        /// <returns>Faceted boundary representation</returns>
        public IBimFacetedBrep NewFacetedBrep()
        {
            return Create.FacetedBrep();
        }

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
        public IBimLocalPlacement NewLocalPlacement(double locationX, double locationY, double locationZ)
        {
            return Create.LocalPlacement(locationX, locationY, locationZ);
        }

        /// <summary>
        /// Creates local placement object in specified location. 
        /// You have to use its functions to define relative placement etc.
        /// </summary>
        /// <param name="relPlacement">Placement and orientation</param>
        /// <returns>Local placement object</returns>
        public IBimLocalPlacement NewLocalPlacement(IBimAxis2Placement3D relPlacement)
        {
            XbimAxis2Placement3D placement = relPlacement as XbimAxis2Placement3D;
            if (placement == null) throw new ArgumentException();
            return Create.LocalPlacement(placement);
        }

        public IBimAxis2Placement3D NewAxis2Placement3D(double locationX, double locationY, double locationZ)
        {
            IBimAxis2Placement3D result = new XbimAxis2Placement3D(_document);
            result.SetLocation(locationX, locationY, locationZ);
            return result;

        }
        #endregion

        #region material
        /// <summary>
        /// Creates new material object of the specified name
        /// </summary>
        /// <param name="name">name of the material</param>
        /// <returns>new material object</returns>
        public IBimMaterial NewMaterial(string name)
        {
            return Create.Material(name);
        }

        public IBimMaterial NewMaterial(string name, string lookUpMaterial)
        {
            return Create.Material(name);
        }
        #endregion

        #region building elements and building element types
        public IBimBuildingElement NewBeam(IBimBuildingElementType beamType)
        {
            XbimBeamType type = GetType<XbimBeamType>(beamType);
            return Create.Beam(type);
        }

        public IBimBuildingElementType NewBeamType(string name, string description, XbimBeamTypeEnum beamType)
        {
            return Create.BeamType(name, description, beamType);
        }

        public IBimBuildingElement NewBuildingElementProxy()
        {
            return Create.BuildingElementProxy();
        }

        public IBimBuildingElement NewBuildingElementProxy(IBimBuildingElementType buildingElementProxyType)
        {
            XbimBuildingElementProxyType type = GetType<XbimBuildingElementProxyType>(buildingElementProxyType);
            return Create.BuildingElementProxy(type);
        }

        public IBimBuildingElement NewBuildingElementProxy(IBimBuildingElementType buildingElementProxyType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            throw new NotImplementedException();
        }

        public IBimBuildingElementType NewBuildingElementProxyType(string name, string description)
        {
            IBimBuildingElementType result = Create.BuildingElementProxyType(name);
            result.Description = description;
            return result;
        }

        public IBimBuildingElement NewCeiling(IBimBuildingElementType ceilingType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimCoveringType type = GetType<XbimCoveringType>(ceilingType);
            return Create.Covering(type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }

        public IBimBuildingElement NewCeiling(IBimBuildingElementType ceilingType)
        {
            XbimCoveringType type = GetType<XbimCoveringType>(ceilingType);
            return Create.Covering(type);
        }

        public IBimBuildingElementType NewCeilingType(string name, string description)
        {
            return Create.CoveringType(name, description, XbimCoveringTypeEnum.CEILING);
        }

        public IBimBuildingElement NewColumn(IBimBuildingElementType columnType)
        {
            XbimColumnType type = GetType<XbimColumnType>(columnType);
            return Create.Column(type);
        }

        public IBimBuildingElementType NewColumnType(string name, string description, XbimColumnTypeEnum type)
        {
            return Create.ColumnType(name, description, type);
        }

        public IBimBuildingElement NewCurtainWall(IBimBuildingElementType curtainWallType)
        {
            XbimCurtainWallType type = GetType<XbimCurtainWallType>(curtainWallType);
            return Create.CurtainWall(type);
        }

        public IBimBuildingElementType NewCurtainWallType(string name, string description)
        {
            IBimBuildingElementType result = Create.CurtainWallType(name);
            result.Description = description;
            return result;
        }

        public IBimBuildingElement NewDoor(IBimBuildingElementType doorType)
        {
            XbimDoorStyle type = GetType<XbimDoorStyle>(doorType);
            return Create.Door(type);
        }

        public IBimBuildingElementType NewDoorType(string name, string description, XbimDoorStyleConstructionEnum construction, XbimDoorStyleOperationEnum operation)
        {
            return Create.DoorStyle(name, description, construction, operation);
        }

        public IBimBuildingElement NewFloor(IBimBuildingElementType floorType)
        {
            XbimSlabType type = GetType<XbimSlabType>(floorType);
            XbimSlab slab = Create.Slab(type);
            (slab.AsRoot as Ifc2x3.SharedBldgElements.IfcSlab).PredefinedType = Ifc2x3.SharedBldgElements.IfcSlabTypeEnum.FLOOR;
            return slab;
        }

        public IBimBuildingElement NewFloor(IBimBuildingElementType floorType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimSlabType type = GetType<XbimSlabType>(floorType);
            XbimSlab slab = Create.Slab(type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
            (slab.AsRoot as Ifc2x3.SharedBldgElements.IfcSlab).PredefinedType = Ifc2x3.SharedBldgElements.IfcSlabTypeEnum.FLOOR;
            return slab;
        }

        public IBimBuildingElementType NewFloorType(string name, string description)
        {
            return Create.SlabType(name, description, XbimSlabTypeEnum.FLOOR);
        }

        public IBimBuildingElement NewPlate(IBimBuildingElementType plateType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimPlateType type = GetType<XbimPlateType>(plateType);
            return Create.Plate(type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }

        public IBimBuildingElement NewPlate(IBimBuildingElementType plateType)
        {
            XbimPlateType type = GetType<XbimPlateType>(plateType);
            return Create.Plate(type);
        }

        public IBimBuildingElementType NewPlateType(string name, string description, XbimPlateTypeEnum plateType)
        {
            return Create.PlateType(name, description, plateType);
        }

        public IBimBuildingElement NewRailing(IBimBuildingElementType railingType)
        {
            XbimRailingType type = GetType<XbimRailingType>(railingType);
            return Create.Railing(type);
        }

        public IBimBuildingElementType NewRailingType(string name, string description, XbimRailingTypeEnum type)
        {
            return Create.RailingType(name, description, type);
        }

        public IBimBuildingElement NewRampFlight(IBimBuildingElementType flightType)
        {
            XbimRampFlightType type = GetType<XbimRampFlightType>(flightType);
            return Create.RampFlight(type);
        }

        public IBimBuildingElementType NewRampFlightType(string name, string description, XbimRampFlightTypeEnum type)
        {
            return Create.RampFlightType(name, description, type);
        }

        public IBimBuildingElement NewRoof(IBimBuildingElementType roofType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimSlabType slabType = GetType<XbimSlabType>(roofType);
            XbimSlab slab = Create.Slab(slabType, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
            (slab.AsRoot as Ifc2x3.SharedBldgElements.IfcSlab).PredefinedType = Ifc2x3.SharedBldgElements.IfcSlabTypeEnum.ROOF;
            return slab;
        }

        public IBimBuildingElement NewRoof(IBimBuildingElementType roofType)
        {
            XbimSlabType slabType = GetType<XbimSlabType>(roofType);
            XbimSlab slab = Create.Slab(slabType);
            (slab.AsRoot as Ifc2x3.SharedBldgElements.IfcSlab).PredefinedType = Ifc2x3.SharedBldgElements.IfcSlabTypeEnum.ROOF;
            return slab;
        }

        public IBimBuildingElementType NewRoofType(string name, string description)
        {
            return Create.SlabType(name, description, XbimSlabTypeEnum.ROOF);
        }

        public IBimBuildingElement NewSlab(IBimBuildingElementType slabType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimSlabType type = GetType<XbimSlabType>(slabType);
            return Create.Slab(type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }

        public IBimBuildingElement NewSlab(IBimBuildingElementType SlabType)
        {
            XbimSlabType type = GetType<XbimSlabType>(SlabType);
            return Create.Slab(type);
        }

        public IBimBuildingElementType NewSlabType(string name, string description, XbimSlabTypeEnum predefinedType)
        {
            return Create.SlabType(name, description, predefinedType);
        }

        public IBimBuildingElement NewStairFlight(IBimBuildingElementType type)
        {
            XbimStairFlightType stType = GetType<XbimStairFlightType>(type);
            return Create.StairFlight(stType);
        }

        public IBimBuildingElementType NewStairFlightType(string name, string description, XbimStairFlightTypeEnum stairType)
        {
            return Create.StairFlightType(name, description, stairType);
        }

        public IBimBuildingElement NewWall(IBimBuildingElementType wallType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            XbimWallType type = GetType<XbimWallType>(wallType);
            return Create.Wall(type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }

        public IBimBuildingElement NewWall(IBimBuildingElementType wallType)
        {
            XbimWallType type = GetType<XbimWallType>(wallType);
            return Create.Wall(type);
        }

        public IBimBuildingElementType NewWallType(string name, string description, XbimWallTypeEnum predefinedType)
        {
            return Create.WallType(name, description, predefinedType);
        }

        public IBimBuildingElement NewWindow(IBimBuildingElementType winType)
        {
            XbimWindowStyle type = GetType<XbimWindowStyle>(winType);
            return Create.Window(type);
        }

        public IBimBuildingElementType NewWindowType(string name, string description, XbimWindowStyleConstructionEnum construction, XbimWindowStyleOperationEnum operation)
        {
            return Create.WindowStyle(name, description, construction, operation);
        }

        #endregion

        #region Getting objects functions
        public IBimBuildingElement GetBuildingElement(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return _document.AllBuildingElements.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public IBimBuildingElementType GetBuildingElementType(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return _document.AllBuildingElementTypes.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public IBimBuildingElementType GetBuildingElementType(string name)
        {
            return _document.AllBuildingElementTypes.Where(e => e.Name == name).FirstOrDefault();
        }


        public IBimMaterial GetMaterial(string name)
        {
            if (_document.Materials.Contains(name)) return _document.Materials[name];
            return null;
        }

        public IBimSpatialStructureElement GetSpatialStructureElement(string name)
        {
            return _document.AllSpatialStructureElements.Where(e => e.Name == name).FirstOrDefault();
        }

        public IBimSpatialStructureElement GetSpatialStructureElement(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return _document.AllSpatialStructureElements.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public IBimSpatialStructureElement GetDefaultBuilding()
        {
            return _document.Buildings.FirstOrDefault() ;
        }
        #endregion

        #region helpers
        private T GetType<T>(object obj) where T : XbimBuildingElementType
        {
            T type = obj as T;
            return type;
        }
        #endregion
    }
}
