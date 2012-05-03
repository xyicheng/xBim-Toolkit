using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.RepresentationResource;

namespace Xbim.DOM
{
    public class XbimWindow: XbimBuildingElement
    {
        #region constructors

        internal XbimWindow(XbimDocument document, XbimWindowStyle type)
            : base(document)
        {
            BaseInit(type);
        }

        internal XbimWindow(XbimDocument document, IfcWindow element)
            : base(document)
        {
            _ifcBuildingElement = element;
        }

        private void BaseInit(XbimWindowStyle type)
        {
            _document.Windows.Add(this);
            _ifcBuildingElement = _document.Model.New<IfcWindow>();
            _ifcBuildingElement.SetDefiningType(type.IfcTypeProduct, _document.Model);
        }
        #endregion

        public override XbimBuildingElementType ElementType
        {
            get { return IfcTypeObject == null ? null : new XbimWindowStyle(_document, IfcTypeObject as IfcWindowStyle); }
        }


        public void PlaceWindow(XbimWall wall, double Xoffset, double Zoffset, double openingWidth, double openingHeight)
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
            IfcLocalPlacement openingPlacement = Document.Model.New<IfcLocalPlacement>();
            openingPlacement.PlacementRelTo = wallPlacement;
            IfcAxis2Placement3D placement3D = Document.Model.New<IfcAxis2Placement3D>();
            openingPlacement.RelativePlacement = placement3D;
            //set opening placement
            placement3D.SetNewDirectionOf_XZ(
                0, 0, 1,
                0, 1, 0);
            placement3D.SetNewLocation(Xoffset, 0, Zoffset);


            if (_ifcBuildingElement.ObjectPlacement == null)
            {
                //local placement for the door
                IfcLocalPlacement windowPlacement = Document.Model.New<IfcLocalPlacement>();
                windowPlacement.PlacementRelTo = openingPlacement;
                IfcAxis2Placement3D doorPlacement3D = Document.Model.New<IfcAxis2Placement3D>();
                windowPlacement.RelativePlacement = doorPlacement3D;
                //set opening placement
                doorPlacement3D.SetNewDirectionOf_XZ(
                    0, 1, 0,
                    1, 0, 0);
                doorPlacement3D.SetNewLocation(0, 0, 0);
                _ifcBuildingElement.ObjectPlacement = windowPlacement;
            }

            //create extrusion geometry for the opening
            XbimExtrudedAreaSolid hole = new XbimExtrudedAreaSolid(_document, width, openingWidth, openingHeight, new XbimXYZ(0, 0, 1));

            //create opening object
            IfcOpeningElement opening = Document.Model.New<IfcOpeningElement>();
            opening.ObjectPlacement = openingPlacement;
            IfcProductDefinitionShape representation = Document.Model.New<IfcProductDefinitionShape>();
            opening.Representation = representation;

            IfcShapeRepresentation shapeRepresentation = Document.Model.New<IfcShapeRepresentation>();
            //set context of geometry to the context of the model
            shapeRepresentation.ContextOfItems = Document.Model.IfcProject.ModelContext();
            shapeRepresentation.RepresentationType = "SweptSolid";
            shapeRepresentation.RepresentationIdentifier = "Body";
            shapeRepresentation.Items.Add_Reversible(hole.IfcSweptAreaSolid);
            representation.Representations.Add_Reversible(shapeRepresentation);

            //create relation between door and opening
            IfcRelFillsElement relFil = Document.Model.New<IfcRelFillsElement>();
            relFil.RelatedBuildingElement = IfcBuildingElement;
            relFil.RelatingOpeningElement = opening;

            //create relation between wall and opening
            IfcRelVoidsElement relVoids = Document.Model.New<IfcRelVoidsElement>();
            relVoids.RelatedOpeningElement = opening;
            relVoids.RelatingBuildingElement = wall;
        }
    }
}
