using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;

namespace Xbim.DOM
{
    public class XbimObjectCreator
    {
        private XbimDocument _document;
        internal XbimObjectCreator(XbimDocument doc)
        {
            _document = doc;
        }

        #region WallType
        public XbimWallType WallType(string name)
        {
            if (_document.WallTypes.Contains(name))
            {
                throw new Exception(string.Format("WallType {0} already exists and cannot be duplicated", name));
            }
            return new XbimWallType(_document, name);
        }

        public XbimWallType WallType(string name, string description, XbimWallTypeEnum predefinedType)
        {
            if (_document.WallTypes.Contains(name))
            {
                throw new Exception(string.Format("WallType {0} already exists and cannot be duplicated", name));
            }
            return new XbimWallType(_document, name, description, predefinedType);
        }

        /// <summary>
        /// Return existing wall type. If it does not exist it is created.
        /// </summary>
        /// <param name="name">Name if the wall type</param>
        /// <param name="description">Description of the wall type</param>
        /// <param name="predefinedType">Predefined type of the wall type</param>
        /// <returns>Wall type</returns>
        public XbimWallType WallTypeGetOrCreate(string name, string description, XbimWallTypeEnum predefinedType)
        {
            if (_document.WallTypes.Contains(name))
            {
                return _document.WallTypes[name] as XbimWallType;
            }
            return new XbimWallType(_document, name, description, predefinedType);
        }
        #endregion

        #region Wall
        /// <summary>
        /// Creates wall with default parameters of material usage:
        /// MaterialLayersDirection = AXIS1
        /// MaterialLayersDirectionSense = POSITIVE
        /// MaterialLayersOffsett = 0
        /// </summary>
        /// <param name="xbimWallType">Wall type to create the wall</param>
        /// <returns></returns>
        public XbimWall Wall(XbimWallType xbimWallType)
        {
            return new XbimWall(_document, xbimWallType);
        }

