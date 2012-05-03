using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.Extensions;
using Xbim.DOM.PropertiesQuantities;
using Xbim.Ifc.GeometricConstraintResource;

namespace Xbim.DOM
{
    public class XbimSite : XbimSpatialStructureElement
    {
        private IfcSite Site { get { return _spatialElement as IfcSite; } }
        
        //public properties specific for the building
        public int[] RefLatitude 
        {
            get { return Site.RefLatitude == null ? null : new int[4] { Site.RefLatitude.Value[0], Site.RefLatitude.Value[1], Site.RefLatitude.Value[2], Site.RefLatitude.Value[3] }; }
            set { if (value.Length != 4) throw new IndexOutOfRangeException(); else Site.RefLatitude = new IfcCompoundPlaneAngleMeasure(value[0], value[1], value[2], value[3]); }
        }
        public int[] RefLongitude 
        {
            get { return Site.RefLongitude == null ? null : new int[4] { Site.RefLongitude.Value[0], Site.RefLongitude.Value[1], Site.RefLongitude.Value[2], Site.RefLongitude.Value[3] }; }
            set { if (value.Length != 4) throw new IndexOutOfRangeException(); else Site.RefLongitude = new IfcCompoundPlaneAngleMeasure(value[0], value[1], value[2], value[3]); }
        }
        public double? RefLatitude_asDouble 
        {
            get { if (Site.RefLatitude == null) return null; else return Site.RefLatitude.Value.ToDouble(); }
            set { if (value != null) Site.RefLatitude = new IfcCompoundPlaneAngleMeasure(value??0); }
        }
        public double? RefLongitude_asDouble
        {
            get { if (Site.RefLongitude == null) return null; else return Site.RefLongitude.Value.ToDouble(); }
            set { if (value != null) Site.RefLongitude = new IfcCompoundPlaneAngleMeasure(value ?? 0); }
        }
        public double? RefElevation { get { return Site.RefElevation; } set { Site.RefElevation = value; } }
        public XbimSiteCommonProperties CommonProperties { get { return new XbimSiteCommonProperties(Site); } }
        public XbimSiteQuantities Quantities { get { return new XbimSiteQuantities(Site); } }

        //internal constructor for creation from XbimDocument (parsing data into the document)
        internal XbimSite(XbimDocument document, IfcSite site) : base(document, site) { }

        //internal constructor for creation from XbimObjectCreator
        internal XbimSite(XbimDocument document, string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
            : base(document, document.Model.New<IfcSite>())
        {
            Site.Name = name;
            Site.CompositionType = GeIfcElementCompositionEnum(compositionEnum);
            if (parentElement != null) parentElement.AddToSpatialDecomposition(this);

            //add the site to the structure of the project if there is not different parrent object
            if (parentElement == null) _document.Model.IfcProject.AddSite(Site);
            if (Document.ModelView == XbimModelView.CoordinationView)
            {
                IfcLocalPlacement lp = Document.Model.New<IfcLocalPlacement>();
                lp.RelativePlacement = Document.WCS;
                if (parentElement != null)  lp.PlacementRelTo = parentElement.GetObjectPlacement();
                Site.ObjectPlacement = lp;
            }
            Document.Sites.Add(this);
        }
    }
}
