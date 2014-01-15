using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;

namespace Xbim.Analysis.Comparing
{
    public class ComparisonManager
    {
        private List<IModelComparerII> _comparers = new List<IModelComparerII>();
        private ComparisonResultsCollection _results = new ComparisonResultsCollection();
        private XbimModel _baseModel;
        private XbimModel _revisedModel;

        public ComparisonResultsCollection Results { get { return _results; } }

        public ComparisonManager(XbimModel baseModel, XbimModel revisedModel)
        {
            _baseModel = baseModel;
            _revisedModel = revisedModel;
        }


        public void AddComparer(IModelComparerII comparer)
        {
            if (comparer == null) throw new ArgumentNullException();
            if (!_comparers.Contains(comparer))
                _comparers.Add(comparer);

        }

        public void Compare<T>() where T : IfcRoot
        {
            _results = new ComparisonResultsCollection();
            var baselineRoots = _baseModel.Instances.OfType<T>();

            //perform all the comparisons in parallel
            //foreach (var comparer in _comparers)
            Parallel.ForEach<IModelComparerII>(_comparers, comparer =>
            {
                foreach (var root in baselineRoots)
                {
                    var result = comparer.Compare<T>(root, _revisedModel);
                    //result can be null if there is no sense in comparison
                    //like comparison of geometry when there is no geometry at all
                    if (result != null)
                        _results.Add(result);   
                }

                //get objects which are supposed to be new
                var residuum = comparer.GetResidualsFromRevision<T>(_revisedModel);
                _results.Add(residuum);
            }
            );
        }

        public void SaveResultToCSV(string path)
        {
            var file = File.CreateText(path);

            //create header
            file.Write("{0},{1},{2}","Type", "Label", "Name");
            foreach (var cmp in _comparers)
            {
                file.Write(",{0} (Weight: {1})", cmp.ComparisonType, cmp.Weight);
            }
            file.Write(",Overall weight\n");

            //write content
            foreach (var item in _results)
            {
                var label = item.Root != null ? Math.Abs(item.Root.EntityLabel).ToString() : "-";
                var type = item.Root != null ? item.Root.GetType().Name : "-";
                var name = item.Root != null ? item.Root.Name.ToString() : "";
                file.Write("{0},#{1},{2}", type, label, name);
                foreach (var cmp in _comparers)
                {
                    var result = item.Results.Where(r => r.Comparer == cmp).FirstOrDefault();
                    file.Write("," + (result!= null ? result.ResultType.ToString() : "-"));
                }
                file.Write("," + item.Weight + "\n");
            }
            file.Close();
        }

    }

    /// <summary>
    /// Keyed collection of comparison results. ModelComparers are used as a key of the collection.
    /// </summary>
    public class ComparisonResultsCollection : List<ComparisonResults>
    {
        public void Add(ComparisonResult result)
        {
            var baseline = result.Baseline;
            var item = this.Where(i => i.Root == baseline).FirstOrDefault();
            if (item != null)
                item.Add(result);
            else
            {
                var cr = new ComparisonResults();
                cr.Add(result);
                this.Add(cr);
            }
        }
    }

    public class ComparisonResults
    {
        private List<ComparisonResult> _results = new List<ComparisonResult>();
        public IEnumerable<ComparisonResult> Results
        {
            get
            {
                foreach (var item in _results)
                {
                    yield return item;
                }
            }
        }

        public IfcRoot Root
        {
            get
            {
                var first = _results.FirstOrDefault();
                return first != null ? _results.FirstOrDefault().Baseline : null;
            }
        }

        public void Add(ComparisonResult result)
        {
            if (Root == null)
            {
                _results.Add(result);
                return;
            }
            if (Root == result.Baseline)
                _results.Add(result);
            else
                throw new ArgumentException("Result must have the same baseline object");
        }

        public bool IsOneToOne { get { return _results.TrueForAll(r => r.IsOneToOne); } }
        public bool IsOnlyBaseLine { get { return _results.TrueForAll(r => r.IsOnlyBaseLine); } }
        public bool IsOnlyInRevision { get { return _results.TrueForAll(r => r.IsOnlyInRevision); } }
        public bool IsAmbiguous { get { return _results.Any(r => r.IsAmbiguous); } }

        public int Weight 
        { 
            get 
            {
                var result = 0;
                foreach (var item in _results)
                    if (item.IsOneToOne)
                        result += item.Comparer.Weight;
                return result;
            }
        }
    }
}