        /// <summary>
        /// Creates wall with specified parameters of material usage
        /// </summary>
        /// <param name="xbimWallType">Wall type to create the wall</param>
        /// <param name="MaterialLayersDirection">Direction of the layers</param>
        /// <param name="MaterialLayersDirectionSense">Sense of the direction</param>
        /// <param name="MaterialLayersOffsett">Offset of the layers from the definition line of the wall</param>
        /// <returns></returns>
        public XbimWall Wall(XbimWallType xbimWallType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            return new XbimWall(_document, xbimWallType, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }
        #endregion

        #region Material
        /// <summary>
        /// This will create a new material, if a material of that name already exists an exception will be thrown
        /// Use the XbimDocument.Materials.Contains method to inspect existance
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public XbimMaterial Material(string name)
        {
            if (_document.Materials.Contains(name))
                throw new Exception(string.Format("Material {0} already exists and cannot be duplicated", name));
            else
            {
               return  new XbimMaterial(_document, name);
            }
        }

        /// <summary>
        /// Returns existing material of the specified name. If material does not exist it is created.
        /// </summary>
        /// <param name="name">Name of the material</param>
        /// <returns>Material of the specified name</returns>
        public XbimMaterial MaterialGetOrCreate(string name)
        {
            if (_document.Materials.Contains(name))
                return _document.Materials[name];
            else
            {
                return new XbimMaterial(_document, name);
            }
        }
        #endregion

        #region Classification
        /// <summary>
        /// Adds a Classification Item to the document and then adds it as one of the root items in the classification system
        /// </summary>
        /// <param name="Notation"></param>
        /// <param name="title"></param>
        /// <param name="classificationSystem"></param>
        /// <returns></returns>
        public XbimClassificationItem ClassificationItemAsRoot(string notation, string title, XbimClassification classificationSystem)
        {
            XbimClassificationItem item = new XbimClassificationItem(_document,classificationSystem, notation, title);
            classificationSystem.AddRootItem(item);
            return item;
        }

        /// <summary>
        /// Create a classification item and adds it as a child of the parent
        /// </summary>
        /// <param name="Notation"></param>
        /// <param name="title"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        public XbimClassificationItem ClassificationItem(string notation, string title, XbimClassification system, XbimClassificationItem parent)
        {
            XbimClassificationItem item = new XbimClassificationItem(_document, system, notation, title);
            parent.AddChildItem(item);
            return item;
        }

        public XbimClassification ClassificationSystem(string publisherId, string name, string edition, DateTime? date)
        {
            return new XbimClassification(_document, publisherId, name, edition, date);
        }

        public XbimClassification ClassificationSystem(string publisherId, string name, string edition)
        {
            return ClassificationSystem(publisherId, name, edition, null);
        }
        #endregion

        #region FacetedBrep
        /// <summary>
        /// Create new instance of faceted Brep ready for imput of the data using "AddFace()" and "AddPoint()" functions.
        /// </summary>
        /// <returns>New faceted Brep object</returns>
        public XbimFacetedBrep FacetedBrep()
        {
            return new XbimFacetedBrep(_document);
        }

        /// <summary>
        /// Create new instance of faceted Brep ready for imput of the data using "AddFace()" and "AddPoint()" functions.
        /// </summary>
        /// <returns>New faceted Brep object</returns>
        public XbimFacetedBrepTopo<VertexRef, EdgeRef, FaceRef> FacetedBrepTopo<VertexRef, EdgeRef, FaceRef>()
        {
            return new XbimFacetedBrepTopo<VertexRef, EdgeRef, FaceRef>(_document);
        }
        #endregion

        #region Placement and geometry
        /// <summary>
        /// Create a new local placement object with not specified object which placement it defines.
        /// </summary>
        /// <param name="locationX">X coordinate of the location</param>
        /// <param name="locationY">Y coordinate of the location</param>
        /// <param name="locationZ">Z coordinate of the location</param>
        /// <returns>New local placement object</returns>
        public XbimLocalPlacement LocalPlacement(double locationX, double locationY, double locationZ)
        {
            return new XbimLocalPlacement(_document, locationX, locationY, locationZ);
        }

        /// <summary>
        /// Create a new local placement object.
        /// </summary>
        /// <param name="relPlacement">Relative placement</param>
        /// <returns></returns>
        public XbimLocalPlacement LocalPlacement(XbimAxis2Placement3D relPlacement)
        {
            return new XbimLocalPlacement(_document, relPlacement);
        }

        #endregion

        #region ExtrudedAreaSolid
        public XbimExtrudedAreaSolid ExtrudedAreaSolid(double depth, XbimXYZ direction)
        {
            return new XbimExtrudedAreaSolid(_document, depth, direction);
        }

        public XbimExtrudedAreaSolid ExtrudedAreaSolid(double depth, double width, double length, XbimXYZ direction)
        {
            return new XbimExtrudedAreaSolid(_document, depth, width, length, direction);
        }

        #endregion

        #region RevolvedAreaSolid
        public XbimRevolvedAreaSolid RevolvedAreaSolid(double angle, XbimXYZ spindleDirection, XbimXYZ spindleLocation)
        {
            return new XbimRevolvedAreaSolid(_document, angle, spindleDirection, spindleLocation);
        }
        #endregion

        #region Spatial structure elements
        /// <summary>
        /// Creates new site.
        /// </summary>
        /// <param name="name">Name of the site</param>
        /// <param name="parentElement">Parent of the site could be another site. Set NULL to set the project to be the parrent.</param>
        /// <param name="compositionEnum">Composition type enumeration</param>
        /// <returns>New Xbim site object</returns>
        public XbimSite Site(string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            return new XbimSite(_document, name, parentElement, compositionEnum);
        }
        /// <summary>
        /// Creates new building object
        /// </summary>
        /// <param name="name">Name of the building</param>
        /// <param name="parentElement">Parent element of the building could be another building or site</param>
        /// <param name="compositionEnum">Composition type enumeration - it should correspond with the type of the parent element</param>
        /// <returns>New building object</returns>
        public XbimBuilding Building(string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            return new XbimBuilding(_document, name, parentElement, compositionEnum);
        }
        /// <summary>
        /// Creates new building storey object
        /// </summary>
        /// <param name="name">Name of the building storey</param>
        /// <param name="parentElement">Parent element of the building storey could be complex storey or building</param>
        /// <param name="compositionEnum">Composition type enumeration - it should correspond with the type of the parent element</param>
        /// <returns>New building storey object</returns>
        public XbimBuildingStorey Storey(string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            return new XbimBuildingStorey(_document, name, parentElement, compositionEnum);
        }

        /// <summary>
        /// Creates new space object
        /// </summary>
        /// <param name="name">Name of the space</param>
        /// <param name="parentElement">Parent element of the space could be complex space or building storey</param>
        /// <param name="compositionEnum">Composition type enumeration - it should correspond with the type of the parent element</param>
        /// <param name="interExterEnum">Internal of external type of the space</param>
        /// <returns>New space object</returns>
        public XbimSpace Space(string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
        {
            return new XbimSpace(_document, name, parentElement, compositionEnum);
        }
        #endregion

        #region Roof
        /// <summary>
        /// Creates new roof object with specified sense and direction of the material layers and roof type
        /// </summary>
        /// <param name="MaterialLayersDirection">direction of the material layers</param>
        /// <param name="MaterialLayersDirectionSense">sense of the material layers</param>
        /// <param name="MaterialLayersOffsett">offset of the material layers</param>
        /// <param name="roofType">type of the room</param>
        /// <returns>new roof object</returns>
        //public XbimRoof Roof(XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        //{
        //    return new XbimRoof(_document, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        //}

        /// <summary>
        /// Creates new roof object. It can has layers and geometry OR it can be decomposed by slabs. 
        /// It is not possible co combine these possibilities (exception could be thrown in that case)
        /// </summary>
        /// <returns></returns>
        public XbimRoof Roof(XbimRoofTypeEnum roofType)
        {
            return new XbimRoof(_document, roofType);
        }
        #endregion

        #region Slab
        /// <summary>
        /// Creates new slab object with specified sense and direction of the material layers and slab type
        /// </summary>
        /// <param name="xbimSlabType">slab type</param>
        /// <param name="MaterialLayersDirection">direction of the material layers</param>
        /// <param name="MaterialLayersDirectionSense">sense of the material layers</param>
        /// <param name="MaterialLayersOffsett">offset of the material layers</param>
        /// <returns>new slab object</returns>
        public XbimSlab Slab(XbimSlabType xbimSlabType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            return new XbimSlab(_document, xbimSlabType, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }

        /// <summary>
        /// Creates new slab object with default sense and direction of the material layers (0, positive sense)
        /// </summary>
        /// <param name="xbimSlabType">slab type</param>
        /// <returns></returns>
        public XbimSlab Slab(XbimSlabType xbimSlabType)
        {
            return new XbimSlab(_document, xbimSlabType);
        }
        #endregion

        #region SlabType
        public XbimSlabType SlabType(string name)
        {
            if (_document.SlabTypes.Contains(name))
            {
                throw new Exception(string.Format("SlabType {0} already exists and cannot be duplicated", name));
            }
            return new XbimSlabType(_document, name);
        }

        public XbimSlabType SlabType(string name, string description, XbimSlabTypeEnum predefinedType)
        {
            if (_document.SlabTypes.Contains(name))
            {
                throw new Exception(string.Format("SlabType {0} already exists and cannot be duplicated", name));
            }
            return new XbimSlabType(_document, name, description, predefinedType);
        }

        public XbimSlabType SlabTypeGetOrCreate(string name, string description, XbimSlabTypeEnum predefinedType)
        {
            if (_document.SlabTypes.Contains(name))
            {
                return _document.SlabTypes[name] as XbimSlabType;
            }
            return new XbimSlabType(_document, name, description, predefinedType);
        }
        #endregion

        #region Plate
        public XbimPlate Plate(XbimPlateType xbimPlateType, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            return new XbimPlate(_document, xbimPlateType, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }
        public XbimPlate Plate(XbimPlateType xbimPlateType)
        {
            return new XbimPlate(_document, xbimPlateType);
        }
        #endregion

        #region PlateType
        public XbimPlateType PlateType(string name)
        {
            if (_document.PlateTypes.Contains(name))
            {
                throw new Exception(string.Format("PlateType {0} already exists and cannot be duplicated", name));
            }
            return new XbimPlateType(_document, name);
        }

        public XbimPlateType PlateType(string name, string description, XbimPlateTypeEnum plateType)
        {
            if (_document.PlateTypes.Contains(name))
            {
                throw new Exception(string.Format("PlateType {0} already exists and cannot be duplicated", name));
            }
            return new XbimPlateType(_document, name, description, plateType);
        }

        public XbimPlateType PlateTypeGetOrCreate(string name, string description, XbimPlateTypeEnum plateType)
        {
            return _document.PlateTypes.Contains(name) ? _document.PlateTypes[name]: new XbimPlateType(_document, name, description, plateType);
        }
        #endregion

        #region Covering
        public XbimCovering Covering(XbimCoveringType type, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            return new XbimCovering(_document, type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }
        public XbimCovering Covering(XbimCoveringType type)
        {
            return new XbimCovering(_document, type);
        }
        #endregion

        #region CoveringType
        public XbimCoveringType CoveringType(string name)
        {
            if (_document.PlateTypes.Contains(name))
            {
                throw new Exception(string.Format("PlateType {0} already exists and cannot be duplicated", name));
            }
            return new XbimCoveringType(_document, name);
        }

        public XbimCoveringType CoveringType(string name, string description, XbimCoveringTypeEnum type)
        {
            if (_document.PlateTypes.Contains(name))
            {
                throw new Exception(string.Format("PlateType {0} already exists and cannot be duplicated", name));
            }
            return new XbimCoveringType(_document, name, description, type);
        }

        public XbimCoveringType CoveringTypeGetOrCreate(string name, string description, XbimCoveringTypeEnum type)
        {
            return _document.CoveringTypes.Contains(name) ? _document.CoveringTypes[name] : new XbimCoveringType(_document, name, description, type);
        }
        #endregion

        #region BeamType
        public XbimBeamType BeamType(string name)
        {
            if (_document.BeamTypes.Contains(name))
            {
                throw new Exception(string.Format("BeamType {0} already exists and cannot be duplicated", name));
            }
            return new XbimBeamType(_document, name);
        }

        public XbimBeamType BeamType(string name, string description, XbimBeamTypeEnum beamType)
        {
            if (_document.BeamTypes.Contains(name))
            {
                throw new Exception(string.Format("BeamType {0} already exists and cannot be duplicated", name));
            }
            return new XbimBeamType(_document, name, description, beamType);
        }

        public XbimBeamType BeamTypeGetOrCreate(string name, string description, XbimBeamTypeEnum plateType)
        {
            return _document.BeamTypes.Contains(name) ? _document.BeamTypes[name]: new XbimBeamType(_document, name, description, plateType);
        }
        #endregion

        #region StairFlightType
        public XbimStairFlightType StairFlightType(string name)
        {
            if (_document.StairFlightTypes.Contains(name))
            {
                throw new Exception(string.Format("StairFlightType {0} already exists and cannot be duplicated", name));
            }
            return new XbimStairFlightType(_document, name);
        }

        public XbimStairFlightType StairFlightType(string name, string description, XbimStairFlightTypeEnum plateType)
        {
            if (_document.StairFlightTypes.Contains(name))
            {
                throw new Exception(string.Format("StairFlightType {0} already exists and cannot be duplicated", name));
            }
            return new XbimStairFlightType(_document, name, description, plateType);
        }

        public XbimStairFlightType StairFlightTypeGetOrCreate(string name, string description, XbimStairFlightTypeEnum plateType)
        {
            return _document.StairFlightTypes.Contains(name) ? _document.StairFlightTypes[name] : new XbimStairFlightType(_document, name, description, plateType);
        }
        #endregion

        #region WindowStyle
        public XbimWindowStyle WindowStyle(string name)
        {
            if (_document.WindowStyles.Contains(name))
            {
                throw new Exception(string.Format("WindowStyle {0} already exists and cannot be duplicated", name));
            }
            return new XbimWindowStyle(_document, name);
        }

        public XbimWindowStyle WindowStyle(string name, string description, XbimWindowStyleConstructionEnum construction, XbimWindowStyleOperationEnum operation)
        {
            if (_document.WindowStyles.Contains(name))
            {
                throw new Exception(string.Format("WindowStyle {0} already exists and cannot be duplicated", name));
            }
            return new XbimWindowStyle(_document, name, description, construction, operation);
        }

        public XbimWindowStyle WindowStyleGetOrCreate(string name, string description, XbimWindowStyleConstructionEnum construction, XbimWindowStyleOperationEnum operation)
        {
            return _document.WindowStyles.Contains(name) ? _document.WindowStyles[name] : new XbimWindowStyle(_document, name, description, construction, operation);
        }
        #endregion

        #region DoorStyle
        public XbimDoorStyle DoorStyle(string name)
        {
            if (_document.DoorStyles.Contains(name))
            {
                throw new Exception(string.Format("DoorStyle {0} already exists and cannot be duplicated", name));
            }
            return new XbimDoorStyle(_document, name);
        }

        public XbimDoorStyle DoorStyle(string name, string description, XbimDoorStyleConstructionEnum construction, XbimDoorStyleOperationEnum operation)
        {
            if (_document.DoorStyles.Contains(name))
            {
                throw new Exception(string.Format("DoorStyle {0} already exists and cannot be duplicated", name));
            }
            return new XbimDoorStyle(_document, name, description, construction, operation);
        }

        public XbimDoorStyle DoorStyleGetOrCreate(string name, string description, XbimDoorStyleConstructionEnum construction, XbimDoorStyleOperationEnum operation)
        {
            return _document.DoorStyles.Contains(name) ? _document.DoorStyles[name] : new XbimDoorStyle(_document, name, description, construction, operation);
        }
        #endregion

        #region CurtainWallType
        public XbimCurtainWallType CurtainWallType(string name)
        {
            if (_document.CurtainWallTypes.Contains(name))
            {
                throw new Exception(string.Format("CurtainWallStyle {0} already exists and cannot be duplicated", name));
            }
            return new XbimCurtainWallType(_document, name);
        }

        public XbimCurtainWallType CurtainWallTypeGetOrCreate(string name)
        {
            return _document.CurtainWallTypes.Contains(name) ? _document.CurtainWallTypes[name] : new XbimCurtainWallType(_document, name);
        }
        #endregion

        #region ColumnType
        public XbimColumnType ColumnType(string name)
        {
            if (_document.ColumnTypes.Contains(name))
            {
                throw new Exception(string.Format("ColumnType {0} already exists and cannot be duplicated", name));
            }
            return new XbimColumnType(_document, name);
        }

        public XbimColumnType ColumnType(string name, string description, XbimColumnTypeEnum type)
        {
            if (_document.ColumnTypes.Contains(name))
            {
                throw new Exception(string.Format("ColumnType {0} already exists and cannot be duplicated", name));
            }
            return new XbimColumnType(_document, name, description, type);
        }

        public XbimColumnType ColumnTypeGetOrCreate(string name)
        {
            return _document.ColumnTypes.Contains(name) ? _document.ColumnTypes[name] : new XbimColumnType(_document, name);
        }
        #endregion

        #region RailingType
        public XbimRailingType RailingType(string name)
        {
            if (_document.RailingTypes.Contains(name))
            {
                throw new Exception(string.Format("RailingType {0} already exists and cannot be duplicated", name));
            }
            return new XbimRailingType(_document, name);
        }

        public XbimRailingType RailingType(string name, string description, XbimRailingTypeEnum type)
        {
            if (_document.RailingTypes.Contains(name))
            {
                throw new Exception(string.Format("RailingType {0} already exists and cannot be duplicated", name));
            }
            return new XbimRailingType(_document, name, description, type);
        }

        public XbimRailingType RailingTypeGetOrCreate(string name)
        {
            return _document.RailingTypes.Contains(name) ? _document.RailingTypes[name] : new XbimRailingType(_document, name);
        }
        #endregion

        #region RampFlightType
        public XbimRampFlightType RampFlightType(string name)
        {
            if (_document.RampFlightTypes.Contains(name))
            {
                throw new Exception(string.Format("RampFlightType {0} already exists and cannot be duplicated", name));
            }
            return new XbimRampFlightType(_document, name);
        }

        public XbimRampFlightType RampFlightType(string name, string description, XbimRampFlightTypeEnum type)
        {
            if (_document.RampFlightTypes.Contains(name))
            {
                throw new Exception(string.Format("RampFlightType {0} already exists and cannot be duplicated", name));
            }
            return new XbimRampFlightType(_document, name, description, type);
        }

        public XbimRampFlightType RampFlightTypeGetOrCreate(string name)
        {
            return _document.RampFlightTypes.Contains(name) ? _document.RampFlightTypes[name] : new XbimRampFlightType(_document, name);
        }
        #endregion

        #region BuildingElementProxyType
        public XbimBuildingElementProxyType BuildingElementProxyType(string name)
        {
            if (_document.BuildingElementProxyTypes.Contains(name))
            {
                throw new Exception(string.Format("BuildingElementProxyType {0} already exists and cannot be duplicated", name));
            }
            return new XbimBuildingElementProxyType(_document, name);
        }

        public XbimBuildingElementProxyType BuildingElementProxyTypeGetOrCreate(string name)
        {
            return _document.BuildingElementProxyTypes.Contains(name) ? _document.BuildingElementProxyTypes[name] : new XbimBuildingElementProxyType(_document, name);
        }
        #endregion

        #region Beam
        public XbimBeam Beam(XbimBeamType type)
        {
            return new XbimBeam(_document, type);
        }
        #endregion

        #region Column
        public XbimColumn Column(XbimColumnType type)
        {
            return new XbimColumn(_document, type);
        }
        #endregion

        #region Door
        public XbimDoor Door (XbimDoorStyle type)
        {
            return new XbimDoor(_document, type);
        }
        #endregion

        #region Railing
        public XbimRailing Railing(XbimRailingType type)
        {
            return new XbimRailing(_document, type);
        }
        #endregion

        #region RampFlight
        public XbimRampFlight RampFlight(XbimRampFlightType type)
        {
            return new XbimRampFlight(_document, type);
        }
        #endregion

        #region StairFlight
        public XbimStairFlight StairFlight(XbimStairFlightType type)
        {
            return new XbimStairFlight(_document, type);
        }
        #endregion

        #region CurtainWall
        public XbimCurtainWall CurtainWall(XbimCurtainWallType type)
        {
            return new XbimCurtainWall(_document, type);
        }
        #endregion

        #region Window
        public XbimWindow Window(XbimWindowStyle type)
        {
            return new XbimWindow(_document, type);
        }
        #endregion

        #region BuildingElementProxy
        public XbimBuildingElementProxy BuildingElementProxy(XbimBuildingElementProxyType type)
        {
            return new XbimBuildingElementProxy(_document, type);
        }
        public XbimBuildingElementProxy BuildingElementProxy()
        {
            return new XbimBuildingElementProxy(_document);
        }
       
        public XbimBuildingElementProxy BuildingElementProxy(XbimBuildingElementProxyType type, XbimLayerSetDirectionEnum MaterialLayersDirection, XbimDirectionSenseEnum MaterialLayersDirectionSense, float MaterialLayersOffsett)
        {
            return new XbimBuildingElementProxy(_document, type, MaterialLayersDirection, MaterialLayersDirectionSense, MaterialLayersOffsett);
        }
        #endregion
    }
}
