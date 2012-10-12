using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc2x3.Kernel;


namespace Xbim.DOM
{
    public class XbimDoor : XbimBuildingElement
    {
         #region constructors

        internal XbimDoor(XbimDocument document, XbimDoorStyle xbimDoorStyle)
            : base(document)
        {
            BaseInit(xbimDoorStyle);
        }

        internal XbimDoor(XbimDocument document, IfcDoor door)
            : base(document)
        {
            _ifcBuildingElement = door;
        }

        private void BaseInit(XbimDoorStyle xbimDoorStyle)
        {
            _document.Doors.Add(this);
            _ifcBuildingElement = _document.Model.Instances.New<IfcDoor>();
            _ifcBuildingElement.SetDefiningType(xbimDoorStyle.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimDoorStyle(_document, IfcTypeObject as IfcDoorStyle); }
        }

        public XbimDoorCommonProperties CommonProperties { get { return new XbimDoorCommonProperties(_ifcBuildingElement as IfcDoor); } }

        public void PlaceDoor(XbimWall wall, double Xoffset, double Zoffset, double openingWidth, double openingHeight)
            {

                //get thicknes of the wall from its layers
                IfcMaterialLayerSet matLayer = wall.IfcMaterialLayerSetUsage.ForLayerSet;
                double width = 0;
                foreach (IfcMaterialLayer layer in matLayer.MaterialLayers)
                {
                    width += layer.LayerThickness;
                }

                //local placement of the wall
                IfcLocalPlacement wallPlacement = wall.IfcBuildingElement.ObjectPlacement as IfcLocalPlacement;
                if (wallPlacement == null) throw new NotSupportedException();
                IfcAxis2Placement3D wallPlacement3D = wallPlacement.RelativePlacement as IfcAxis2Placement3D;
                if (wallPlacement3D == null) throw new NotSupportedException();

                //new local placement of the opening
                IfcLocalPlacement openingPlacement = Document.Model.Instances.New<IfcLocalPlacement>();
                openingPlacement.PlacementRelTo = wallPlacement;
                IfcAxis2Placement3D placement3D = Document.Model.Instances.New<IfcAxis2Placement3D>();
                openingPlacement.RelativePlacement = placement3D;
                //set opening placement
                placement3D.SetNewDirectionOf_XZ(
                    0, 0, 1,
                    0, 1, 0);
                placement3D.SetNewLocation(Xoffset, 0, Zoffset);


                if (_ifcBuildingElement.ObjectPlacement == null)
                {
                    //local placement for the door
                    IfcLocalPlacement doorPlacement = Document.Model.Instances.New<IfcLocalPlacement>();
                    doorPlacement.PlacementRelTo = openingPlacement;
                    IfcAxis2Placement3D doorPlacement3D = Document.Model.Instances.New<IfcAxis2Placement3D>();
                    doorPlacement.RelativePlacement = doorPlacement3D;
                    //set opening placement
                    doorPlacement3D.SetNewDirectionOf_XZ(
                        0, 1, 0,
                        1, 0, 0);
                    doorPlacement3D.SetNewLocation(0, 0, 0);
                    _ifcBuildingElement.ObjectPlacement = doorPlacement;
                }

                //create extrusion geometry for the opening
                XbimExtrudedAreaSolid hole = new XbimExtrudedAreaSolid(_document, width, openingWidth, openingHeight, new XbimXYZ(0, 0, 1));

                //create opening object
                IfcOpeningElement opening = Document.Model.Instances.New<IfcOpeningElement>();
                opening.ObjectPlacement = openingPlacement;
                IfcProductDefinitionShape representation = Document.Model.Instances.New<IfcProductDefinitionShape>();
                opening.Representation = representation;

                IfcShapeRepresentation shapeRepresentation = Document.Model.Instances.New<IfcShapeRepresentation>();
                //set context of geometry to the context of the model
                shapeRepresentation.ContextOfItems = ((IfcProject)Document.Model.IfcProject).ModelContext();
                shapeRepresentation.RepresentationType = "SweptSolid";
                shapeRepresentation.RepresentationIdentifier = "Body";
                shapeRepresentation.Items.Add_Reversible(hole.IfcSweptAreaSolid);
                representation.Representations.Add_Reversible(shapeRepresentation);

                //create relation between door and opening
                IfcRelFillsElement relFil = Document.Model.Instances.New<IfcRelFillsElement>();
                relFil.RelatedBuildingElement = IfcBuildingElement;
                relFil.RelatingOpeningElement = opening;

                //create relation between wall and opening
                IfcRelVoidsElement relVoids = Document.Model.Instances.New<IfcRelVoidsElement>();
                relVoids.RelatedOpeningElement = opening;
                relVoids.RelatingBuildingElement = wall;
            }
    }
}
