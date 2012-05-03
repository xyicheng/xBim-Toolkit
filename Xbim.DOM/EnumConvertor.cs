using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.ProductExtension;

namespace Xbim.DOM
{
    public class EnumConvertor<TypeFrom, TypeTo>
    {
        public TypeTo Conversion(TypeFrom value)
        {
            string val = Enum.GetName(typeof(TypeFrom), value);
            TypeTo res = default(TypeTo);
            try
            {
            res = (TypeTo)Enum.Parse(typeof(TypeTo), val, true);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            return res;
        }

        public TypeFrom Conversion(TypeTo value)
        {
            string val = Enum.GetName(typeof(TypeTo), value);
            TypeFrom res = default(TypeFrom);
            try
            {
                res = (TypeFrom)Enum.Parse(typeof(TypeFrom), val, true);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            return res;
        }
    }

    public static class EnumConvertors
    {
        public static XbimWallTypeEnum XbimWallTypeEnum(this IfcWallTypeEnum en)
        {
            EnumConvertor<XbimWallTypeEnum, IfcWallTypeEnum> convertor = new EnumConvertor<XbimWallTypeEnum, IfcWallTypeEnum>();
            return convertor.Conversion(en);
        }

        public static IfcLayerSetDirectionEnum IfcLayerSetDirectionEnum(this XbimLayerSetDirectionEnum en)
        {
            EnumConvertor<IfcLayerSetDirectionEnum, XbimLayerSetDirectionEnum> convertor = new EnumConvertor<Ifc.MaterialResource.IfcLayerSetDirectionEnum, XbimLayerSetDirectionEnum>();
            return convertor.Conversion(en);
        }

        public static IfcPlateTypeEnum IfcPlateTypeEnum(this XbimPlateTypeEnum en)
        {
            EnumConvertor<IfcPlateTypeEnum, XbimPlateTypeEnum> conv = new EnumConvertor<IfcPlateTypeEnum, XbimPlateTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimPlateTypeEnum XbimPlateTypeEnum(this  IfcPlateTypeEnum en)
        {
            EnumConvertor<IfcPlateTypeEnum, XbimPlateTypeEnum> conv = new EnumConvertor<IfcPlateTypeEnum, XbimPlateTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcBeamTypeEnum IfcBeamTypeEnum(this XbimBeamTypeEnum en)
        {
            EnumConvertor<IfcBeamTypeEnum, XbimBeamTypeEnum> conv = new EnumConvertor<IfcBeamTypeEnum, XbimBeamTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimBeamTypeEnum XbimBeamTypeEnum(this IfcBeamTypeEnum en)
        {
            EnumConvertor<IfcBeamTypeEnum, XbimBeamTypeEnum> conv = new EnumConvertor<IfcBeamTypeEnum, XbimBeamTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcStairFlightTypeEnum IfcStairFlightTypeEnum(this XbimStairFlightTypeEnum en)
        {
            EnumConvertor<IfcStairFlightTypeEnum, XbimStairFlightTypeEnum> conv = new EnumConvertor<IfcStairFlightTypeEnum, XbimStairFlightTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimStairFlightTypeEnum XbimStairFlightTypeEnum(this IfcStairFlightTypeEnum en)
        {
            EnumConvertor<IfcStairFlightTypeEnum, XbimStairFlightTypeEnum> conv = new EnumConvertor<IfcStairFlightTypeEnum, XbimStairFlightTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcWindowStyleConstructionEnum IfcWindowStyleConstructionEnum(this XbimWindowStyleConstructionEnum en)
        {
            EnumConvertor<IfcWindowStyleConstructionEnum, XbimWindowStyleConstructionEnum> conv = new EnumConvertor<IfcWindowStyleConstructionEnum, XbimWindowStyleConstructionEnum>();
            return conv.Conversion(en);
        }

        public static XbimWindowStyleConstructionEnum XbimWindowStyleConstructionEnum(this IfcWindowStyleConstructionEnum en)
        {
            EnumConvertor<IfcWindowStyleConstructionEnum, XbimWindowStyleConstructionEnum> conv = new EnumConvertor<IfcWindowStyleConstructionEnum, XbimWindowStyleConstructionEnum>();
            return conv.Conversion(en);
        }

        public static IfcWindowStyleOperationEnum IfcWindowStyleOperationEnum(this XbimWindowStyleOperationEnum en)
        {
            EnumConvertor<IfcWindowStyleOperationEnum, XbimWindowStyleOperationEnum> conv = new EnumConvertor<IfcWindowStyleOperationEnum, XbimWindowStyleOperationEnum>();
            return conv.Conversion(en);
        }

        public static XbimWindowStyleOperationEnum XbimWindowStyleOperationEnum(this IfcWindowStyleOperationEnum en)
        {
            EnumConvertor<IfcWindowStyleOperationEnum, XbimWindowStyleOperationEnum> conv = new EnumConvertor<IfcWindowStyleOperationEnum, XbimWindowStyleOperationEnum>();
            return conv.Conversion(en);
        }

        public static IfcDoorStyleConstructionEnum IfcDoorStyleConstructionEnum(this XbimDoorStyleConstructionEnum en)
        {
            EnumConvertor<IfcDoorStyleConstructionEnum, XbimDoorStyleConstructionEnum> conv = new EnumConvertor<IfcDoorStyleConstructionEnum, XbimDoorStyleConstructionEnum>();
            return conv.Conversion(en);
        }

        public static XbimDoorStyleConstructionEnum XbimDoorStyleConstructionEnum(this IfcDoorStyleConstructionEnum en)
        {
            EnumConvertor<IfcDoorStyleConstructionEnum, XbimDoorStyleConstructionEnum> conv = new EnumConvertor<IfcDoorStyleConstructionEnum, XbimDoorStyleConstructionEnum>();
            return conv.Conversion(en);
        }

        public static IfcDoorStyleOperationEnum IfcDoorStyleOperationEnum(this XbimDoorStyleOperationEnum en)
        {
            EnumConvertor<IfcDoorStyleOperationEnum, XbimDoorStyleOperationEnum> conv = new EnumConvertor<IfcDoorStyleOperationEnum, XbimDoorStyleOperationEnum>();
            return conv.Conversion(en);
        }

        public static XbimDoorStyleOperationEnum XbimDoorStyleOperationEnum (this IfcDoorStyleOperationEnum en)
        {
            EnumConvertor<IfcDoorStyleOperationEnum, XbimDoorStyleOperationEnum> conv = new EnumConvertor<IfcDoorStyleOperationEnum, XbimDoorStyleOperationEnum>();
            return conv.Conversion(en);
        }

        public static IfcColumnTypeEnum IfcColumnTypeEnum(this XbimColumnTypeEnum en)
        {
            EnumConvertor<IfcColumnTypeEnum, XbimColumnTypeEnum> conv = new EnumConvertor<IfcColumnTypeEnum, XbimColumnTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimColumnTypeEnum XbimColumnTypeEnum(this IfcColumnTypeEnum en)
        {
            EnumConvertor<IfcColumnTypeEnum, XbimColumnTypeEnum> conv = new EnumConvertor<IfcColumnTypeEnum, XbimColumnTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcRailingTypeEnum IfcRailingTypeEnum(this XbimRailingTypeEnum en)
        {
            EnumConvertor<IfcRailingTypeEnum, XbimRailingTypeEnum> conv = new EnumConvertor<IfcRailingTypeEnum, XbimRailingTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimRailingTypeEnum XbimRailingTypeEnum(this IfcRailingTypeEnum en)
        {
            EnumConvertor<IfcRailingTypeEnum, XbimRailingTypeEnum> conv = new EnumConvertor<IfcRailingTypeEnum, XbimRailingTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcRampFlightTypeEnum IfcRampFlightTypeEnum(this XbimRampFlightTypeEnum en)
        {
            EnumConvertor<IfcRampFlightTypeEnum, XbimRampFlightTypeEnum> conv = new EnumConvertor<IfcRampFlightTypeEnum, XbimRampFlightTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimRampFlightTypeEnum XbimRampFlightTypeEnum(this IfcRampFlightTypeEnum en)
        {
            EnumConvertor<IfcRampFlightTypeEnum, XbimRampFlightTypeEnum> conv = new EnumConvertor<IfcRampFlightTypeEnum, XbimRampFlightTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcSlabTypeEnum IfcSlabTypeEnum(this XbimSlabTypeEnum en)
        {
            EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum> conv = new EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimSlabTypeEnum XbimSlabTypeEnum(this IfcSlabTypeEnum en)
        {
            EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum> conv = new EnumConvertor<IfcSlabTypeEnum, XbimSlabTypeEnum>();
            return conv.Conversion(en);
        }

        public static XbimCoveringTypeEnum XbimCoveringTypeEnum(this IfcCoveringTypeEnum en)
        {
            EnumConvertor<IfcCoveringTypeEnum, XbimCoveringTypeEnum> conv = new EnumConvertor<IfcCoveringTypeEnum, XbimCoveringTypeEnum>();
            return conv.Conversion(en);
        }

        public static IfcCoveringTypeEnum IfcCoveringTypeEnum(this XbimCoveringTypeEnum en)
        {
            EnumConvertor<IfcCoveringTypeEnum, XbimCoveringTypeEnum> conv = new EnumConvertor<IfcCoveringTypeEnum, XbimCoveringTypeEnum>();
            return conv.Conversion(en);
        }
    }

    public enum XbimLayerSetDirectionEnum
    {
        AXIS1,
        AXIS2,
        AXIS3
    }

    public enum XbimDirectionSenseEnum
    {
        POSITIVE,
        NEGATIVE
    }

    public enum XbimSlabTypeEnum
    {
        FLOOR,
        ROOF,
        LANDING,
        BASESLAB,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimWallTypeEnum
    {
        STANDARD,
        POLYGONAL,
        SHEAR,
        ELEMENTEDWALL,
        PLUMBINGWALL,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimPlateTypeEnum
    {
        CURTAIN_PANEL,
        SHEET,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimRoofTypeEnum
    {
        FLAT_ROOF,
        SHED_ROOF,
        GABLE_ROOF,
        HIP_ROOF,
        HIPPED_GABLE_ROOF,
        GAMBREL_ROOF,
        MANSARD_ROOF,
        BARREL_ROOF,
        RAINBOW_ROOF,
        BUTTERFLY_ROOF,
        PAVILION_ROOF,
        DOME_ROOF,
        FREEFORM,
        NOTDEFINED
    }

    public enum XbimBeamTypeEnum
    {
        BEAM,
        JOIST,
        LINTEL,
        T_BEAM,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimStairFlightTypeEnum
    {
        STRAIGHT,
        WINDER,
        SPIRAL,
        CURVED,
        FREEFORM,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimWindowStyleConstructionEnum
    {
        ALUMINIUM,
        HIGH_GRADE_STEEL,
        STEEL,
        WOOD,
        ALUMINIUM_WOOD,
        PLASTIC,
        OTHER_CONSTRUCTION,
        NOTDEFINED
    }

    public enum XbimWindowStyleOperationEnum
    {
        SINGLE_PANEL,
        DOUBLE_PANEL_VERTICAL,
        DOUBLE_PANEL_HORIZONTAL,
        TRIPLE_PANEL_VERTICAL,
        TRIPLE_PANEL_BOTTOM,
        TRIPLE_PANEL_TOP,
        TRIPLE_PANEL_LEFT,
        TRIPLE_PANEL_RIGHT,
        TRIPLE_PANEL_HORIZONTAL,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimDoorStyleOperationEnum
    {
        SINGLE_SWING_LEFT,
        SINGLE_SWING_RIGHT,
        DOUBLE_DOOR_SINGLE_SWING,
        DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_LEFT,
        DOUBLE_DOOR_SINGLE_SWING_OPPOSITE_RIGHT,
        DOUBLE_SWING_LEFT,
        DOUBLE_SWING_RIGHT,
        DOUBLE_DOOR_DOUBLE_SWING,
        SLIDING_TO_LEFT,
        SLIDING_TO_RIGHT,
        DOUBLE_DOOR_SLIDING,
        FOLDING_TO_LEFT,
        FOLDING_TO_RIGHT,
        DOUBLE_DOOR_FOLDING,
        REVOLVING,
        ROLLINGUP,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimDoorStyleConstructionEnum
    {
        ALUMINIUM,
        HIGH_GRADE_STEEL,
        STEEL,
        WOOD,
        ALUMINIUM_WOOD,
        ALUMINIUM_PLASTIC,
        PLASTIC,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimColumnTypeEnum
    {
        COLUMN,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimRailingTypeEnum
    {
        HANDRAIL,
        GUARDRAIL,
        BALUSTRADE,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimRampFlightTypeEnum
    {
        STRAIGHT,
        SPIRAL,
        USERDEFINED,
        NOTDEFINED
    }

    public enum XbimValueTypeEnum
    {
        INTEGER,
        REAL,
        BOOLEAN,
        STRING
    }

    public enum XbimCoveringTypeEnum
    {
        CEILING,
        FLOORING,
        CLADDING,
        ROOFING,
        INSULATION,
        MEMBRANE,
        SLEEVING,
        WRAPPING,
        USERDEFINED,
        NOTDEFINED
    }
}
