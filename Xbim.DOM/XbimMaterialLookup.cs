using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.DOM
{
    public class XbimMaterialLookup
    {

        public static Dictionary<string, string> RevitLookup;
        static XbimMaterialLookup()
        {
            RevitLookup = new Dictionary<string, string>();
            RevitLookup.Add("Aggregate concrete blocks", "Masonry - Concrete Block");
            RevitLookup.Add("Partial fill cavity insulation", "Insulation / Thermal Barriers - Cavity Fill");
            RevitLookup.Add("Clay facing bricks", "Masonry - Brick");
            RevitLookup.Add("Aerated concrete blocks", "Masonry - Concrete Block");
            RevitLookup.Add("Granite stone blocks", "Stone - Natural");
            RevitLookup.Add("Cavity", "Misc. Air Layers - Air Space");
        }
    }
}
