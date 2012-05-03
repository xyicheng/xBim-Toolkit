#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    WallExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.SharedBldgElements;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class WallExtensions
    {
        /// <summary>
        ///   Set Material set usage with typical values and creates it if it doesn't exist.
        ///   LayerSetDirection = IfcLayerSetDirectionEnum.AXIS1
        ///   DirectionSense = IfcDirectionSenseEnum.POSITIVE
        ///   OffsetFromReferenceLine = 0
        /// </summary>
        /// <param name = "forLayerSet">Material layer set for the usage</param>
        public static void SetTypicalMaterialLayerSetUsage(this IfcWall wall, IfcMaterialLayerSet forLayerSet)
        {
            wall.SetMaterialLayerSetUsage(forLayerSet, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
        }
    }
}