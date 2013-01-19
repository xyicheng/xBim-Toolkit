#undef SupportActivation

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.IO;
using Xbim.IO;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.UtilityResource;
using System.Diagnostics;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.ActorResource;

namespace Xbim.DOM
{
    public enum XbimModelView
    {
        CoordinationView
    }
    public class XbimDocument : IDisposable
    {
        //DOM infrastructure
        private XbimModel _model;
        private XbimObjectCreator _creator;
        //xbim transatcion on the model
        private XbimReadWriteTransaction _transaction;
        public XbimModelView ModelView = XbimModelView.CoordinationView;
        private IfcAxis2Placement3D _wcs;

        /// <summary>
        /// The world coosrdinate system for the document
        /// </summary>
        public IfcAxis2Placement3D WCS
        {
            get { return _wcs; }
            set { _wcs = value; }
        }
        #region Objects modified by SRL

        public XbimModel Model { get { return _model; } }
        public XbimModel IfcModel() { return _model; }
        

        #endregion

        public IEnumerable<IfcBuildingElement> IsMadeOf(XbimMaterial material)
        {
            HashSet<IfcBuildingElement> elements = new HashSet<IfcBuildingElement>();
            
            IEnumerable<IfcMaterialLayer> layers = _model.Instances.Where<IfcMaterialLayer>(l => l.Material == material.Material).Distinct();




            //IEnumerable<IfcRelAssociatesMaterial> rels = _model.InstancesWhere<IfcRelAssociatesMaterial>(
            //                                                .Select<IfcRelAssociatesMaterial, IfcMaterialSelect>(rel => rel.RelatingMaterial).OfType<IfcMaterialLayerSetUsage>()
            //                                                    .Select<IfcMaterialLayerSetUsage, IfcMaterialLayerSet>(lsu => lsu.ForLayerSet)
            //                                                    .SelectMany<IfcMaterialLayerSet, IfcMaterialLayer>(ls => ls.MaterialLayers)
            //                                                    .Where(ml => ml.Material == material.Material).Select(r);
                                                                
            //foreach (var rel in rels)
            //{
            //    foreach (var obj in rel.RelatedObjects.OfType<IfcBuildingElement>())
            //       if (!elements.Contains(obj)) elements.Add(obj);

            //}
            return elements;
        }

        public XbimObjectCreator Create { get { return _creator; } }

        
        //----------------------existing DOM objects in the document----------------------------------------
        //building elements
        private XbimMaterialCollection                                  _materials = new XbimMaterialCollection();
        private XbimUniqueNameCollection<XbimWallType>                  _wallTypes = new XbimUniqueNameCollection<XbimWallType>();
        private XbimUniqueNameCollection<XbimSlabType>                  _slabTypes = new XbimUniqueNameCollection<XbimSlabType>();
        private XbimUniqueNameCollection<XbimPlateType>                 _plateTypes = new XbimUniqueNameCollection<XbimPlateType>();
        private XbimUniqueNameCollection<XbimBeamType>                  _beamTypes = new XbimUniqueNameCollection<XbimBeamType>();
        private XbimUniqueNameCollection<XbimStairFlightType>           _stairFlightType = new XbimUniqueNameCollection<XbimStairFlightType>();
        private XbimUniqueNameCollection<XbimWindowStyle>               _windowStyles = new XbimUniqueNameCollection<XbimWindowStyle>();
        private XbimUniqueNameCollection<XbimDoorStyle>                 _doorStyles = new XbimUniqueNameCollection<XbimDoorStyle>();
        private XbimUniqueNameCollection<XbimCurtainWallType>           _curtainWallTypes = new XbimUniqueNameCollection<XbimCurtainWallType>();
        private XbimUniqueNameCollection<XbimColumnType>                _columnTypes = new XbimUniqueNameCollection<XbimColumnType>();
        private XbimUniqueNameCollection<XbimRailingType>               _railingTypes = new XbimUniqueNameCollection<XbimRailingType>();
        private XbimUniqueNameCollection<XbimRampFlightType>            _rampFlightTypes = new XbimUniqueNameCollection<XbimRampFlightType>();
        private XbimUniqueNameCollection<XbimBuildingElementProxyType>  _buildingElementProxyTypes = new XbimUniqueNameCollection<XbimBuildingElementProxyType>();
        private XbimUniqueNameCollection<XbimCoveringType>              _coveringTypes = new XbimUniqueNameCollection<XbimCoveringType>();

        private List<XbimWall>                  _walls = new List<XbimWall>();
        private List<XbimRoof>                  _roofs = new List<XbimRoof>();
        private List<XbimSlab>                  _slabs = new List<XbimSlab>();
        private List<XbimPlate>                 _plates = new List<XbimPlate>();
        private List<XbimBeam>                  _beams = new List<XbimBeam>();
        private List<XbimColumn>                _columns = new List<XbimColumn>();
        private List<XbimDoor>                  _doors = new List<XbimDoor>();
        private List<XbimRailing>               _railings = new List<XbimRailing>();
        private List<XbimRampFlight>            _rampFlights = new List<XbimRampFlight>();
        private List<XbimStairFlight>           _stairFlights = new List<XbimStairFlight>();
        private List<XbimCurtainWall>           _curtainWalls = new List<XbimCurtainWall>();
        private List<XbimWindow>                _windows = new List<XbimWindow>();
        private List<XbimBuildingElementProxy>  _buildingElementProxys = new List<XbimBuildingElementProxy>();
        private List<XbimCovering>              _coverings = new List<XbimCovering>();

