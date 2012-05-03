using System;
namespace Xbim.DOM
{
    public interface IBimAxis2Placement3D
    {
        bool IsValid { get; }
        void SetDirections(XbimXYZ X_axisDirection, XbimXYZ Z_axisDirection);
        void SetLocation(double X, double Y, double Z);
        void SetLocation(XbimXYZ location);
    }
}
