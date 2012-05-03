using System;
namespace Xbim.DOM
{
    public interface IBimLocalPlacement
    {
        void SetDirectionOf_XZ(double X_axisDirection_X, double X_axisDirection_Y, double X_axisDirection_Z, double Z_axisDirection_X, double Z_axisDirection_Y, double Z_axisDirection_Z);
        void SetLocation(double X, double Y, double Z);
        void SetPlacementRelTo(IBimLocalPlacement LocalPlacement);
    }
}
