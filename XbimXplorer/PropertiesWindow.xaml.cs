#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     XbimXplorer
// Filename:    PropertiesWindow.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for PropertiesWindow.xaml
    /// </summary>
    public partial class PropertiesWindow : Window
    {
        public PropertiesWindow()
        {
            InitializeComponent();
        }


        public object Instance
        {
            get { return (object) GetValue(InstanceProperty); }
            set { SetValue(InstanceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Instance.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty InstanceProperty =
            DependencyProperty.Register("Instance", typeof (object), typeof (PropertiesWindow),
                                        new UIPropertyMetadata(null));
    }
}