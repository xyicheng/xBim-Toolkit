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
    public partial class ScriptingControl : System.Windows.Controls.UserControl
    {
        public ScriptingControl()
        {
            InitializeComponent();
            CreateParser();
        }

        public XbimVariables Results
        {
            get { return _parser.Results; }
        }

        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(ScriptingControl), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ScriptingControl sw = d as ScriptingControl;
            if (sw != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                if (model != null)
                    //create new parser which is associated to the model if model has been changed
                    sw.CreateParser(model);
            }
        }

        private XbimQueryParser _parser;
        private void CreateParser(XbimModel model = null)
        {
            if (model != null) _parser = new XbimQueryParser(model);
            else _parser = new XbimQueryParser();
            _parser.OnModelChanged += new ModelChangedHandler(delegate(object sender, ModelChangedEventArgs e) {
                SetValue(ModelProperty, model);
                var provider = DataContext as ObjectDataProvider;
                if (provider != null)
                    provider.Refresh();
            });
            _parser.OnScriptParsed += new ScriptParsedHandler(delegate(object sender, ScriptParsedEventArgs e) {
                //fire the event
                ScriptParsed();
            });
        }

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
            dlg.Title = "Set file name of the script...";
            dlg.OverwritePrompt = true;
            dlg.ValidateNames = true;
            dlg.FileOk += new System.ComponentModel.CancelEventHandler(delegate(object s, System.ComponentModel.CancelEventArgs eArg)
            {
                var name = dlg.FileName;
                StreamWriter file = null;
                try
                {
                    file = new StreamWriter(name, false);
                    file.Write(script);
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("Saving script to file failed. Check if the location is writable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file!= null)
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
            dlg.Title = "Specify the script file...";
            dlg.ValidateNames = true;
            dlg.FileOk += new System.ComponentModel.CancelEventHandler(delegate(object s, System.ComponentModel.CancelEventArgs eArg) {
                Stream file = null;
                try
                {
                    file = dlg.OpenFile();
                    var reader = new StreamReader(file);
                    var script = reader.ReadToEnd();
                    ScriptInput.Text = script;
                }
                catch (Exception)
                {
                    System.Windows.MessageBox.Show("Loading script from file failed. Check if the file exist and is readable.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
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

            ErrorsWindow.Visibility = System.Windows.Visibility.Collapsed;
            ErrorsWindow.Text = null;
            MsgWindow.Visibility = System.Windows.Visibility.Collapsed;
            MsgWindow.Text = null;

            if (_parser.Errors.Count != 0)
            {
                foreach (var err in _parser.Errors)
                    ErrorsWindow.Text += err + "\n";
                ErrorsWindow.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                MsgWindow.Text += DateTime.Now.ToShortTimeString() + " run OK";
                MsgWindow.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void ScriptInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var num = ScriptInput.LineCount;
            LineNumbers.Text = "1";

            for (int i = 2; i < num+1; i++)
			{
                LineNumbers.Text += "\n" + i;
			}


        }

        public event ScriptParsedHandler OnScriptParsed;
        private void ScriptParsed()
        {
            if (OnScriptParsed != null)
                OnScriptParsed(this, new ScriptParsedEventArgs());
        }

    }
}
