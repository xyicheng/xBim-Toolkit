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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.IO;
using Xbim.Script;
using System.Windows.Forms;
using System.IO;

namespace Xbim.Presentation
{
    /// <summary>
    /// Interaction logic for ScriptingWindow.xaml
    /// </summary>
    public partial class ScriptingWindow : System.Windows.Controls.UserControl
    {
        public ScriptingWindow()
        {
            InitializeComponent();
        }

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
            if (sw != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                //create new parser which is associated to the model if model has been changed
                sw._parser = new XbimQueryParser(model);
            }
        }

        private XbimQueryParser _parser = new XbimQueryParser();

        private void SaveScript_Click(object sender, RoutedEventArgs e)
        {
            string script = ScriptInput.Text;
            if (String.IsNullOrEmpty(script))
            {
                System.Windows.MessageBox.Show("There is no script to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Filter = "*.bql | BQL files";
            dlg.Title = "Set file name of the script...";
            dlg.ValidateNames = true;
            dlg.FileOk += new System.ComponentModel.CancelEventHandler(delegate(object s, System.ComponentModel.CancelEventArgs eArg)
            {
                var name = dlg.FileName;

                var file = File.Open(name, FileMode.Create, FileAccess.Write);
                try
                {
                    var txtWriter = new StreamWriter(file);
                    txtWriter.Write(script);
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("Saving script to file failed. Check if the location is writable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    file.Close();
                }
            });
            dlg.ShowDialog();
        }


        private void LoadScript_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Filter = "*.bql | BQL files";
            dlg.Title = "Specify the script file...";
            dlg.ValidateNames = true;
            dlg.FileOk += new System.ComponentModel.CancelEventHandler(delegate(object s, System.ComponentModel.CancelEventArgs eArg) {
                var file = dlg.OpenFile();
                var reader = new StreamReader(file);
                var script = reader.ReadToEnd();
                ScriptInput.Text = script;
                file.Close();
            });
            dlg.ShowDialog();
        }

        private void Execute_Click(object sender, RoutedEventArgs e)
        {
            var script = ScriptInput.Text;
            if (String.IsNullOrEmpty(script))
            {
                System.Windows.MessageBox.Show("There is no script to execute.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _parser.Parse(script);

            if (_parser.Errors.Count != 0)
            {
                //todo: handle errors from parser execution
                throw new NotImplementedException();
            }
        }


    }
}
