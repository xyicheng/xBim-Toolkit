using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PropertyTools.Wpf;
using Xbim.IO;
using Xbim.IO.TreeView;
using System.Windows;
using Xbim.Ifc2x3.Kernel;
using System.Windows.Data;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Windows.Controls.Primitives;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Specialized;

namespace Xbim.Presentation
{
    public class XbimTreeview : TreeListBox
    {

        public XbimTreeview()
        {
            SelectionMode = System.Windows.Controls.SelectionMode.Single; //always use single selection mode
           
        }

        protected override void OnSelectionChanged(System.Windows.Controls.SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            if (e.AddedItems.Count > 0)
                EntityLabel = ((IXbimViewModel)(e.AddedItems[0])).EntityLabel;
        }



        public int EntityLabel
        {
            get { return (int)GetValue(EntityLabelProperty); }
            set { SetValue(EntityLabelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EntityLabel.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EntityLabelProperty =
            DependencyProperty.Register("EntityLabel", typeof(int), typeof(XbimTreeview), new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnEntityLabelChanged)));

        


        private static void OnEntityLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XbimTreeview view = d as XbimTreeview;
            if (view != null && e.NewValue is int)
            {
                view.UnselectAll();
                int newVal = (int)(e.NewValue);
                if (newVal > 0) view.Select(newVal);
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
                model.RefencedModels.CollectionChanged += tv.RefencedModels_CollectionChanged;
                switch (tv.ViewDefinition)
                {
                    case XbimViewType.SpatialStructure:
                        tv.ViewModel();
                        
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

        void RefencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {  
                XbimReferencedModel refModel = e.NewItems[0] as XbimReferencedModel;
                XbimModelViewModel vm = HierarchySource.Cast<XbimModelViewModel>().FirstOrDefault();
                if(vm!=null)
                {
                    vm.AddRefModel(new XbimModelViewModel(refModel.Model.IfcProject));
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
                foreach (var child in svList)
                {
                    LazyLoadAll(child);
                }
            }
            else //Load any spatialstructure
            {
            }
        }
        private void ViewModel()
        {
            IfcProject project = Model.IfcProject as IfcProject;
            if (project != null)
            {
                this.ChildrenBinding = new Binding("Children");
                List<XbimModelViewModel> svList = new List<XbimModelViewModel>();  
                svList.Add(new XbimModelViewModel(project));
                this.HierarchySource = svList;
            }
        }
        private void LazyLoadAll(IXbimViewModel parent)
        {

            foreach (var child in parent.Children)
            {
                LazyLoadAll(child);
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
