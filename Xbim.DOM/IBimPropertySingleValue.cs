using System;
namespace Xbim.DOM
{
    public interface IBimPropertySingleValue
    {
        string Description { get; set; }
        string Name { get; set; }
        string PsetName { get; }
        XbimValueTypeEnum Type { get; set; }
        object Value { get; set; }
    }
}