        //spatial structure elements
        private List<XbimSite>              _sites = new List<XbimSite>();
        private List<XbimBuilding>          _buildings = new List<XbimBuilding>();
        private List<XbimBuildingStorey>    _storeys = new List<XbimBuildingStorey>();
        private List<XbimSpace>             _spaces = new List<XbimSpace>();

        //-------------------public properties to get access to document objects-----------------------------
        //building elements properties
        public XbimMaterialCollection Materials { get { return _materials; } }
        public XbimUniqueNameCollection<XbimWallType>                   WallTypes                   { get { return _wallTypes; } }
        public XbimUniqueNameCollection<XbimSlabType>                   SlabTypes                   { get { return _slabTypes; } }
        public XbimUniqueNameCollection<XbimPlateType>                  PlateTypes                  { get { return _plateTypes; } }
        public XbimUniqueNameCollection<XbimBeamType>                   BeamTypes                   { get { return _beamTypes; } }
        public XbimUniqueNameCollection<XbimStairFlightType>            StairFlightTypes            { get { return _stairFlightType; } }
        public XbimUniqueNameCollection<XbimWindowStyle>                WindowStyles                { get { return _windowStyles; } }
        public XbimUniqueNameCollection<XbimDoorStyle>                  DoorStyles                  { get { return _doorStyles; } }
        public XbimUniqueNameCollection<XbimCurtainWallType>            CurtainWallTypes            { get { return _curtainWallTypes; } }
        public XbimUniqueNameCollection<XbimColumnType>                 ColumnTypes                 { get { return _columnTypes; } }
        public XbimUniqueNameCollection<XbimRailingType>                RailingTypes                { get { return _railingTypes; } }
        public XbimUniqueNameCollection<XbimRampFlightType>             RampFlightTypes             { get { return _rampFlightTypes; } }
        public XbimUniqueNameCollection<XbimBuildingElementProxyType>   BuildingElementProxyTypes   { get { return _buildingElementProxyTypes; } }
        public XbimUniqueNameCollection<XbimCoveringType>               CoveringTypes               { get { return _coveringTypes;} }

        ////Select functions for WEB (ObjectDataSource SELECT method)
        //public IList<XbimMaterial> GetMaterials()  { return _materials; }
        //public IList<XbimWallType> GetWallTypes()  { return _wallTypes; }
        //public IList<XbimSlabType> GetSlabTypes () { return _slabTypes; }
        //public IList<XbimPlateType> GetPlateTypes () { return _plateTypes;}
        //public IList<XbimBeamType> GetBeamTypes()  { return _beamTypes; } 
        //public IList<XbimStairFlightType> GetStairFlightTypes () { return _stairFlightType; }
        //public IList<XbimWindowStyle> GetWindowStyles () { return _windowStyles; } 
        //public IList<XbimDoorStyle> GetDoorStyles()  { return _doorStyles; } 
        //public IList<XbimCurtainWallType> GetCurtainWallTypes () { return _curtainWallTypes; } 
        //public IList<XbimColumnType> GetColumnTypes()  { return _columnTypes; } 
        //public IList<XbimRailingType> GetRailingTypes ()  { return _railingTypes; } 
        //public IList<XbimRampFlightType> GetRampFlightTypes () { return _rampFlightTypes; } 

        public List<XbimWall>                   Walls                 { get { return _walls; } }
        public List<XbimRoof>                   Roofs                 { get { return _roofs; } }
        public List<XbimSlab>                   Slabs                 { get { return _slabs; } }
        public List<XbimPlate>                  Plates                { get { return _plates; } }
        public List<XbimBeam>                   Beams                 { get { return _beams; } }
        public List<XbimColumn>                 Columns               { get { return _columns; } }
        public List<XbimDoor>                   Doors                 { get { return _doors; } }
        public List<XbimRailing>                Railings              { get { return _railings; } }
        public List<XbimRampFlight>             RampFlights           { get { return _rampFlights; } }
        public List<XbimStairFlight>            StairFlights          { get { return _stairFlights; } }
        public List<XbimCurtainWall>            CurtainWalls          { get { return _curtainWalls; } }
        public List<XbimWindow>                 Windows               { get { return _windows; } }
        public List<XbimBuildingElementProxy>   BuildingElementProxys { get { return _buildingElementProxys; } }
        public List<XbimCovering>               Coverings             { get { return _coverings;} } 
        //spatial structure properties
        public List<XbimSite> Sites { get { return _sites; } }
        public List<XbimBuilding> Buildings { get { return _buildings; } }
        public List<XbimBuildingStorey> Storeys { get { return _storeys; } }
        public List<XbimSpace> Spaces { get { return _spaces; } }

        public IEnumerable<XbimBuildingElement> AllBuildingElements 
        {
            get
            { 
                foreach (XbimBuildingElement elem in Walls                ) {yield return elem;}
                foreach (XbimBuildingElement elem in Roofs                ) {yield return elem;}
                foreach (XbimBuildingElement elem in Slabs                ) {yield return elem;}
                foreach (XbimBuildingElement elem in Plates               ) {yield return elem;}
                foreach (XbimBuildingElement elem in Beams                ) {yield return elem;}
                foreach (XbimBuildingElement elem in Columns              ) {yield return elem;}
                foreach (XbimBuildingElement elem in Doors                ) {yield return elem;}
                foreach (XbimBuildingElement elem in Railings             ) {yield return elem;}
                foreach (XbimBuildingElement elem in RampFlights          ) {yield return elem;}
                foreach (XbimBuildingElement elem in StairFlights         ) {yield return elem;}
                foreach (XbimBuildingElement elem in CurtainWalls         ) {yield return elem;}
                foreach (XbimBuildingElement elem in Windows              ) {yield return elem;}
                foreach (XbimBuildingElement elem in BuildingElementProxys) {yield return elem;}
                foreach (XbimBuildingElement elem in Coverings            ) {yield return elem;}
            }
        }

