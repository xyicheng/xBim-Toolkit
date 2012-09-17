#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    IfcMetaDataControl.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;

#endregion

namespace Xbim.Presentation
{
    /// <summary>
    ///   Interaction logic for IfcMetaDataControl.xaml
    /// </summary>
    public partial class IfcMetaDataControl : UserControl, INotifyPropertyChanged
    {
        public class PropertyItem : IComparable
        {
            private int _ID;
            private string _name;
            private string _value;

            public int ID
            {
                get { return _ID; }
                set { _ID = value; }
            }


            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }


            public string Value
            {
                get { return _value; }
                set { _value = value; }
            }

            #region IComparable Members

            public int CompareTo(object obj)
            {
                PropertyItem pi = obj as PropertyItem;
                if (pi != null)
                    return this.ID.CompareTo(pi.ID);
                else
                    return -1;
            }

            #endregion
        }

        public IfcMetaDataControl()
        {
            InitializeComponent();
        }

        private ObservableCollection<PropertyItem> _properties = new ObservableCollection<PropertyItem>();

        public ObservableCollection<PropertyItem> Properties
        {
            get { return _properties; }
        }

        private ObservableCollection<IfcMaterialSelect> _materials = new ObservableCollection<IfcMaterialSelect>();

        public ObservableCollection<IfcMaterialSelect> Materials
        {
            get { return _materials; }
        }

        private ObservableCollection<IfcPropertySet> _propertySets = new ObservableCollection<IfcPropertySet>();

        public ObservableCollection<IfcPropertySet> PropertySets
        {
            get { return _propertySets; }
        }

        private ObservableCollection<IfcPropertySetDefinition> _typePropertySets =
            new ObservableCollection<IfcPropertySetDefinition>();

        public ObservableCollection<IfcPropertySetDefinition> TypePropertySets
        {
            get { return _typePropertySets; }
        }

        public object IfcInstance
        {
            get { return (object) GetValue(IfcInstanceProperty); }
            set { SetValue(IfcInstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IfcInstance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IfcInstanceProperty =
            DependencyProperty.Register("IfcInstance", typeof (object), typeof (IfcMetaDataControl),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnIfcInstanceChanged)));


        private static void OnIfcInstanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IfcMetaDataControl ctrl = d as IfcMetaDataControl;
            if (ctrl != null && e.NewValue != null && e.NewValue is IPersistIfc)
                ctrl.LoadMetaData((IPersistIfcEntity) e.NewValue);
        }

        private void LoadMetaData(IPersistIfcEntity item)
        {
            IfcType ifcType = item.IfcType();

            List<PropertyItem> pis = new List<PropertyItem>(ifcType.IfcProperties.Count());
            PropertyItem plabel = new PropertyItem()
                                      {ID = 0, Name = "Label", Value = "#" + Math.Abs(item.EntityLabel).ToString()};
            pis.Add(plabel);
            foreach (var pInfo in ifcType.IfcProperties)
            {
                if (pInfo.Value.IfcAttribute.State != IfcAttributeState.DerivedOverride)
                    //only process Ifc Properties and those that are not derived
                {
                    PropertyItem pi = new PropertyItem()
                                          {ID = pInfo.Value.IfcAttribute.Order, Name = pInfo.Value.PropertyInfo.Name};
                    pis.Add(pi);
                    object val = pInfo.Value.PropertyInfo.GetValue(item, null);
                    if (val != null)
                    {
                        if (val is ExpressType)
                            pi.Value = ((ExpressType) val).ToPart21;
                        else if (val.GetType().IsEnum)
                            pi.Value = string.Format(".{0}.", val.ToString());
                        else //it's a class
                        {
                            pi.Value = string.Format("{0}", pInfo.Value.PropertyInfo.PropertyType.Name.ToUpper());
                        }
                    }
                    else
                        pi.Value = "null";
                }
            }

            _properties = new ObservableCollection<PropertyItem>(pis);
            NotifyPropertyChanged("Properties");

            //now deal with PropertySets
            _propertySets.Clear();
            _typePropertySets.Clear();
            IfcObject ifcObj = item as IfcObject;
            //ModelDataProvider mdp = DataContext as ModelDataProvider;
            if (ifcObj != null)
            {
                IModel m = ifcObj.ModelOf;
                //write out any material layers
                IEnumerable<IfcRelAssociatesMaterial> matRels =
                    ifcObj.HasAssociations.OfType<IfcRelAssociatesMaterial>();
                foreach (IfcRelAssociatesMaterial matRel in matRels)
                {
                    _materials.Add(matRel.RelatingMaterial);
                }
                //now the property sets
                foreach (IfcRelDefinesByProperties relDef in ifcObj.IsDefinedByProperties)
                {
                    IfcPropertySet pSet = relDef.RelatingPropertyDefinition as IfcPropertySet;
                    if (pSet != null)
                        _propertySets.Add(pSet);
                }
                //now the type property sets
                IfcTypeObject to = ifcObj.GetDefiningType(m);
                if (to != null)
                {
                    PropertySetDefinitionSet pds = to.HasPropertySets;
                    if (pds != null)
                    {
                        foreach (IfcPropertySetDefinition pSet in pds)
                        {
                            _typePropertySets.Add(pSet);
                        }
                    }
                }
            }
            NotifyPropertyChanged("PropertySets");
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