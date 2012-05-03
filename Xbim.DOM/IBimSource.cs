using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.DOM
{
    public interface IBimSource
    {
        int ConvertBeams(IBimTarget target);
        int ConvertBeamTypes(IBimTarget target);

        int ConvertColumns(IBimTarget target);
        int ConvertColumnTypes(IBimTarget target);

        int ConvertCurtainWalls(IBimTarget target);
        int ConvertCurtainWallTypes(IBimTarget target);

        int ConvertCeilings(IBimTarget target);
        int ConvertCeilingTypes(IBimTarget target);

        int ConvertDoors(IBimTarget target);
        int ConvertDoorTypes(IBimTarget target);

        int ConvertFloors(IBimTarget target);
        int ConvertFloorTypes(IBimTarget target);

        int ConvertPlates(IBimTarget target);
        int ConvertPlateTypes(IBimTarget target);

        int ConvertRailings(IBimTarget target);
        int ConvertRailingTypes(IBimTarget target);

        int ConvertRampFlights(IBimTarget target);
        int ConvertRampFlightTypes(IBimTarget target);

        int ConvertRoofs(IBimTarget target);
        int ConvertRoofTypes(IBimTarget target);

        int ConvertSlabs(IBimTarget target);
        int ConvertSlabTypes(IBimTarget target);

        int ConvertStairFlights(IBimTarget target);
        int ConvertStairFlightTypes(IBimTarget target);

        int ConvertWindows(IBimTarget target);
        int ConvertWindowTypes(IBimTarget target);

        int ConvertWalls(IBimTarget target);
        int ConvertWallTypes(IBimTarget target);

        int ConvertMaterials(IBimTarget target);
        int ConvertSpatialStructure(IBimTarget target);

        int ConvertUnconvertedElements(IBimTarget target);
        int ConvertUnconvertedElementTypes(IBimTarget target);
             
        IEnumerable<object> ConvertedObjects { get; }

    }
}
