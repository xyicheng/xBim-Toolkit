using System;
namespace Xbim.DOM
{
    public interface INRMQuantities
    {
        double? Area { get; set; }
        double? Count { get; set; }
        double? Length { get; set; }
        double? Number { get; set; }
        double? Volume { get; set; }
    }
}