        public IEnumerable<XbimBuildingElementType> AllBuildingElementTypes
        {
            get
            {
                foreach (XbimBuildingElementType elem in WallTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in SlabTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in PlateTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in BeamTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in StairFlightTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in WindowStyles) { yield return elem; }
                foreach (XbimBuildingElementType elem in DoorStyles) { yield return elem; }
                foreach (XbimBuildingElementType elem in CurtainWallTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in ColumnTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in RailingTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in RampFlightTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in BuildingElementProxyTypes) { yield return elem; }
                foreach (XbimBuildingElementType elem in CoveringTypes) { yield return elem; }
            }
        }

        public IEnumerable<XbimSpatialStructureElement> AllSpatialStructureElements
        {
            get
            {
                foreach (XbimSpatialStructureElement elem in Sites) { yield return elem; }
                foreach (XbimSpatialStructureElement elem in Buildings) { yield return elem; }
                foreach (XbimSpatialStructureElement elem in Storeys) { yield return elem; }
                foreach (XbimSpatialStructureElement elem in Spaces) { yield return elem; }
            }
        }

        #region constructors
        public XbimDocument(string applicationId, string applicationVersion, string applicationName, string authorName, string organisationName, string viewDefinition)
        {
            BaseInit();

            using (XbimReadWriteTransaction trans = _model.BeginTransaction("Model initialization"))
            {
                IfcApplication app = _model.DefaultOwningApplication as IfcApplication;
                if (app != null)
                {
                    app.ApplicationIdentifier = applicationId;
                    app.ApplicationDeveloper.Name = authorName;
                    app.ApplicationFullName = applicationName;
                    app.Version = applicationVersion;
                }
                IfcPersonAndOrganization po = _model.DefaultOwningUser as IfcPersonAndOrganization;
                if (po != null)
                {
                    po.ThePerson.FamilyName = "Unknown";
                    po.TheOrganization.Name = "Unknown";
                }
                _model.Header.FileDescription.Description.Clear();
                _model.Header.FileDescription.Description.Add(viewDefinition);
                _model.Header.FileName.AuthorName.Add(authorName);
                _model.Header.FileName.AuthorizationName = organisationName;
                IfcProject project = _model.Instances.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = "Xbim";
               
                trans.Commit();
            }

        }

        public XbimDocument()
        {
            //initialize empty lists and collections;
            BaseInit();
        }

        public XbimDocument(string fileName)
        {
            BaseInit();
           
            try
            {
                _model = new XbimModel();
                Model.CreateFrom(fileName);

            }
            catch (Exception e)
            {
                throw new Exception(string.Format("{0} is not a valid Ifc File\n{1}", fileName, e.Message)); //todo: different form of error messaging - it is possible that it is wrong data but we can do something with that

            }
           

            //make all existing elements accessible via the document
            InitMaterials();
            InitBuildingElementTypes();
            InitBuildingElements();
            InitSpatialStructures();
        }

        public XbimDocument(XbimModel model)
        {
            _model = model;

            //initialize empty lists and collections;
            BaseInit();

            //make all existing elements accessible via the document
           
            InitMaterials();
            InitBuildingElementTypes();
            InitBuildingElements();
            InitSpatialStructures();
        }

        #endregion

        protected virtual void BaseInit()
        {
            _creator = new XbimObjectCreator(this);
            if (_model == null) _model = new XbimModel();
            _transaction = _model.BeginTransaction("XbimDocument transaction");
            _wcs = _model.Instances.New<IfcAxis2Placement3D>();
            //set world coordinate system
            _wcs.SetNewDirectionOf_XZ(
                0, 0, 1,
                0, 1, 0);
            _wcs.SetNewLocation(0, 0, 0);
           
        }

        protected virtual void InitMaterials()
        {
            _materials = new XbimMaterialCollection();
            IEnumerable<IfcMaterial> ifcMaterials = Model.Instances.OfType<IfcMaterial>();
            foreach (IfcMaterial material in ifcMaterials) Materials.Add(new XbimMaterial(this, material));
        }

        protected virtual void InitSpatialStructures()
        {
            _sites = new List<XbimSite>();
            _buildings = new List<XbimBuilding>();
            _storeys = new List<XbimBuildingStorey>();
            _spaces = new List<XbimSpace>();

            //spatial structure elements: 
            foreach (IfcSite site in Model.Instances.OfType<IfcSite>()) _sites.Add(new XbimSite(this, site));
            foreach (IfcBuilding building in Model.Instances.OfType<IfcBuilding>()) _buildings.Add(new XbimBuilding(this, building));
            foreach (IfcBuildingStorey storey in Model.Instances.OfType<IfcBuildingStorey>()) _storeys.Add(new XbimBuildingStorey(this, storey));
            foreach (IfcSpace space in Model.Instances.OfType<IfcSpace>()) _spaces.Add(new XbimSpace(this, space));
        }

