using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public class XbimSpace : XbimSpatialStructureElement
    {
        private IfcSpace Space { get { return _spatialElement as IfcSpace; } }

        //public properties specific for the space
        public bool? IsInternal 
        {
            get 
            {
                IfcSpace space = this._spatialElement as IfcSpace;
                switch (space.InteriorOrExteriorSpace)
                {
                    case IfcInternalOrExternalEnum.EXTERNAL: return false;
                    case IfcInternalOrExternalEnum.INTERNAL: return true;
                    default: return null;
                }
            }
        }
        public double ElevationWithFlooring { get { return Space.ElevationWithFlooring.Value; } set { Space.ElevationWithFlooring = value; } }
        public XbimSpaceCommonProperties CommonProperties { get { return new XbimSpaceCommonProperties(Space); } }
        public XbimSpaceQuantities Quantities { get { return new XbimSpaceQuantities(Space); } }
        public XbimInterExterEnum InteriorOrExteriorSpace { get { return GetXbimInternalOrExternalEnum(Space.InteriorOrExteriorSpace); } set { Space.InteriorOrExteriorSpace = GetIfcInternalOrExternalEnum(value); } }

        //internal constructor for creation from XbimDocument (parsing data into the document)
        internal XbimSpace(XbimDocument document, IfcSpace space) : base(document, space) { }

        //internal constructor for creation from XbimObjectCreator
        internal XbimSpace(XbimDocument document, string name, XbimSpatialStructureElement parentElement, XbimElementCompositionEnum compositionEnum)
            : base(document, document.Model.New<IfcSpace>())
        {
            Space.CompositionType = GeIfcElementCompositionEnum(compositionEnum);
            Space.Name = name;
            if (parentElement != null) parentElement.AddToSpatialDecomposition(this);
            Document.Spaces.Add(this);
        }


        private IfcInternalOrExternalEnum GetIfcInternalOrExternalEnum(XbimInterExterEnum enu)
        {
            switch (enu)
            {
                case XbimInterExterEnum.EXTERNAL: return IfcInternalOrExternalEnum.EXTERNAL;
                case XbimInterExterEnum.INTERNAL: return IfcInternalOrExternalEnum.INTERNAL;
                case XbimInterExterEnum.NOTDEFINED: return IfcInternalOrExternalEnum.NOTDEFINED;
            }
            return IfcInternalOrExternalEnum.NOTDEFINED;
        }

        private XbimInterExterEnum GetXbimInternalOrExternalEnum(IfcInternalOrExternalEnum enu)
        {
            switch (enu)
            {
                case IfcInternalOrExternalEnum.EXTERNAL: return XbimInterExterEnum.EXTERNAL;
                case IfcInternalOrExternalEnum.INTERNAL: return XbimInterExterEnum.INTERNAL;
                case IfcInternalOrExternalEnum.NOTDEFINED: return XbimInterExterEnum.NOTDEFINED;
            }
            return XbimInterExterEnum.NOTDEFINED;
        }  

        public void AddBoundingElement(XbimBuildingElement element, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external)
        {
            IfcElement el = element.IfcBuildingElement;
            if (el == null) return;
            Space.AddBoundingElement(_document.Model, el, GetIfcPhysicalOrVirtualEnum(type), GetIfcInternalOrExternalEnum(external));
        }

        public void AddBoundingElement(Guid guid, XbimPhysicalOrVirtualEnum type, XbimInternalOrExternalEnum external)
        {
            IfcGloballyUniqueId gid = new IfcGloballyUniqueId(guid);
            IfcElement element = _document.Model.InstancesWhere<IfcBuildingElement>(elem => elem.GlobalId == gid).FirstOrDefault();
            if (element == null) return;
            Space.AddBoundingElement(_document.Model, element, GetIfcPhysicalOrVirtualEnum(type), GetIfcInternalOrExternalEnum(external));
        }

        private IfcPhysicalOrVirtualEnum GetIfcPhysicalOrVirtualEnum(XbimPhysicalOrVirtualEnum type)
        {
            switch (type)
            {
                case XbimPhysicalOrVirtualEnum.NOTDEFINED: return IfcPhysicalOrVirtualEnum.NOTDEFINED;
                case XbimPhysicalOrVirtualEnum.PHYSICAL: return IfcPhysicalOrVirtualEnum.PHYSICAL;
                case XbimPhysicalOrVirtualEnum.VIRTUAL: return IfcPhysicalOrVirtualEnum.VIRTUAL;
            }
            throw new Exception("Not defined");
        }

        private IfcInternalOrExternalEnum GetIfcInternalOrExternalEnum(XbimInternalOrExternalEnum external)
        {
            switch (external)
            {
                case XbimInternalOrExternalEnum.EXTERNAL: return IfcInternalOrExternalEnum.EXTERNAL;
                case XbimInternalOrExternalEnum.INTERNAL: return IfcInternalOrExternalEnum.INTERNAL;
                case XbimInternalOrExternalEnum.NOTDEFINED: return IfcInternalOrExternalEnum.NOTDEFINED;
            }
            throw new Exception("Not defined");
        }
    }

    public enum XbimInterExterEnum
    {
        INTERNAL,
        EXTERNAL,
        NOTDEFINED
    }

    public enum XbimPhysicalOrVirtualEnum
    {
        NOTDEFINED,
        PHYSICAL,
        VIRTUAL
    }

    public enum XbimInternalOrExternalEnum
    {
        INTERNAL,
        EXTERNAL,
        NOTDEFINED
    }

    

    

}
