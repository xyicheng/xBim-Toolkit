using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStyler2CustomSetAppearence
    {
        public HashSet<IPersistIfcEntity> Set = new HashSet<IPersistIfcEntity>();
        public ModelGeometry.Scene.XbimTexture Appearence;
    }
}