        protected virtual void InitBuildingElements()
        {
            _walls = new List<XbimWall>();
            _roofs = new List<XbimRoof>();
            _slabs = new List<XbimSlab>();
            _plates = new List<XbimPlate>();
            _beams = new List<XbimBeam>();
            _columns = new List<XbimColumn>();
            _doors = new List<XbimDoor>();
            _railings = new List<XbimRailing>();
            _rampFlights = new List<XbimRampFlight>();
            _stairFlights = new List<XbimStairFlight>();
            _curtainWalls = new List<XbimCurtainWall>();
            _windows = new List<XbimWindow>();
            _buildingElementProxys = new List<XbimBuildingElementProxy>();
            _coverings = new List<XbimCovering>();

            //building elements
            foreach (IfcWall wall in Model.Instances.OfType<IfcWall>()) _walls.Add(new XbimWall(this, wall));
            foreach (IfcRoof roof in Model.Instances.OfType<IfcRoof>()) _roofs.Add(new XbimRoof(this, roof));
            foreach (IfcSlab slab in Model.Instances.OfType<IfcSlab>()) _slabs.Add(new XbimSlab(this, slab));
            foreach (IfcPlate plate in Model.Instances.OfType<IfcPlate>()) _plates.Add(new XbimPlate(this, plate));
            foreach (IfcBeam beam in Model.Instances.OfType<IfcBeam>()) _beams.Add(new XbimBeam(this, beam));
            foreach (IfcColumn column in Model.Instances.OfType<IfcColumn>()) _columns.Add(new XbimColumn(this, column));
            foreach (IfcDoor door in Model.Instances.OfType<IfcDoor>()) _doors.Add(new XbimDoor(this, door));
            foreach (IfcRailing railing in Model.Instances.OfType<IfcRailing>()) _railings.Add(new XbimRailing(this, railing));
            foreach (IfcRampFlight ramp in Model.Instances.OfType<IfcRampFlight>()) _rampFlights.Add(new XbimRampFlight(this, ramp));
            foreach (IfcStairFlight stairFlight in Model.Instances.OfType<IfcStairFlight>()) _stairFlights.Add(new XbimStairFlight(this, stairFlight));
            foreach (IfcCurtainWall curtWall in Model.Instances.OfType<IfcCurtainWall>()) _curtainWalls.Add(new XbimCurtainWall(this, curtWall));
            foreach (IfcWindow window in Model.Instances.OfType<IfcWindow>()) _windows.Add(new XbimWindow(this, window));
            foreach (IfcBuildingElementProxy proxy in Model.Instances.OfType<IfcBuildingElementProxy>()) _buildingElementProxys.Add(new XbimBuildingElementProxy(this, proxy));
            foreach (IfcCovering covering in Model.Instances.OfType<IfcCovering>()) _coverings.Add(new XbimCovering(this, covering));
        }


        protected virtual void InitBuildingElementTypes()
        {
            _wallTypes = new XbimUniqueNameCollection<XbimWallType>();
            _slabTypes = new XbimUniqueNameCollection<XbimSlabType>();
            _plateTypes = new XbimUniqueNameCollection<XbimPlateType>();
            _beamTypes = new XbimUniqueNameCollection<XbimBeamType>();
            _stairFlightType = new XbimUniqueNameCollection<XbimStairFlightType>();
            _windowStyles = new XbimUniqueNameCollection<XbimWindowStyle>();
            _doorStyles = new XbimUniqueNameCollection<XbimDoorStyle>();
            _curtainWallTypes = new XbimUniqueNameCollection<XbimCurtainWallType>();
            _columnTypes = new XbimUniqueNameCollection<XbimColumnType>();
            _railingTypes = new XbimUniqueNameCollection<XbimRailingType>();
            _rampFlightTypes = new XbimUniqueNameCollection<XbimRampFlightType>();
            _buildingElementProxyTypes = new XbimUniqueNameCollection<XbimBuildingElementProxyType>();
            _coveringTypes = new XbimUniqueNameCollection<XbimCoveringType>();

            //building element types:
            foreach (IfcWallType wallType in Model.Instances.OfType<IfcWallType>()) WallTypes.Add(new XbimWallType(this, wallType));
            foreach (IfcSlabType slabType in Model.Instances.OfType<IfcSlabType>()) SlabTypes.Add(new XbimSlabType(this, slabType));
            foreach (IfcPlateType plateType in Model.Instances.OfType<IfcPlateType>()) PlateTypes.Add(new XbimPlateType(this, plateType));
            foreach (IfcBeamType ifcBeamType in Model.Instances.OfType<IfcBeamType>()) { BeamTypes.Add(new XbimBeamType(this, ifcBeamType)); }
            foreach (IfcStairFlightType ifcStairFlightType in Model.Instances.OfType<IfcStairFlightType>()) {StairFlightTypes.Add( new XbimStairFlightType(this, ifcStairFlightType)); }
            foreach (IfcWindowStyle windowStyle in Model.Instances.OfType<IfcWindowStyle>()) {WindowStyles.Add( new XbimWindowStyle(this, windowStyle)); }
            foreach (IfcDoorStyle doorStyle in Model.Instances.OfType<IfcDoorStyle>()) {DoorStyles.Add(new XbimDoorStyle(this, doorStyle)); }
            foreach (IfcCurtainWallType curtainWallType in Model.Instances.OfType<IfcCurtainWallType>()) {CurtainWallTypes.Add( new XbimCurtainWallType(this, curtainWallType)); }
            foreach (IfcColumnType columnType in Model.Instances.OfType<IfcColumnType>()) { ColumnTypes.Add( new XbimColumnType(this, columnType)); }
            foreach (IfcRailingType railingType in Model.Instances.OfType<IfcRailingType>()) {RailingTypes.Add( new XbimRailingType(this, railingType)); }
            foreach (IfcRampFlightType rampFlightType in Model.Instances.OfType<IfcRampFlightType>()) {RampFlightTypes.Add( new XbimRampFlightType(this, rampFlightType)); }
            foreach (IfcBuildingElementProxyType proxyType in Model.Instances.OfType<IfcBuildingElementProxyType>()) { BuildingElementProxyTypes.Add(new XbimBuildingElementProxyType(this, proxyType)); }
            foreach (IfcCoveringType type in Model.Instances.OfType<IfcCoveringType>()) { CoveringTypes.Add(new XbimCoveringType(this, type)); }
        }


