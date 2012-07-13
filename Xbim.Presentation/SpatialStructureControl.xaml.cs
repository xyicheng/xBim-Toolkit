#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    SpatialStructureControl.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Presentation
{
    public class SpatialStructureTreeItem
    {
        public SpatialStructureTreeItem()
        {
        }

        public SpatialStructureTreeItem(IfcObjectDefinition item)
        {
            _item = item;
        }

        private IfcObjectDefinition _item;

        public IfcObjectDefinition Item
        {
            get { return _item; }
            set { _item = value; }
        }

        private SortedList<string, SpatialStructureTreeItem> _children;

        public IList<SpatialStructureTreeItem> Children
        {
            get { return _children != null ? _children.Values : null; }
        }

        public string ElementType
        {
            get { return _item.GetType().Name; }
        }

        public void AddChild(SpatialStructureTreeItem child)
        {
            if (_children == null) _children = new SortedList<string, SpatialStructureTreeItem>();
            _children.Add(child.Item.ToString() + child.Item.GlobalId, child);
        }

        public override string ToString()
        {
            return string.Format("SpatialStructureTreeItem - IfcProduct = {0}", _item.GetType().Name);
        }
    }

    /// <summary>
    ///   Interaction logic for SpatialStructureControl.xaml
    /// </summary>
    public partial class SpatialStructureControl : UserControl, INotifyPropertyChanged
    {
        private ModelDataProvider _modelProvider;

        public IfcProject Project
        {
            get { return (IfcProject) GetValue(ProjectProperty); }
            set { SetValue(ProjectProperty, value); }
        }


        // Using a DependencyProperty as the backing store for Project.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ProjectProperty =
            DependencyProperty.Register("Project", typeof (IfcProject), typeof (SpatialStructureControl),
                                        new UIPropertyMetadata(null, new PropertyChangedCallback(OnProjectChanged)));


        internal ModelDataProvider ModelProvider
        {
            get
            {
                if (_modelProvider == null)
                {
                    ObjectDataProvider objDP = FindResource("ModelProvider") as ObjectDataProvider;
                    if (objDP != null)
                        _modelProvider = (ModelDataProvider) objDP.ObjectInstance;
                }
                return _modelProvider;
            }
        }

        private static void OnProjectChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IfcProject proj = e.NewValue as IfcProject;
            SpatialStructureControl sc = d as SpatialStructureControl;
            if (sc != null && proj != null)
            {
                ModelDataProvider mdp = sc.ModelProvider;
                ObjectDataProvider spatialStructureProvider =
                    sc.FindResource("SpatialStructureProvider") as ObjectDataProvider;
                if (mdp != null && spatialStructureProvider != null)
                {
                    Dictionary<IfcObjectDefinition, SpatialStructureTreeItem> created =
                        new Dictionary<IfcObjectDefinition, SpatialStructureTreeItem>(
                            mdp.Model.InstancesOfType<IfcRelDecomposes>().Count());

                    foreach (IfcRelDecomposes rel in mdp.Model.InstancesOfType<IfcRelDecomposes>())
                    {
                        if (rel.RelatingObject != null)
                        {
                            SpatialStructureTreeItem treeItem;
                            if (!created.TryGetValue(rel.RelatingObject, out treeItem)) //already written
                            {
                                treeItem = new SpatialStructureTreeItem(rel.RelatingObject);
                                created.Add(rel.RelatingObject, treeItem);
                            }
                            foreach (IfcObjectDefinition child in rel.RelatedObjects)
                            {
                                SpatialStructureTreeItem childItem;
                                if (!created.TryGetValue(child, out childItem)) //already written
                                {
                                    childItem = new SpatialStructureTreeItem(child);
                                    created.Add(child, childItem);
                                }
                                treeItem.AddChild(childItem);
                            }
                        }
                    }
                    //if(mdp.Model.Instances.RelContainedInSpatialStructures.First() !=null)
                    //    treeItem.AddChild(new SpatialStructureTreeItem());
                    foreach (
                        IfcRelContainedInSpatialStructure scRel in
                            mdp.Model.InstancesOfType<IfcRelContainedInSpatialStructure>())
                    {
                        if (scRel.RelatingStructure != null)
                        {
                            SpatialStructureTreeItem treeItem;
                            if (!created.TryGetValue(scRel.RelatingStructure, out treeItem)) //already written
                            {
                                treeItem = new SpatialStructureTreeItem(scRel.RelatingStructure);
                                created.Add(scRel.RelatingStructure, treeItem);
                            }
                            foreach (IfcObjectDefinition child in scRel.RelatedElements)
                            {
                                SpatialStructureTreeItem childItem;
                                if (!created.TryGetValue(child, out childItem)) //already written
                                {
                                    childItem = new SpatialStructureTreeItem(child);
                                    created.Add(child, childItem);
                                }
                                treeItem.AddChild(childItem);
                            }
                        }
                    }
                    List<SpatialStructureTreeItem> root = new List<SpatialStructureTreeItem>(1);
                    SpatialStructureTreeItem projItem;
                    if (created.TryGetValue(proj, out projItem))
                        root.Add(projItem);
                    spatialStructureProvider.ObjectInstance = root;
                }
            }
        }


        public SpatialStructureControl()
        {
            InitializeComponent();

            // SpatialTreeView.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(SpatialTreeView_SelectedItemChanged);
            SpatialTreeView.SelectedItemChanged +=
                new RoutedPropertyChangedEventHandler<object>(Tree_SelectedItemChanged);
        }

        #region Events

        public static readonly RoutedEvent SelectedItemChangedEvent =
            EventManager.RegisterRoutedEvent("SelectedItemChangedEvent", RoutingStrategy.Bubble,
                                             typeof (RoutedPropertyChangedEventHandler<SpatialStructureTreeItem>),
                                             typeof (SpatialStructureControl));

        public event RoutedPropertyChangedEventHandler<SpatialStructureTreeItem> SelectedItemChanged
        {
            add { AddHandler(SelectedItemChangedEvent, value); }
            remove { RemoveHandler(SelectedItemChangedEvent, value); }
        }

        #endregion

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            if (tv != null)
            {
                SpatialStructureTreeItem si = tv.SelectedItem as SpatialStructureTreeItem;
                if (si != null) SelectedItem = si.Item;
            }
            if (e.NewValue is SpatialStructureTreeItem && e.OldValue is SpatialStructureTreeItem)
            {
                RoutedPropertyChangedEventArgs<SpatialStructureTreeItem> selEv =
                    new RoutedPropertyChangedEventArgs<SpatialStructureTreeItem>((SpatialStructureTreeItem)e.OldValue,
                                                                                 (SpatialStructureTreeItem)e.NewValue,
                                                                                 SelectedItemChangedEvent);
                RaiseEvent(selEv);
            }
        }

        private void SpatialTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            if (tv != null)
            {
                SpatialStructureTreeItem si = tv.SelectedItem as SpatialStructureTreeItem;
                if (si != null) SelectedItem = si.Item;
            }
        }

        private object _selectedItem;

        public object SelectedItem
        {
            get { return _selectedItem; }
            private set
            {
                // SpatialTreeView.s
                _selectedItem = value;
                NotifyPropertyChanged("SelectedItem");
            }
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #endregion
    }
}