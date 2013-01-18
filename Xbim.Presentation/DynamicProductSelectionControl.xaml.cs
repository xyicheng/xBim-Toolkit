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
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using Xbim.XbimExtensions;
using Xbim.IO;
using Xbim.Ifc.Kernel;
using Microsoft.Win32;
using System.IO;

namespace Xbim.Presentation
{
    /// <summary>
    /// This class can compile the code in the runtime and can be used to select 
    /// certain products with C# code;
    /// </summary>
    public partial class DynamicProductSelectionControl : UserControl
    {
        public DynamicProductSelectionControl()
        {
            InitializeComponent();
            txtCode.Text = CodeTemplate;
        }

        #region Code skeleton
        private string CodeSkeleton1 = @"
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using System.Reflection;

using Xbim.XbimExtensions;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.RepresentationResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.PresentationAppearanceResource;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.DataProviders;
using Xbim.Ifc.StructuralLoadResource;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.StructuralAnalysisDomain;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.PlumbingFireProtectionDomain;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.ElectricalDomain;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.Ifc.ConstraintResource;
using Xbim.Ifc.BuildingControlsDomain;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.TopologyResource;
using Xbim.Ifc.ProfileResource;
using Xbim.Ifc.PropertySetDefinitions;
using Xbim.Ifc.ProfilePropertyResource;
using Xbim.Ifc.ProcessExtensions;
using Xbim.Ifc.GeometricModelResource;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.PresentationResource;
using Xbim.Ifc.PresentationDefinitionResource;
using Xbim.Ifc.CostResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.MaterialResource;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.MaterialPropertyResource;
using Xbim.Ifc.FacilitiesMgmtDomain;
using Xbim.Ifc.ControlExtension;
using Xbim.Ifc.TimeSeriesResource;
using Xbim.Ifc.SharedFacilitiesElements;
using Xbim.Ifc.SharedComponentElements;
using Xbim.Ifc.PresentationOrganizationResource;
using Xbim.Ifc.QuantityResource;
using Xbim.XbimExtensions.Helpers;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.Ifc.Extensions;


namespace DynamicQuery
{
    public class Query
    {
        private StringWriter Output = new StringWriter();
        public string GetOutput()
        {
            if (Output == null) return """";
            return Output.ToString();
        }
            ";

        string CodeTemplate =
@"//This will perform selection of the objects. 
//Selected objects with the geometry will be highlighted
public IEnumerable<IfcProduct> Select(IModel model)
{
    Output.WriteLine(""Hello selected products"");
    return model.InstancesOfType<IfcWall>();
}

//This will hide all objects except for the returned ones
public IEnumerable<IfcProduct> ShowOnly(IModel model)
{
    Output.WriteLine(""Hello visible products!"");
    return model.InstancesWhere<IfcProduct>(p => p.Name != null &&  ((string)p.Name).ToLower().Contains(""wall""));
}

//This will execute arbitrary code with no return value
public void Execute(IModel model)
{
    IEnumerable<IfcSpace> spaces = model.InstancesOfType<IfcSpace>();
    foreach (IfcSpace space in spaces)
    {
        Output.WriteLine(space.Name + "" - "" + space.LongName);
    }
}
";

        string CodeSkeleton2 =
@"
    }
}
";
        #endregion

        #region Model Dependency Property
        public IModel Model
        {
            get { return (IModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(IModel), typeof(DynamicProductSelectionControl), new UIPropertyMetadata(null));
        #endregion

        #region ProductSelectionChanged infrastructure
        public delegate void ProductSelectionChangedEventHandler(object sender, ProductSelectionChangedEventArgs a);
        public event ProductSelectionChangedEventHandler ProductSelectionChanged;

        protected virtual void OnRaiseProductSelectionChangedEvent(IEnumerable<IfcProduct> products)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            ProductSelectionChangedEventHandler handler = ProductSelectionChanged;

            // Event will be null if there are no subscribers 
            if (handler != null)
            {
                //create argument
                ProductSelectionChangedEventArgs e = new ProductSelectionChangedEventArgs(products);

                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        public class ProductSelectionChangedEventArgs : EventArgs
        {
            public ProductSelectionChangedEventArgs(IEnumerable<IfcProduct> selection)
            {
                _selection = selection;
            }
            private IEnumerable<IfcProduct> _selection;
            public IEnumerable<IfcProduct> Selection
            {
                get { return _selection; }
            }
        }
        #endregion

        #region ProductVisibilityChanged infrastructure
        public delegate void ProductVisibilityChangedEventHandler(object sender, ProductVisibilityChangedEventArgs a);
        public event ProductVisibilityChangedEventHandler ProductVisibilityChanged;

        protected virtual void OnRaiseProductVisibilityChangedEvent(IEnumerable<IfcProduct> products)
        {
            // Make a temporary copy of the event to avoid possibility of 
            // a race condition if the last subscriber unsubscribes 
            // immediately after the null check and before the event is raised.
            ProductVisibilityChangedEventHandler handler = ProductVisibilityChanged;

            // Event will be null if there are no subscribers 
            if (handler != null)
            {
                //create argument
                ProductVisibilityChangedEventArgs e = new ProductVisibilityChangedEventArgs(products);

                // Use the () operator to raise the event.
                handler(this, e);
            }
        }

        public class ProductVisibilityChangedEventArgs : EventArgs
        {
            public ProductVisibilityChangedEventArgs(IEnumerable<IfcProduct> selection)
            {
                _selection = selection;
            }
            private IEnumerable<IfcProduct> _selection;
            public IEnumerable<IfcProduct> Selection
            {
                get { return _selection; }
            }
        }
        #endregion

        private void btnPerform_Click(object sender, RoutedEventArgs e)
        {
            if (Model == null)
            {
                MessageBox.Show("There is no model available.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string code = txtCode.Text;
            if (String.IsNullOrEmpty(code) || String.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("You have to insert some C# code.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            //create compiler
            Dictionary<string, string> providerOptions = new Dictionary<string, string> { { "CompilerVersion", "v4.0" } };
            CSharpCodeProvider provider = new CSharpCodeProvider(providerOptions);
            CompilerParameters compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };
            compilerParams.ReferencedAssemblies.Add("Microsoft.CSharp.dll");
            compilerParams.ReferencedAssemblies.Add("System.dll");
            compilerParams.ReferencedAssemblies.Add("System.Core.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.dll");
            compilerParams.ReferencedAssemblies.Add("System.Data.DataSetExtensions.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.dll");
            compilerParams.ReferencedAssemblies.Add("System.Xml.Linq.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Common.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Ifc.dll");
            compilerParams.ReferencedAssemblies.Add("Xbim.Ifc.Extensions.dll");

            //get the code together
            string source = CodeSkeleton1 + code + CodeSkeleton2;
            //compile the source
            CompilerResults results = provider.CompileAssemblyFromSource(compilerParams, source);

            //check the result
            if (results.Errors.Count != 0)
            {
                StringBuilder errors = new StringBuilder("Compiler Errors :\r\n");
                foreach (CompilerError error in results.Errors)
                {
                    errors.AppendFormat("Line {0},{1}\t: {2}\n", error.Line, error.Column, error.ErrorText);
                }
                MessageBox.Show("Compilation of your code has failed. \n" + errors.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            //create instance of the objekct from the compiled assembly
            object o = results.CompiledAssembly.CreateInstance("DynamicQuery.Query");
            if (o == null)
                throw new Exception("Compiled code does not contain class DynamicQuery.Query");
            MethodInfo miSelect = o.GetType().GetMethod("Select", new Type[] { typeof(IModel) });
            MethodInfo miShowOnly = o.GetType().GetMethod("ShowOnly", new Type[] { typeof(IModel) });
            MethodInfo miExecute = o.GetType().GetMethod("Execute", new Type[] { typeof(IModel) });
            MethodInfo miOutput = o.GetType().GetMethod("GetOutput");
            

            //check for existance of the methods
            if (miOutput == null)
                //this function should be there because it is my infrastructure
                throw new Exception("Code doesn't contain predefined method with the signature: public string GetOutput();");
            try
            {
                if (miSelect != null)
                {
                        IEnumerable<IfcProduct> prods = miSelect.Invoke(o, new object[] { Model }) as IEnumerable<IfcProduct> ?? new List<IfcProduct>();

                        //raise the event about the selection change
                        OnRaiseProductSelectionChangedEvent(prods);
                }
                if (miShowOnly != null)
                {
                        IEnumerable<IfcProduct> prods = miShowOnly.Invoke(o, new object[] { Model }) as IEnumerable<IfcProduct> ?? new List<IfcProduct>();

                        //raise the event about the selection change
                        OnRaiseProductVisibilityChangedEvent(prods);
                }
                if (miExecute != null)
                {
                        miExecute.Invoke(o, new object[] { Model });
                }
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException;
                if (innerException != null)
                    MessageBox.Show("There was a runtime exception during the code execution: \n" + innerException.Message + "\n" + innerException.StackTrace, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else throw ex;
            }
           

           

            //get messages from the compiled code
            string msg = miOutput.Invoke(o, null) as string;
            txtOutput.Text += msg;
            txtOutput.ScrollToEnd();

        }

        private void btnDefault_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Text = CodeTemplate;

        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".cs";
            dlg.CheckFileExists = true;
            dlg.Multiselect = false;
            dlg.Title = "Choose existing code file...";
            dlg.ValidateNames = true;
            if (dlg.ShowDialog() == true)
            {
                txtCode.Text = File.ReadAllText(dlg.FileName);
            }

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".cs";
            dlg.OverwritePrompt = true;
            dlg.Title = "Save the code as...";
            dlg.ValidateNames = true;

            if (dlg.ShowDialog() == true)
            {
                Stream file = dlg.OpenFile();
                TextWriter wr = new StreamWriter(file);
                wr.Write(txtCode.Text);
                wr.Close();
                file.Close();
            }

        }

        private void btnSaveOutput_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".txt";
            dlg.OverwritePrompt = true;
            dlg.Title = "Save the output as...";
            dlg.ValidateNames = true;

            if (dlg.ShowDialog() == true)
            {
                Stream file = dlg.OpenFile();
                TextWriter wr = new StreamWriter(file);
                wr.Write(txtOutput.Text);
                wr.Close();
                file.Close();
            }
        }
    }

   
}
