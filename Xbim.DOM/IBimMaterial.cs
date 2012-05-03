using System;
namespace Xbim.DOM
{
    public interface IBimMaterial
    {
        string Name { get; set; }
        IBimSingleProperties SingleProperties { get; }
    }
}
