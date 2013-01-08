using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PropertyTools.Wpf;
using Xbim.IO;
using System.Windows;
using Xbim.Ifc2x3.Kernel;
using System.Windows.Data;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Windows.Controls.Primitives;
using System.Diagnostics;

namespace Xbim.Presentation
{
    public class XbimTreeview : TreeListBox
    {

        public XbimTreeview()
        {
            SelectionMode = System.Windows.Controls.SelectionMode.Single; //always use single selection mode
            SelectedValuePath = "EntityLabel";
        }



        new public int SelectedItem
        {
            get { return (int)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        new public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(int), typeof(XbimTreeview), new UIPropertyMetadata(-1, new PropertyChangedCallback(OnSelectedItemChanged)));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview view = d as XbimTreeview;
            if (view != null && e.NewValue is int)
            {
                int newVal = (int)(e.NewValue);
                view.Select(newVal);
                return;
                
           }
        }

        private void Select(int newVal)
        {
            foreach (var item in HierarchySource.OfType<IXbimViewModel>())
            {
                IXbimViewModel toSelect = FindItem(item, newVal);
                if (toSelect != null)
                {
                    item.IsExpanded = true;

                    UpdateLayout();
                    ScrollIntoView(toSelect);
                    toSelect.IsSelected = true; ;
                    return;
                }
            }
        }

        public IXbimViewModel FindItem(IXbimViewModel node, int entitylabel)
        {
            if (node.EntityLabel == entitylabel)
            {
                node.IsExpanded = true;
                return node;
            }

            foreach (var child in node.Children)
            {
                IXbimViewModel res = FindItem(child, entitylabel);
                if (res != null)
                {
                    node.IsExpanded = true; //it is here so expand parent
                    return res;
                }
            }
            return null;
        }

        public XbimViewType ViewDefinition
        {
            get { return (XbimViewType)GetValue(ViewDefinitionProperty); }
            set { SetValue(ViewDefinitionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ViewDefinition.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewDefinitionProperty =
            DependencyProperty.Register("ViewDefinition", typeof(XbimViewType), typeof(XbimTreeview), new UIPropertyMetadata(XbimViewType.SpatialStructure, new PropertyChangedCallback(OnViewDefinitionChanged)));

        private static void OnViewDefinitionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }


       
        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(XbimTreeview), new UIPropertyMetadata(null, new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview tv = d as XbimTreeview;
            XbimModel model = e.NewValue as XbimModel;
            if (tv != null && model != null)
            {
                switch (tv.ViewDefinition)
                {
                    case XbimViewType.SpatialStructure:
                        tv.ViewSpatialStructure();
                        
                        break;
                    case XbimViewType.Classification:
                        break;
                    case XbimViewType.Materials:
                        break;
                    case XbimViewType.IfcEntityType:
                        break;
                    default:
                        break;
                }

                
            }
            else
            {
                if (tv != null) //unbind
                {
                    tv.HierarchySource = null;
                }
            }
        }

        private void ViewSpatialStructure()
        {
            IfcProject project = Model.IfcProject as IfcProject;
            if (project != null)
            {
                this.ChildrenBinding = new Binding("Children");
                List<SpatialViewModel> svList = new List<SpatialViewModel>();
                foreach (var item in project.GetSpatialStructuralElements())
                {
                    var sv = new SpatialViewModel(item);
                    svList.Add(sv); 
                }
                
                this.HierarchySource = svList;
            }
            else //Load any spatialstructure
            {
            }
        }
        private void Expand(IXbimViewModel treeitem)
        {
            treeitem.IsExpanded = true;
            foreach (var child in treeitem.Children)
            {
                Expand(child);
            }
        }

        private void ViewClassification()
        {
            //IfcProject project = Model.IfcProject as IfcProject;
            //if (project != null)
            //{
            //    this.ChildrenBinding = new Binding("SubClassifications");
            //    List<ClassificationViewModel> sv = new List<ClassificationViewModel>();
            //    foreach (var item in Model.Instances.OfType<IfcClassification>())
            //    {
            //        sv.Add(new ClassificationViewModel(item));
            //    }
            //    this.HierarchySource = sv;
            //}
            //else //Load any spatialstructure
            //{
            //}
        }
    }
}
