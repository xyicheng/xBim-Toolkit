using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Xbim.DOM.ExportHelpers
{
    public class ExportElementTypeHelper
    {
        private XbimDocumentSource _source;
        private XbimDocumentSource Source
        {
            get { return _source; }
        }
        private ExportPropertiesHelper PropertiesHelper
        {
            get { return Source.PropertiesHelper; }
        }
        private IBimTarget Target { get { return Source.Target; } }
        private bool _checkNames = true;


        public ExportElementTypeHelper(XbimDocumentSource source)
        {
            _source = source;
        }

        private void CreateMaterialLayers(IBimBuildingElementType tType, XbimBuildingElementType sType)
        {
            foreach (XbimMaterialLayer layer in sType.MaterialLayers)
            {
                string matName = layer.Material.Name;
                
                IBimMaterial material = Target.GetMaterial(matName);
                if (material == null) material = Source.MaterialHelper.Convert(layer.Material);

                int layerIndex = sType.MaterialLayers.IndexOf(layer);
                tType.AddMaterialLayer(material, layer.Thickness, false, sType.GetMaterialFunction(layerIndex));
            }
        }

        private IBimBuildingElementType BaseConversion(IBimBuildingElementType tType, XbimBuildingElementType sType)
        {
            try
            {
                Debug.WriteLine(sType.Name);
                //set guid
                tType.GlobalId = sType.Guid;

                //set material layers if it exists in the object and object has appropriate type
                CreateMaterialLayers(tType, sType);

                //parameters
                PropertiesHelper.Convert(tType.Properties, sType.Properties);

                //add converted element to the stack of converted elements
                Source.AddConvertedObject(sType);

                return tType;
            }
            catch (Exception e)
            {
                
                //throw new Exception("Error while processing element type '" + sType.Name + "': " + e.Message);
            }
            return null;
            
        }

        public IBimBuildingElementType ConvertBeamType(XbimBeamType sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewBeamType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertColumnType(XbimColumnType sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewColumnType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertCurtainWallType(XbimCurtainWallType sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewCurtainWallType(sType.Name, sType.Description);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertDoorType(XbimDoorStyle sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewDoorType(sType.Name, sType.Description, sType.ConstructionType, sType.OperationType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertSlabType(IBimBuildingElementType type)
        {
            XbimSlabType sType = type as XbimSlabType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewSlabType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertGeneralType(XbimBuildingElementProxyType sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewBuildingElementProxyType(sType.Name, sType.Description);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertRailingType(XbimRailingType sType)
        {
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewRailingType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertRampType(IBimBuildingElementType type)
        {
            XbimRampFlightType sType = type as XbimRampFlightType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewRampFlightType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertStairType(IBimBuildingElementType type)
        {
            XbimStairFlightType sType = type as XbimStairFlightType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewStairFlightType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertWallType(IBimBuildingElementType type)
        {
            XbimWallType sType = type as XbimWallType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewWallType(sType.Name, sType.Description, sType.PredefinedType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertWindowType(IBimBuildingElementType type)
        {
            XbimWindowStyle sType = type as XbimWindowStyle;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewWindowType(sType.Name, sType.Description, sType.ConstructionType, sType.OperationType);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertPlateType(IBimBuildingElementType type)
        {
            XbimPlateType sType = type as XbimPlateType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewCeilingType(sType.Name, sType.Description);
            return BaseConversion(tType, sType);
        }

        public IBimBuildingElementType ConvertCoveringType(IBimBuildingElementType type)
        {
            XbimCoveringType sType = type as XbimCoveringType;
            if (sType == null) throw new ArgumentException("Unexpected type");
            CheckName(sType);
            IBimBuildingElementType tType = Target.NewCeilingType(sType.Name, sType.Description);
            return BaseConversion(tType, sType);
        } 

        //recursive function - check tne uniqueness of the name of the type 
        //in the target document. If the name already exists it is changed
        //in the source model so that it can be used later
        private bool CheckName(XbimBuildingElementType xType) 
        {
            if (!_checkNames) return true;

            string name = xType.Name;
            IBimBuildingElementType tType = Target.GetBuildingElementType(name);
            if (tType != null)
            { 
                int index = name.Length - 2; //ge_t character one before the end
                char testChar = name[index];
                if (testChar == '_')
                {
                    char last = name.LastOrDefault(); //last character
                    int number = 1;
                    if (int.TryParse(last.ToString(), out number))
                    {
                        name = name.Substring(0, name.Length - 1);
                        name += number + 1;
                    }
                    else
                    {
                        name += "_1";
                    }
                }
                else
                {
                    name += "_1";
                }
                xType.Name = name;
                return CheckName(xType);
            }
            return true;
        }


    }
}
