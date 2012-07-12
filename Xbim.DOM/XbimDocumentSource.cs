using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.DOM.ExportHelpers;
using System.Collections.ObjectModel;
using Xbim.Ifc.UtilityResource;
using System.Diagnostics;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM
{
    public class XbimDocumentSource :XbimDocument, IBimSource
    {
        #region fields
        protected IBimTarget _target;
        public IBimTarget Target { get { return _target; } }

        private ExportMaterialHelper _materialHelper;
        public ExportMaterialHelper MaterialHelper
        {
            get { return _materialHelper; }
        }

        private ExportPropertiesHelper _propHelper;
        public ExportPropertiesHelper PropertiesHelper
        {
            get { return _propHelper; }
        }

        private ExportElementTypeHelper _elTypeHelper;
        public ExportElementTypeHelper ElementTypeHelper
        {
            get { return _elTypeHelper; }
        }

        private ExportElementHelper _elementHelper;
        public ExportElementHelper ElementHelper
        {
            get { return _elementHelper;}
        }

        #endregion

        #region Constructors
        public XbimDocumentSource(string fileName) : base(fileName) { Init(); }

        public XbimDocumentSource(IModel model) : base(model) { Init(); }

        private void Init()
        {
            _materialHelper = new ExportMaterialHelper(this);
            _converted = new ConvertedElements();
            _propHelper = new ExportPropertiesHelper();
            _elTypeHelper = new ExportElementTypeHelper(this);
            _elementHelper = new ExportElementHelper(this);
        }
        #endregion


        public int ConvertBeams(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertBeamTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimBeamType type in BeamTypes)
            {
                if (ElementTypeHelper.ConvertBeamType(type) != null) count++;
            }
            return count;
        }

        public int ConvertColumns(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertColumnTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimColumnType type in ColumnTypes)
            {
                if (ElementTypeHelper.ConvertColumnType(type) != null) count++;
            }
            return count;
        }

        public int ConvertCurtainWalls(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertCurtainWallTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimCurtainWallType type in CurtainWallTypes)
            {
                if (ElementTypeHelper.ConvertCurtainWallType(type) != null) count++;
            }
            return count;
        }

        public int ConvertCeilings(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (var element in Coverings)
            {
                if (ElementHelper.ConvertCovering(element) != null) count++;
            }
            return count;
        }

        public int ConvertCeilingTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (var type in CoveringTypes)
            {
                if (ElementTypeHelper.ConvertCoveringType(type) != null) count++;
            }
            return count;
        }

        public int ConvertDoors(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertDoorTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimDoorStyle type in DoorStyles)
            {
                if (ElementTypeHelper.ConvertDoorType(type) != null) count++;
            }
            return count;
        }

        public int ConvertFloors(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertFloorTypes(IBimTarget target)
        {
            _target = target;
            Debug.WriteLine("XbimDocumentSource: ConvertFloorTypes: No such a concept in IFC");
            return 0;
        }

        public int ConvertPlates(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertPlateTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimPlateType type in PlateTypes)
            {
                if (ConvertedObjects.Contains(type)) continue;
                if (ElementTypeHelper.ConvertPlateType(type) != null) count++;
            }
            return count;
        }

        public int ConvertRailings(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertRailingTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimRailingType type in RailingTypes)
            {
                if (ElementTypeHelper.ConvertRailingType(type) != null) count++;
            }
            return count;
        }

        public int ConvertRampFlights(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertRampFlightTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimRailingType type in RailingTypes)
            {
                if (ElementTypeHelper.ConvertRailingType(type) != null) count++;
            }
            return count;
        }

        public int ConvertRoofs(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertRoofTypes(IBimTarget target)
        {
            _target = target;
            Debug.WriteLine("XbimDocumentSource: ConvertRoofTypes: Not used in Xbim.DOM. Slab type with predefined type \"ROOF\" is used instead of that.");
            return 0;
        }

        public int ConvertSlabs(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (var element in Slabs)
            {
                if (ElementHelper.ConvertSlab(element) != null) count++;
            }
            return count;
        }

        public int ConvertSlabTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimSlabType type in SlabTypes)
            {
                if (ElementTypeHelper.ConvertSlabType(type) != null) count++;
            }
            return count;
        }

        public int ConvertStairFlights(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertStairFlightTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimStairFlightType type in StairFlightTypes)
            {
                if (ElementTypeHelper.ConvertStairType(type) != null) count++;
            }
            return count;
        }

        public int ConvertWindows(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertWindowTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimWindowStyle type in WindowStyles)
            {
                if (ElementTypeHelper.ConvertWindowType(type) != null) count++;
            }
            return count;
        }

        public int ConvertWalls(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (var element in Walls)
            {
                if (ElementHelper.ConvertWall(element) != null) count++;
            }
            return count;
        }

        public int ConvertWallTypes(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimWallType type in WallTypes)
            {
                if (ElementTypeHelper.ConvertWallType(type) != null) count++;
            }
            return count;
        }

        public int ConvertMaterials(IBimTarget target)
        {
            _target = target;
            int count = 0;
            foreach (XbimMaterial material in Materials)
            {
                if (MaterialHelper.Convert(material) != null) count++;
            }
            return count;
        }

        public int ConvertSpatialStructure(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertUnconvertedElements(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public int ConvertUnconvertedElementTypes(IBimTarget target)
        {
            _target = target;
            throw new NotImplementedException();
        }

        public IEnumerable<object> ConvertedObjects
        {
            get { foreach (var element in _converted) { yield return element; } }
        }

        #region infrastructure to keep track about converted elements
        private ConvertedElements _converted;
        internal void AddConvertedObject(object Object) {_converted.Add(Object);}
        internal bool IsConverted(object Object) { return _converted.Contains(Object);}
        private class ConvertedElements : KeyedCollection<string, object>
        {
            protected override string GetKeyForItem(object item)
            {
                IXbimRoot root = item as IXbimRoot;
                if (root != null) return root.GlobalId;

                XbimMaterial material = item as XbimMaterial;
                if (material != null) return material.Name;
                
                return item.ToString();
            }
        }
        #endregion

    }
}
