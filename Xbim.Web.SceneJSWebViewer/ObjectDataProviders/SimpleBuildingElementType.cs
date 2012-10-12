using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using System.Collections.Specialized;
using Xbim.XbimExtensions;

namespace Xbim.SceneJSWebViewer.ObjectDataProviders
{
    public class SimpleBuildingElementType : INotifyPropertyChanged
    {
        private IfcTypeProduct _type;
        //private CarbonData _carbonData;
        //private Dictionary<IfcBuildingElement, CarbonData> _elementsCarbon;
        private IModel _model { get { return (_type as IPersistIfcEntity).ModelOf; } }

        public IfcTypeProduct IfcType { get { return _type; } }
        //public CarbonData CarbonData { get { return _carbonData; } }
        //public Dictionary<IfcBuildingElement, CarbonData> ElementsCarbonData { get { return _elementsCarbon; } }
        public string Name { get { return _type.Name; } }

        public SimpleBuildingElementType(IfcTypeProduct type)
        {
            _type = type;
            Init();
        }

        private void Init()
        {
            //CarbonContentAnalyser analyser = new CarbonContentAnalyser(_model);
            //_elementsCarbon = analyser.GetCO2Data(GetElements());
            //_carbonData = new CarbonData();

            //foreach (var pair in _elementsCarbon)
            //{
            //    _carbonData.Add(pair.Value);
            //}
            //ComputeCarbon();
        }

        //create sum of all carbon data of the elements of this type from the prepaired dictionary
        private void ComputeCarbon()
        {
            //_carbonData = new CarbonData();
            //foreach (var item in _elementsCarbon)
            //{
            //    _carbonData.Add(item.Value);
            //}
            //OnPropertyChanged("CarbonData");
        }

        private IEnumerable<IfcBuildingElement> GetElements()
        {
            IModel model = (_type as IPersistIfcEntity).ModelOf;
            IEnumerable<IfcRelDefinesByType> rels = model.InstancesWhere<IfcRelDefinesByType>(r => r.RelatingType == _type);
            if (rels.FirstOrDefault() == null) //we need at least one relation to listen to
            {
                //using (Transaction trans = model.BeginTransaction("RelDefinesByType for element type "+ _type.Name)){
                IfcRelDefinesByType newRel = model.New<IfcRelDefinesByType>(r => r.RelatingType = _type);
                rels = new List<IfcRelDefinesByType>();
                (rels as List<IfcRelDefinesByType>).Add(newRel);
                //    trans.Commit();
                //}
            }
            foreach (var rel in rels)
            {
                IEnumerable<IfcBuildingElement> elements = rel.RelatedObjects.OfType<IfcBuildingElement>();
                (rel as INotifyPropertyChanged).PropertyChanged -= new PropertyChangedEventHandler(RelationChanged);
                (rel as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(RelationChanged);

                (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged -= new NotifyCollectionChangedEventHandler(ElementCollectionChanged);
                (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ElementCollectionChanged);
                foreach (var element in elements)
                {
                    yield return element;
                }
            }
        }

        //partial changes in underlying data
        void ElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    //CarbonContentAnalyser analyser = new CarbonContentAnalyser(_model);
                    foreach (var item in e.NewItems)
                    {
                        IfcBuildingElement element = item as IfcBuildingElement;
                        if (element == null) continue;
                        //CarbonData carbonData = analyser.GetCO2Data(element);
                        //if (!_elementsCarbon.ContainsKey(element))
                        //    _elementsCarbon.Add(element, carbonData);
                    }
                    ComputeCarbon();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        IfcBuildingElement element = item as IfcBuildingElement;
                        //if (element == null) continue;
                        //_elementsCarbon.Remove(element);
                    }
                    ComputeCarbon();
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException();
                case NotifyCollectionChangedAction.Reset:
                    Init();
                    break;
                default:
                    break;
            }
        }

        void RelationChanged(object sender, PropertyChangedEventArgs e)
        {
            //this would mean major change and it is necessary to recompute everything
            if (e.PropertyName == "RelatingType" || e.PropertyName == "RelatedObjects")
            {
                IfcRelDefinesByType rel = sender as IfcRelDefinesByType;
                IfcTypeProduct type = rel.RelatingType as IfcTypeProduct;
                if (type == null) return;

                _type = type;
                Init();

                OnPropertyChanged("Name");
                OnPropertyChanged("Elements");
                OnPropertyChanged("ElementsCarbonData");
                OnPropertyChanged("CarbonData");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}