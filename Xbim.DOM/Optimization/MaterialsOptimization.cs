using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.IO;

namespace Xbim.DOM.Optimization
{
    public static class MaterialsOptimization
    {
        /// <summary>
        /// This function provides a tool to recognise IfcMaterialList that are composed of the same materials.
        /// </summary>
        /// <param name="Model">The model to extract material lists from</param>
        /// <returns>Dictionary associating the entitylabel (value) of an IfcMaterialList that duplicates the composition of the one identified by the entitylabel (Key).</returns>
        public static Dictionary<int, int> IfcMaterialListReplacementDictionary(this XbimModel Model)
        {
            // the resulting dictionary contains information on the replacing ID of any IfcMaterialList that duplicates another of the same composition.
            
            Dictionary<int, int> dic = new Dictionary<int, int>();
            Dictionary<string, int> CompositionDic = new Dictionary<string, int>();
            foreach (var matList in Model.Instances.OfType<IfcMaterialList>())
            {
                List<int> mlist = new List<int>();
                foreach (var item in matList.Materials)
                {
                    mlist.Add(Math.Abs(item.EntityLabel));
                }
                mlist.Sort();
                string stSignature = string.Join(",", mlist.ToArray());
                if (CompositionDic.ContainsKey(stSignature))
                {
                    dic.Add(Math.Abs(matList.EntityLabel), CompositionDic[stSignature]);
                }
                else
                {
                    CompositionDic.Add(stSignature, Math.Abs(matList.EntityLabel));
                    dic.Add(Math.Abs(matList.EntityLabel), Math.Abs(matList.EntityLabel));
                }
            }
            return dic;
        }
    }
}
