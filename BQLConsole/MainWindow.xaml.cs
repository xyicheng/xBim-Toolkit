using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Xbim.IO;
using Xbim.Script;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace BQLConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            //new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd).Text = "";
            Data = new IntelliData();
            ScriptText.KeyWordSource = Data.KeyWords;
            ScriptText.PredictTriggers = Data.Predict;
            ScriptText.KeyColour = false;
            KeyWordColour.IsChecked = ScriptText.KeyColour;
            ScriptText.Focus();

            CreateParser();
            

            DataContext = this;
            
        }


        IntelliData Data;
        private XbimQueryParser _parser;
        private TextWriter _output;
        
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
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnModelChanged)));

       

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainWindow mw = d as MainWindow;
            if (mw != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                if (model != null)
                    //create new parser which is associated to the model if model has been changed
                    mw.CreateParser(model);
            }
        }

        private void CreateParser(XbimModel model = null)
        {
            if (model != null) _parser = new XbimQueryParser(model);
            else _parser = new XbimQueryParser();

            _parser.OnScriptParsed += delegate(object sender, ScriptParsedEventArgs e)
            {
                //fire the event
                //ScriptParsed();
            };
            _parser.OnModelChanged += delegate(object sender, ModelChangedEventArgs e)
            {
                Model = e.NewModel;
                //ModelChangedByScript(e.NewModel);
            };
            //redirect the parser output to the results window
            _output = new RedirectWriter(ResultsWindow);
            _parser.Output = _output;
        }
        
        
        //public event ScriptParsedHandler OnScriptParsed;
        //private void ScriptParsed()
        //{
        //    if (OnScriptParsed != null)
        //        OnScriptParsed(this, new ScriptParsedEventArgs());
        //}

        //public event ModelChangedHandler OnModelChangedByScript;
        //private void ModelChangedByScript(XbimModel newModel)
        //{
        //    if (OnModelChangedByScript != null)
        //        OnModelChangedByScript(this, new ModelChangedEventArgs(newModel));
        //}


        private void OnClick_SaveScript(object sender, RoutedEventArgs e)
        {
            string dataFormat = "Text";
            TextRange txtRange = new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd);
            string script = txtRange.Text.Trim();
            if (String.IsNullOrEmpty(script))
            {
                System.Windows.MessageBox.Show("There is no script to save.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Filter = "Building Query Language|*.bql";
            dlg.FilterIndex = 0;
            dlg.Title = "Set file name of the script";
            dlg.OverwritePrompt = true;
            dlg.ValidateNames = true;
            dlg.FileOk += delegate(object s, System.ComponentModel.CancelEventArgs eArg)
            {
                var name = dlg.FileName;
                FileStream file = null;
                try
                {
                    file = new FileStream(name, FileMode.Create);
                    if (txtRange.CanSave(dataFormat))
                    {
                        txtRange.Save(file, dataFormat);
                    }

                    this.Title = "BQL Console - " + System.IO.Path.GetFileName(name);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format("Saving script to file failed with message : {0}.", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            };
            dlg.ShowDialog();
        }

        private void OnClick_LoadScript(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog();
            dlg.AddExtension = true;
            dlg.DefaultExt = ".bql";
            dlg.Title = "Specify the script file";
            dlg.Filter = "Building Query Language|*.bql";
            dlg.FilterIndex = 0;
            dlg.ValidateNames = true;
            dlg.FileOk += delegate(object s, System.ComponentModel.CancelEventArgs eArg)
            {
                Stream file = null;
                string dataFormat = "Text";
                try
                {
                    file = dlg.OpenFile();
                    TextRange txtRange = new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd);
                    if (txtRange.CanLoad(dataFormat))
                    {
                        txtRange.Load(file, dataFormat);
                    }
                    this.Title = "BQL Console - " + System.IO.Path.GetFileName(dlg.FileName);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format("Loading script from file failed : {0}.", ex.Message), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (file != null)
                        file.Close();
                }
            };
            dlg.ShowDialog();
            ScriptText.RefreshKeyColour();
            
        }

        private void OnClick_NewScript(object sender, RoutedEventArgs e)
        {
            string script = new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd).Text;
            if (!String.IsNullOrEmpty(script))
            {
                MessageBoxResult mbr =  System.Windows.MessageBox.Show("Do you want to save script", "BQL Console", MessageBoxButton.YesNo);
                if (mbr == MessageBoxResult.Yes)
                {
                    OnClick_SaveScript(sender, e);
                }
                this.Title = "BQL Console - Unnamed";
            }
            new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd).Text = "";
        }

        
        private void OnClick_Execute(object sender, RoutedEventArgs e)
        {
            //clear last text
            ResultsWindow.Text = String.Empty;
            ErrorsWindow.Visibility = System.Windows.Visibility.Collapsed;
            ErrorsWindow.Text = String.Empty;

            TextRange txtRange = new TextRange(ScriptText.Document.ContentStart, ScriptText.Document.ContentEnd);
            string script = txtRange.Text.Trim();
            if (String.IsNullOrEmpty(script))
            {
                System.Windows.MessageBox.Show("There is no script to execute.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var origCurs = this.Cursor;
            this.Cursor = Cursors.Wait;
            _parser.Parse(script);
            this.Cursor = origCurs;

            //display errors
            if (_parser.Errors.Count != 0)
            {
                ErrorsWindow.Text = "Errors:\n";
                foreach (var err in _parser.Errors)
                {
                    ErrorsWindow.Text += err + "\n";
                }
                ErrorsWindow.Visibility = System.Windows.Visibility.Visible;
            }

            
            foreach (string varName in _parser.Results.GetVariables)
            {
                ResultsWindow.Text += varName + ":\n";
                _parser.Parse("Dump " + varName + ";");
                ResultsWindow.Text += "\n";
            }
        }
        
        private void OnClick_LoadModel(object sender, RoutedEventArgs e)
        {
            string fileName = string.Empty;
            string saveTxt = txtDBName.Text;
            txtDBName.SetCurrentValue(TextBlock.TextProperty, "Loading file, please wait...");

            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Title = "Select Model";
                dlg.Filter = "All XBim Files|*.ifc;*.ifcxml;*.ifczip;*.xbim;*.xbimf|IFC Files|*.ifc;*.ifcxml;*.ifczip|Xbim Files|*.xbim|Xbim Federated Files|*.xbimf";
                dlg.FilterIndex = 0;
                dlg.ValidateNames = true;
                System.Windows.Forms.DialogResult dlgresult = dlg.ShowDialog();
                if (dlgresult == System.Windows.Forms.DialogResult.OK)
                {
                    fileName = dlg.FileName;
                }
            }

            if (!string.IsNullOrEmpty(fileName))
            {
                txtDBName.SetCurrentValue(TextBlock.TextProperty, "Loading file, please wait...");
                if (Model != null)
                {
                    Model.Close();
                }
                XbimModel model = new XbimModel();
                string fileExt = System.IO.Path.GetExtension(fileName);
                if ((fileExt.Equals(".xbim", StringComparison.OrdinalIgnoreCase)) ||
                    (fileExt.Equals(".xbimf", StringComparison.OrdinalIgnoreCase))
                   )
                {
                    model.Open(fileName, XbimDBAccess.ReadWrite);
                    //model.CacheStart();
                }
                else //ifc file
                {
                    string xbimFile = System.IO.Path.ChangeExtension(fileName, "xBIM");
                    //ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                    //{
                    //    progressBar.Value = percentProgress;
                    //};
                    model.CreateFrom(fileName, xbimFile, null, true, false);
                }
                Model = model;
            }
            else
            {
                txtDBName.SetCurrentValue(TextBlock.TextProperty, saveTxt);
            }
            
        }

        /// <summary>
        /// On Click for turning key word colour on/off
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnClick_KeyWordColour(object sender, RoutedEventArgs e)
        {
            ScriptText.KeyColour = (bool)KeyWordColour.IsChecked;
            ScriptText.RefreshKeyColour();
        }
    }
}
