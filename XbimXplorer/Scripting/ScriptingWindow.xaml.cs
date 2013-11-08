using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xbim.IO;
using Xbim.Script;

namespace XbimXplorer.Scripting
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : Window
    {


        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(ScriptingWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnModelChanged)));


        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScriptingWindow sw = d as ScriptingWindow;
            var dp = sw.DataContext as ObjectDataProvider;
            if (dp != null) 
                dp.Refresh();
        }

        public ScriptingWindow()
        {
            InitializeComponent();
            ScriptingConcrol.OnScriptParsed += new ScriptParsedHandler(delegate(object sender, ScriptParsedEventArgs arg){
                ScriptParsed();
            });
        }

        public event ScriptParsedHandler OnScriptParsed;
        private void ScriptParsed()
        {
            if (OnScriptParsed != null)
                OnScriptParsed(this, new ScriptParsedEventArgs());
        }
    }
}
