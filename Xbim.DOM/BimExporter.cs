using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.DOM
{
    public class BimExporter
    {
        public static int Convert(IBimSource source, IBimTarget target)
        {
            int result = 0;
            result += source.ConvertBeams(target);
            result += source.ConvertBeamTypes( target);
            result += source.ConvertColumns( target);
            result += source.ConvertColumnTypes( target);
            result += source.ConvertCurtainWalls( target);
            result += source.ConvertCurtainWallTypes( target);
            result += source.ConvertCeilings( target);
            result += source.ConvertCeilingTypes( target);
            result += source.ConvertDoors( target);
            result += source.ConvertDoorTypes( target);
            result += source.ConvertFloors( target);
            result += source.ConvertFloorTypes( target);
            result += source.ConvertPlates( target);
            result += source.ConvertPlateTypes( target);
            result += source.ConvertRailings( target);
            result += source.ConvertRailingTypes( target);
            result += source.ConvertRampFlights( target);
            result += source.ConvertRampFlightTypes( target);
            result += source.ConvertRoofs( target);
            result += source.ConvertRoofTypes( target);
            result += source.ConvertSlabs( target);
            result += source.ConvertSlabTypes( target);
            result += source.ConvertStairFlights( target);
            result += source.ConvertStairFlightTypes( target);
            result += source.ConvertWindows( target);
            result += source.ConvertWindowTypes( target);
            result += source.ConvertWalls( target);
            result += source.ConvertWallTypes( target);
            result += source.ConvertMaterials( target);
            result += source.ConvertUnconvertedElements( target);
            //spatial structure to be converted at the end to be able to reference contained or bounding elements
            result += source.ConvertSpatialStructure(target);

            return result;
        }
    }
}