        #region public functions


        public void Save(string fileName)
        {

            _model.Header.FileName.Name = Path.GetFileName(fileName);
            _model.SaveAs(fileName, XbimStorageType.IFC);

        }

        /// <summary>
        /// Merges the content of mergeDoc with the this document. The IfcProject of this document is used. All items merged are removed from the MergeDocument
        /// </summary>
        /// <param name="mergeDoc"></param>
        public void MergeDocument(XbimDocument mergeDoc)
        {
            //this.Model.MoveTo(mergeDoc.Model);
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Writes actual data in the model to the IFC physical file.
        /// </summary>
        /// <param name="FileName">Name of the output file</param>
        /// <param name="ValidationFlag">If true, function returns the error message from the validation of the model</param>
        /// <returns></returns>
        public string WriteModelToFile(string FileName, bool ValidationFlag)
        {
            string output = null;
            Model.SaveAs(FileName, XbimStorageType.IFC);
            return output;
        }


        /// <summary>
        /// Sets SI unit or change it if it exists. It does not affect any physical 
        /// values in the model, it just changes their meaning.
        /// </summary>
        /// <param name="UnitType">Enumeration of unit types</param>
        /// <param name="siUnitName">Enumeration of base SI unit names</param>
        /// <param name="siUnitPrefix">Enumeration of SI units prefixes</param>
        public void SetOrChangeSIUnit(IfcUnitEnum UnitType, IfcSIUnitName siUnitName, IfcSIPrefix? siUnitPrefix)
        {
            ((IfcProject)Model.IfcProject).SetOrChangeSIUnit(UnitType, siUnitName, siUnitPrefix);
        }


        /// <summary>
        /// Sets conversional unit (like foot, gallon, ...) or change it if it exists. 
        /// It does not affect any physical values in the model, it just changes their meaning.
        /// </summary>
        /// <param name="UnitType">Enumeration of unit types</param>
        /// <param name="conversionUnit">Enumeration of conversional units defined in IFC2x3</param>
        //public void SetOrChangeConversionUnit(IfcUnitEnum UnitType, ConversionBasedUnit conversionUnit)
        //{
        //    Model.IfcProject.SetOrChangeConversionUnit(UnitType, conversionUnit);
        //}

        #endregion

        #region Getting objects functions
        public XbimBuildingElement GetBuildingElement(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return AllBuildingElements.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public XbimBuildingElementType GetBuildingElementType(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return AllBuildingElementTypes.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public XbimBuildingElementType GetBuildingElementType(string name)
        {
            return AllBuildingElementTypes.Where(e => e.Name == name).FirstOrDefault();
        }


        public XbimMaterial GetMaterial(string name)
        {
            if (Materials.Contains(name)) return Materials[name];
            return null;
        }

        public XbimSpatialStructureElement GetSpatialStructureElement(string name)
        {
            return AllSpatialStructureElements.Where(e => e.Name == name).FirstOrDefault();
        }

        public XbimSpatialStructureElement GetSpatialStructureElement(Guid guid)
        {
            Ifc2x3.UtilityResource.IfcGloballyUniqueId id = new Ifc2x3.UtilityResource.IfcGloballyUniqueId(guid);
            return AllSpatialStructureElements.Where(e => e.GlobalId == id).FirstOrDefault();
        }

        public XbimSpatialStructureElement GetDefaultBuilding()
        {
            return Buildings.FirstOrDefault();
        }
        #endregion



        #region Changing types infrastructure
        public void ChangeElementsType(Guid oldType, Guid newType)
        {
            XbimBuildingElementType oldT = GetBuildingElementType(oldType);
            XbimBuildingElementType newT = GetBuildingElementType(newType);
            if (oldT == null || newT == null) throw new Exception("Elements not contained in the document");
            ChangeElementsType(oldT, newT);
        }

        public void ChangeElementsType(XbimBuildingElementType oldType, XbimBuildingElementType newType)
        {
            if (oldType == newType || oldType.GlobalId == newType.GlobalId) return;  //no processing if the elements are the same ones

            if (oldType.Document != newType.Document)
            {
                InsertBuildingElementType(newType); //insert new type if it is not present in the actual document
            }
            IEnumerable<XbimBuildingElement> elements = GetElementsOfType(oldType);
            foreach (var element in elements)
            {
                //change Type
                element.IfcTypeObject = newType.IfcTypeProduct;

                //change state in owner history to modified so that we can get the modified data quickly "GetModifiedBuildingElements()"
                IfcBuildingElement ifcElement = element.IfcBuildingElement;
                ifcElement.OwnerHistory.ChangeAction = Ifc2x3.UtilityResource.IfcChangeActionEnum.MODIFIED;
            }
        }

        public bool InsertBuildingElementType (XbimBuildingElementType newType)
        {
            //XbimBuildingElementType test = AllBuildingElementTypes.Where(t => t == newType || t.GlobalId == newType.GlobalId).FirstOrDefault();
            //if (test != null) return false; //if it is already there there is no point in inserting that

            ////if model is Transient model it is possible to use side effect
            //XbimMemoryModel model = Model as XbimMemoryModel;
            //if (model != null)
            //{
            //    //create new actual owner history with proper state
            //    newType.IfcTypeProduct.OwnerHistory = GetNewOwnerHistory(IfcChangeActionEnum.ADDED);
            //    model.AddNew(newType.IfcTypeProduct);
            //    return true;
            //}
 
            //other types of models are not supported at the moment
            throw new NotImplementedException(); //todo: implement inserting of elements
        }

        public IEnumerable<XbimBuildingElement> GetElementsOfType(XbimBuildingElementType type)
        {
            return AllBuildingElements.Where(el => el.IfcTypeObject == type.IfcTypeProduct);
        }

        public IEnumerable<XbimBuildingElement> GetModifiedBuildingElements()
        {
            return AllBuildingElements.Where(el => el.IfcBuildingElement.OwnerHistory.ChangeAction == Ifc2x3.UtilityResource.IfcChangeActionEnum.MODIFIED);
        }

        public IEnumerable<XbimBuildingElementType> GetNewAddedBuildingElementTypes()
        {
            return AllBuildingElementTypes.Where(ty => ty.IfcTypeProduct.OwnerHistory.ChangeAction == Ifc2x3.UtilityResource.IfcChangeActionEnum.ADDED);
        }

        /// <summary>
        /// returns document with only added od modified objects acording to the information on IfcOwnerHistory
        /// </summary>
        /// <returns></returns>
        public XbimDocument GetModificationDocument()
        {
            XbimModel model = new XbimModel();
           /// TODO: ResolveEventArgs this code
            //IEnumerable<IfcRoot> instances = this.Model.InstancesWhere<IfcRoot>(r => r.OwnerHistory.ChangeAction == IfcChangeActionEnum.MODIFIED || r.OwnerHistory.ChangeAction == IfcChangeActionEnum.MODIFIEDADDED || r.OwnerHistory.ChangeAction == IfcChangeActionEnum.ADDED);
            //foreach (var item in instances)
            //{
            //    model.Instances.Add(item);
            //}
            XbimDocument result = new XbimDocument(model);
            return result;
        }

        public void RefreshDocument()
        {
            InitMaterials();
            InitBuildingElementTypes();
            InitBuildingElements();
        }

        /// <summary>
        /// Generates owner history for every single IfcRoot object in the model and set its ChangeAction to NOCHANGE.
        /// It can be than used as benchmark for all the new changes. All existing owner history objects are destroyed.
        /// They stay in document just as siblings.
        /// </summary>
        public void GenerateNoChangeOwnerHistoryForAll()
        {
            IEnumerable<IfcRoot> instances = this.Model.Instances.OfType<IfcRoot>();
            foreach (var instance in instances)
            {
                //create new object of owner history
                instance.OwnerHistory = GetNewOwnerHistory(IfcChangeActionEnum.NOCHANGE);
            }
        }

        private IfcOwnerHistory GetNewOwnerHistory(IfcChangeActionEnum changeAction)
        {
            //existing default owner history
            IfcOwnerHistory defOwner = Model.OwnerHistoryAddObject as IfcOwnerHistory;
            IfcTimeStamp stamp = IfcTimeStamp.ToTimeStamp(DateTime.Now);

            //return new object
            return Model.Instances.New<IfcOwnerHistory>(h => { h.OwningUser = defOwner.OwningUser; h.OwningApplication = defOwner.OwningApplication; h.CreationDate = stamp; h.ChangeAction = changeAction; });
        }

        /// <summary>
        /// Tries to find groups of elements of the same type and create their element type
        /// </summary>
        public void TryToCreateUndefinedElementTypes()
        {
            TryToCreateUndefinedElementTypes(Model);
        }


        /// <summary>
        /// Tries to find groups of elements of the same type and create their element type
        /// </summary>
        public static void TryToCreateUndefinedElementTypes(IModel model)
        {
            //get elements without element type
            IEnumerable<IfcBuildingElement> temp = model.Instances.Where<IfcBuildingElement>(el => el.GetDefiningType() == null);
            IEnumerable<IfcBuildingElement> elements = temp.Where(el => el.GetMaterialLayerSetUsage(model) != null);

            //create groups of elements with the asme material layer set usage (it means they have the same element type indirectly)
            IEnumerable<IGrouping<IfcMaterialLayerSet, IfcBuildingElement>> groups = elements.GroupBy(el => el.GetMaterialLayerSetUsage(model).ForLayerSet);

            foreach (IGrouping<IfcMaterialLayerSet, IfcBuildingElement> group in groups)
            {
                IfcMaterialLayerSet layerSet = group.Key;
                List<IfcBuildingElement> toProcess = group.ToList();

                //create new type object
                IfcTypeProduct type = null;
                //element to find the proper type
                IfcBuildingElement elem = group.FirstOrDefault();

                if (elem.IsDecomposedBy != null)
                {
                    if (elem.IsDecomposedBy.Count() != 0)
                    {
                        //composed element like stair or roof
                        Debug.WriteLine("Decomposed element detected and skipped: " + elem.Name);
                        toProcess.Remove(elem);


                        //add decomposing elements to "to process" list 
                        foreach (var rel in elem.IsDecomposedBy)
                        {
                            foreach (var el in rel.RelatedObjects)
                            {
                                IfcBuildingElement buildelem = el as IfcBuildingElement;
                                if (buildelem != null) toProcess.Add(buildelem);
                                //change type to look for (i.e. IfcSlab instead of IfcRoof - there is nothinh gile IfcRoofType in IFC 2x3)
                                elem = buildelem;
                            }
                        }
                        /*it means that if the roof is composed from four 
                         slabs it appears as four floor slabs at the end of this proces.
                         This is side effect we have to cope with.*/
                    }
                }

                if (elem is IfcBuildingElementProxy) type = model.Instances.New<IfcBuildingElementProxyType>();
                if (elem is IfcCovering) type = model.Instances.New<IfcCoveringType>();
                if (elem is IfcBeam) type = model.Instances.New<IfcBeamType>();
                if (elem is IfcColumn) type = model.Instances.New<IfcColumnType>();
                if (elem is IfcCurtainWall) type = model.Instances.New<IfcCurtainWallType>();
                if (elem is IfcDoor) type = model.Instances.New<IfcDoorStyle>();
                if (elem is IfcMember) type = model.Instances.New<IfcMemberType>();
                if (elem is IfcRailing) type = model.Instances.New<IfcRailingType>();
                if (elem is IfcRampFlight) type = model.Instances.New<IfcRampFlightType>();
                if (elem is IfcWall) type = model.Instances.New<IfcWallType>();
                if (elem is IfcSlab) type = model.Instances.New<IfcSlabType>(t => t.PredefinedType = (elem as IfcSlab).PredefinedType ?? IfcSlabTypeEnum.NOTDEFINED);
                if (elem is IfcStairFlight) type = model.Instances.New<IfcStairFlightType>();
                if (elem is IfcWindow) type = model.Instances.New<IfcWindowStyle>();
                if (elem is IfcPlate) type = model.Instances.New<IfcPlateType>();
                if (elem is IfcCovering) type = model.Instances.New<IfcCoveringType>();

                if (type == null) 
                {
#if DEBUG
                    throw new Exception("No type for the element!");
#else
                    continue;
#endif
                }
                //material layer set
                type.SetMaterial(layerSet);

                //set element type name
                string typeName = layerSet.LayerSetName;
                string[] parts = typeName.Split(':');
                if (parts.Length > 1) typeName = parts[1];
                //create default name
                if (string.IsNullOrEmpty(typeName))
                {
                    typeName = type.GlobalId; //to be sure that it is unique for now
                }

                //assign name to the type object
                type.Name = typeName;
                
                //assign element type to the instances
                foreach (var element in toProcess)
                {
                    element.SetDefiningType(type, model);
                }
            }
        }

        /// <summary>
        /// Tries to create property table of building elements with material names and their 
        /// volumes acording to geometry and material layer structure of the element.
        /// </summary>
        public void GenerateMaterialVolumeTables()
        {
            IModel model = _model;

            //get elements
            IEnumerable<IfcBuildingElement> elements = model.Instances.OfType<IfcBuildingElement>();

            foreach (var element in elements)
            {
                //get existing tables if any exist
                XbimMaterialQuantities quantities = new XbimMaterialQuantities(element, this);
                IfcPropertyTableValue table = element.GetPropertyTableValue(XbimMaterialQuantities._pSetName, XbimMaterialQuantities._pVolumeName);
                if (table != null)
                {
                    //delete existing tables
                    model.Delete(table);
                }

                //get volume from the table (must be preprocessed before)
                IfcValue volVal = element.GetPropertySingleNominalValue("BuildingElementVolume", "Volume");
                if (volVal == null) continue;
                if (!(volVal is IfcVolumeMeasure)) continue; //volume information is not present in the model
                
                IfcVolumeMeasure volume = (IfcVolumeMeasure)volVal; //get volume as ifc volume measure
                if (volume <= 0) continue;

                //get material layers (if there are any)
                IfcMaterialSelect materialSelect = element.GetMaterial();
                if (materialSelect == null)
                {
                    //try to get material or material structure from the element's type
                    IfcTypeObject type = element.GetDefiningType(Model);
                    if (type == null) continue;
                    materialSelect = type.GetMaterial();
                    if (materialSelect == null) continue;
                }

                //in the case that material is from element type
                IfcMaterialLayerSet layerSet = materialSelect as IfcMaterialLayerSet;
                if (layerSet == null)
                {
                    IfcMaterialLayerSetUsage materialUsage = materialSelect as IfcMaterialLayerSetUsage;
                    if (materialUsage != null)
                    {
                        layerSet = materialUsage.ForLayerSet;
                    }
                }
                if (layerSet != null)
                {
                    foreach (var layer in layerSet.MaterialLayers)
                    {
                        //get material name
                        IfcLabel materialName = layer.Material.Name;

                        //compute volume of the material
                        IfcVolumeMeasure matVolume = volume / layerSet.TotalThickness * layer.LayerThickness; //portion of the material in the layer structure

                        //save it to the property set
                        quantities.SetMaterialVolume(materialName, matVolume);
                    }
                }
                else
                {
                    continue;
                }

                //if there is no material layer, check material and use it for all element
                IfcMaterial material = materialSelect as IfcMaterial;
                if (material != null)
                {
                    //get material name
                    IfcLabel materialName = material.Name;

                    //save it to the property set
                    quantities.SetMaterialVolume(materialName, volume); 
                }
            }
        }

        /// <summary>
        /// Ensure that there are only unique names for materials and building element types in the model
        /// (this is for example even Revit assumption for element types)
        /// </summary>
        public static void GenerateUniqueNames(IModel model)
        {
            IEnumerable<IfcTypeProduct> types = model.Instances.OfType<IfcTypeProduct>();
            foreach (var type in types)
            {
                List<IfcTypeProduct> identNameTypes = types.Where(t => t.Name == type.Name).ToList();
                if (identNameTypes.Count() == 1) continue;

                //skip the first one and rename the rest with the increment
                for (int i = 1; i < identNameTypes.Count(); i++) 
                {
                    IfcTypeProduct identType = identNameTypes[i];
                    string oldName = identType.Name;
                    identType.Name = oldName + "_" + i;
                }
            }
        }

        public void TryToGetElementNRMQuantitiesFromProperties()
        {
            //get project length units => use right volume units
            double power = ((IfcProject)Model.IfcProject).UnitsInContext.LengthUnitPower();

            foreach (var element in AllBuildingElements)
            {
                XbimPropertySingleValue lengthProp = element.SingleProperties.FlatProperties.Where(p => p.Name.ToLower().Contains("length")).FirstOrDefault();
                XbimPropertySingleValue areaProp = element.SingleProperties.FlatProperties.Where(p => p.Name.ToLower().Contains("area")).FirstOrDefault();

                if (lengthProp != null) { double length = (double)lengthProp.Value; element.NRMQuantities.Length = length * power; } //set length in meters
                if (areaProp != null) { double area = (double)areaProp.Value; element.NRMQuantities.Area = area *power * power; } //set area in square meters
                element.NRMQuantities.Count = 1;
            }
        }

        public void UpdateMaterialPropertiesFromLibrary(string libraryPath)
        {
            //update material information acording to library data
            XbimDocument library = new XbimDocument(libraryPath);
            XbimMaterialCollection materials = library.Materials;

            //update material informations using NBL/ICIM library
            foreach (var modelMaterial in this.Materials)
            {
                string matName = modelMaterial.Name;
                XbimMaterial libMaterial = materials.GetMaterialByName(matName);
                if (libMaterial != null)
                {
                    foreach (var property in libMaterial.SingleProperties.FlatProperties)
                    {
                        modelMaterial.SingleProperties.SetProperty(property);
                    }
                }
            }
        }

        public void GenerateUniqueNames()
        {
            GenerateUniqueNames(Model);
        }
        #endregion

        #region Grouping
        public void AddObjectToGroup(string groupName, IXbimRoot obj)
        {
            IfcGroup group = GetOrCreateGroup(groupName);
            IfcObjectDefinition definition = obj.AsRoot as IfcObjectDefinition;
            if (definition != null)
            {
                group.AddObjectToGroup(definition);
            }
        }

        public IfcGroup AddElementsToGroup(string groupName, IEnumerable<IXbimRoot> objects)
        {
            IfcGroup group = GetOrCreateGroup(groupName);
            foreach (var obj in objects)
            {
                IfcObjectDefinition definition = obj.AsRoot as IfcObjectDefinition;
                if (definition != null)
                {
                    group.AddObjectToGroup(definition);
                }
            }
            return group;
        }

        public IEnumerable<IfcObjectDefinition> GetGroupedObjects(string groupName)
        {
            IfcGroup group = GetOrCreateGroup(groupName);
            return group.GetGroupedObjects();
        }

        private IfcGroup GetOrCreateGroup(string name)
        {
            //check group existence
            IfcGroup group = Model.Instances.Where<IfcGroup>(gr => gr.Name == name).FirstOrDefault();  //assume unique name of the group in the model which is not IFC rule
            if (group == null) group = Model.Instances.New<IfcGroup>(gr => gr.Name = name);
            //check relation existence and create it if it does not esist
            IfcRelAssignsToGroup relation = group.IsGroupedBy;
            if (relation == null) Model.Instances.New<IfcRelAssignsToGroup>();

            //return group
            return group;
        }
        #endregion


        #region Dispose
        void IDisposable.Dispose()
        {
            if (Model != null)
            {
                //commit transaction of the xbim document
                if (_transaction != null)
                {
                    _transaction.Commit();
                }
                //srl this code needs to be resolved due to changes to the XbimModel server
                _model  = null;
                Debug.Assert(false, "Code attention required below this line");
                ////close model server if it is the case
                //if (Model is XbimFileModelServer)
                //{
                //    (Model as XbimFileModelServer).Dispose(true);
                //}
                //if (Model is XbimMemoryModel)
                //{
                //    _model  = null;
                //}
            }
        }

        #endregion

        protected XbimWallType NewWallType(IfcWallType type) { return new XbimWallType(this, type); }
        protected XbimSlabType NewSlabType(IfcSlabType type) { return new XbimSlabType(this, type); }
        protected XbimPlateType NewPlateType(IfcPlateType type) { return new XbimPlateType(this, type); }
        protected XbimBeamType NewBeamType(IfcBeamType type) { return new XbimBeamType(this, type);}
        protected XbimStairFlightType NewStairFlightType(IfcStairFlightType type) { return new XbimStairFlightType(this, type); }
        protected XbimWindowStyle NewWindowStyle(IfcWindowStyle type) { return new XbimWindowStyle(this, type); }
        protected XbimDoorStyle NewDoorStyle(IfcDoorStyle type) { return new XbimDoorStyle(this, type); }
        protected XbimCurtainWallType NewCurtainWallType(IfcCurtainWallType type) { return new XbimCurtainWallType(this, type); }
        protected XbimColumnType NewColumnType(IfcColumnType type) { return new XbimColumnType(this, type); }
        protected XbimRailingType NewRailingType(IfcRailingType type) { return new XbimRailingType(this, type); }
        protected XbimRampFlightType NewRampFlightType(IfcRampFlightType type) { return new XbimRampFlightType(this, type); }
        protected XbimBuildingElementProxyType NewBuildingElementProxyType(IfcBuildingElementProxyType type) { return new XbimBuildingElementProxyType(this, type); }
        protected XbimCoveringType NewCoveringType(IfcCoveringType type) { return new XbimCoveringType(this, type); }

    }

}
