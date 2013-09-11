using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis.Comparitors
{
    public class NameComparison : IModelComparer
    {
        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IfcProduct, ChangeType> Compare(IEnumerable<IfcProduct> Baseline, IEnumerable<IfcProduct> Delta)
        {
            results.Clear();

            var baseline = new List<IfcProduct>(Baseline);
            var delta = new List<IfcProduct>(Delta);

            Match(baseline, delta);
            Match(delta, baseline, false);

            foreach (var i in baseline)
            {
                results.Add(i, ChangeType.Deleted);
            }
            foreach (var i in delta)
            {
                results.Add(i, ChangeType.Added);
            }

            return results;
        }
        private Dictionary<IfcProduct, ChangeType> results = new Dictionary<IfcProduct, ChangeType>();
        private void Match(List<IfcProduct> start, List<IfcProduct> delta, bool ReturnMappingFromBaseline=true)
        {
            var collection = new List<IfcProduct>(start);
            foreach (var i in collection)
            {
                var b = delta.Where(x => x.Name == i.Name && x.GetType() == i .GetType());
                if (b.Count() == 1) //if we have only 1 result, it should be a match
                {
                    var j = b.First();
                    if (!results.ContainsKey(j))
                    {
                        results.Add(j, ChangeType.Matched);
                        if(ReturnMappingFromBaseline)
                            map.Add(i.EntityLabel, j.EntityLabel);
                        else
                            map.Add(j.EntityLabel, i.EntityLabel);
                    }
                    delta.Remove(j);
                    start.Remove(i);
                }
                else if (b.Count() > 1)
                { // if we have multiple matches
                    foreach (var j in b)
                    {
                        if(!results.ContainsKey(j))
                            results.Add(j, ChangeType.Unknown);
                    }
                }
            }
        }
    }
}
