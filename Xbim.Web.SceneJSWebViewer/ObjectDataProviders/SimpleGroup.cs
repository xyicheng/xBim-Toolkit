using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.ComponentModel;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Transactions;
using System.Collections.Specialized;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;

namespace Xbim.SceneJSWebViewer.ObjectDataProviders
{
    public class SimpleGroup : IHierarchyData, INotifyPropertyChanged
    {
        private IfcGroup _group;
        public IfcGroup IfcGroup { get { return _group; } }
        public string IfcID { get { return _group.GlobalId; } }
        public string Name { get { return _group.Name; } }
        public string Description { get { return _group.Description; } }

        private SimpleGroup _parent;
        private List<SimpleGroup> _children;
        private List<SimpleBuildingElementType> _buildingElementTypes;
        private XbimModel _model { get { return ((_group as IPersistIfcEntity).ModelOf) as XbimModel; } }

        public SimpleGroup Parent { get { return _parent; } }
        public IEnumerable<SimpleGroup> Children { get { foreach (var item in _children) yield return item; } }
        public IEnumerable<SimpleBuildingElementType> BuildingElementsTypes { get { foreach (var item in _buildingElementTypes) yield return item; } }

        public SimpleGroup(IfcGroup group, SimpleGroup parent)
        {
            if (group == null) throw new ArgumentNullException();
            _group = group;
            _parent = parent;

            Init();
        }

        private void Init()
        {
            _children = new List<SimpleGroup>();
            _buildingElementTypes = new List<SimpleBuildingElementType>();

            IEnumerable<IfcRelAssignsToGroup> rels = _model.Instances.Where<IfcRelAssignsToGroup>(r => r.RelatingGroup == _group).ToList();
            if (rels.FirstOrDefault() == null)
            {
                using (XbimReadWriteTransaction trans = _model.BeginTransaction("Group relation creation"))
                {
                    var rel = _model.Instances.New<IfcRelAssignsToGroup>(r => r.RelatingGroup = _group);
                    trans.Commit();
                    (rel as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(RelationChanged);
                    (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged -= new NotifyCollectionChangedEventHandler(ElementCollectionChanged);
                    (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ElementCollectionChanged);
                }

            }
            else
            {
                foreach (var rel in rels)
                {
                    (rel as INotifyPropertyChanged).PropertyChanged += new PropertyChangedEventHandler(RelationChanged);
                    (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged -= new NotifyCollectionChangedEventHandler(ElementCollectionChanged);
                    (rel.RelatedObjects as INotifyCollectionChanged).CollectionChanged += new NotifyCollectionChangedEventHandler(ElementCollectionChanged);

                    IEnumerable<IfcGroup> groups = rel.RelatedObjects.OfType<IfcGroup>();
                    foreach (IfcGroup group in groups)
                    {
                        SimpleGroup child = new SimpleGroup(group, this);
                        child.PropertyChanged += new PropertyChangedEventHandler(ChildChanged);
                        _children.Add(child);
                    }

                    IEnumerable<IfcTypeProduct> types = rel.RelatedObjects.OfType<IfcTypeProduct>();
                    foreach (var type in types)
                    {
                        SimpleBuildingElementType simpleType = new SimpleBuildingElementType(type);
                        simpleType.PropertyChanged -= new PropertyChangedEventHandler(TypeChanged);
                        simpleType.PropertyChanged += new PropertyChangedEventHandler(TypeChanged);
                        _buildingElementTypes.Add(simpleType);
                    }
                }
            }

        }

        void ElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        IfcTypeProduct type = item as IfcTypeProduct;
                        if (type == null) continue;
                        //find existing in the structure
                        SimpleBuildingElementType simpleType = GetTypeFromHierarchy(type);

                        //if it does not exist than create new one
                        if (simpleType == null)
                            simpleType = new SimpleBuildingElementType(type);
                        _buildingElementTypes.Add(simpleType);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        IfcTypeProduct type = item as IfcTypeProduct;
                        if (type == null) continue;

                        SimpleBuildingElementType simpleType = _buildingElementTypes.Where(st => st.IfcType == type).FirstOrDefault();
                        if (simpleType != null) _buildingElementTypes.Remove(simpleType);
                    }

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

        private SimpleBuildingElementType GetTypeFromHierarchy(IfcTypeProduct type)
        {
            return (GetTypeFromHierarchy(type, Root));
        }

        private static SimpleBuildingElementType GetTypeFromHierarchy(IfcTypeProduct type, SimpleGroup group)
        {
            SimpleBuildingElementType simpleType = group._buildingElementTypes.Where(t => t.IfcType == type).FirstOrDefault();
            if (simpleType != null) return simpleType;
            foreach (var subGroup in group._children)
            {
                simpleType = GetTypeFromHierarchy(type, subGroup);
                if (simpleType != null) return simpleType;
            }
            return null;
        }

        private SimpleGroup Root
        {
            get
            {
                if (Parent == null) return this;
                return Parent.Root;
            }
        }

        void RelationChanged(object sender, PropertyChangedEventArgs e)
        {
            Init();
        }

        void TypeChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CarbonData")
            {
                //ComputeCarbon();
            }
        }

        void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CarbonData")
            {

            }
        }



        public override bool Equals(object obj)
        {
            SimpleGroup simple = obj as SimpleGroup;
            if (simple == null) return false;
            return this._group.Equals(simple._group);
        }

        public override int GetHashCode()
        {
            return _group.GetHashCode();
        }

        public override string ToString()
        {
            return Name + "; " + Description;
        }

        IHierarchicalEnumerable IHierarchyData.GetChildren()
        {
            return new SimpleGroupHierarchyEnumerable(Children);
        }

        IHierarchyData IHierarchyData.GetParent()
        {
            return Parent;
        }


        bool IHierarchyData.HasChildren
        {
            get { return Children.FirstOrDefault() != null; }
        }

        object IHierarchyData.Item
        {
            get { return this; }
        }

        string IHierarchyData.Path
        {
            get
            {
                string path = _group.GlobalId;
                if (Parent == null) return path;
                path = ((IHierarchyData)Parent).Path + ";" + path;
                return path;
            }
        }

        string IHierarchyData.Type
        {
            get { return this.GetType().Name; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }
}