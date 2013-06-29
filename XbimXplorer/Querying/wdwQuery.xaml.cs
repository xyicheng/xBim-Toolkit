using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace XbimXplorer.Querying
{
    /// <summary>
    /// Interaction logic for wdwQuery.xaml
    /// </summary>
    public partial class wdwQuery : Window
    {
        public wdwQuery()
        {
            InitializeComponent();
            DisplayHelp();
        }

        public XbimModel Model;
        public XplorerMainWindow ParentWindow;

        private bool bDoClear = true; 

        private void txtCommand_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && 
                (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                )
            {
                e.Handled = true;
                if (bDoClear)
                    txtOut.Text = "";

                string[] CommandArray = txtCommand.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                if (txtCommand.SelectedText != string.Empty)
                    CommandArray = txtCommand.SelectedText.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var cmd in CommandArray)
                {
                    if (cmd.TrimStart().StartsWith("//"))
                        continue;

                    // put here all commands that don't require a database open
                    var mdbclosed = Regex.Match(cmd, @"help", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        DisplayHelp();
                        continue;
                    }

                    mdbclosed = Regex.Match(cmd, @"clear *\b(?<mode>(on|off))*", RegexOptions.IgnoreCase);
                    if (mdbclosed.Success)
                    {
                        try
                        {
                            string option = mdbclosed.Groups["mode"].Value;

                            if (option == "")
                            {
                                txtOut.Text = "";
                                continue;
                            }
                            else if (option == "on")
                                bDoClear = true;
                            else if (option == "off")
                                bDoClear = false;
                            else
                            {
                                txtOut.Text += string.Format("Autoclear not changed ({0} is not a valid option).\r\n", option);
                                continue;
                            }
                            txtOut.Text += string.Format("Autoclear set to {0}\r\n", option.ToLower());
                            continue;
                        }
                        catch (Exception ex)
                        {
                        }
                        txtOut.Text = "";
                        continue;
                    }


                    if (Model == null)
                    {
                        txtOut.Text = "Plaese open a database.\r\n";
                        continue;
                    }

                    // all commands here
                    //
                    var m = Regex.Match(cmd, @"(entitylabel|el) (?<el>\d+)(?<recursion> -*\d+)*", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        int recursion = 0;
                        int v = Convert.ToInt32(m.Groups["el"].Value);
                        try
                        {
                            recursion = Convert.ToInt32(m.Groups["recursion"].Value);
                        }
                        catch (Exception ex)
                        {
                        }

                        txtOut.Text += ReportEntity(v, recursion) + "\r\n";
                        continue;
                    }

                    m = Regex.Match(cmd, @"(ifctype|it) (?<type>.+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string type = m.Groups["type"].Value;
                        txtOut.Text += ReportType(type);
                        continue;
                    }

                    m = Regex.Match(cmd, @"(select|se) (?<mode>(count|list|short) )*(?<start>([\d,]+|[^ ]+)) *(?<props>.*)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string start = m.Groups["start"].Value;
                        string props = m.Groups["props"].Value;
                        string mode = m.Groups["mode"].Value;

                        int iIndex = -1;
                        if (start.Contains('['))
                        {
                            string[] cut = start.Split(new char[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                            start = cut[0];
                            int.TryParse(cut[1], out iIndex);
                        }

                        int[] labels = tointarray(start, ',');
                        IEnumerable<int> ret = null;
                        if (labels.Length != 0)
                        {
                            ret = QueryEngine.RecursiveQuery(Model, props, labels);                            
                        }
                        else
                        {
                            var items = QueryEngine.EntititesForType(start, Model);
                            ret = QueryEngine.RecursiveQuery(Model, props, items);
                        }
                        if (iIndex != -1)
                        {
                            int iVal = ret.ElementAt(iIndex);
                            ret = new int[] { iVal };
                        }
                        if (mode.ToLower() == "count ")
                        {
                            txtOut.Text += string.Format("Count: {0}\r\n", ret.Count());
                        }
                        else if (mode.ToLower() == "list ")
                        {
                            foreach (var item in ret)
                            {
                                txtOut.Text += item + "\r\n";
                            }
                        }
                        else
                        {
                            bool BeVerbose = true;
                            if (mode.ToLower() == "short ")
                                BeVerbose = false;
                            foreach (var item in ret)
                            {
                                txtOut.Text += ReportEntity(item, 0, Verbose: BeVerbose) + "\r\n";
                            }
                        }
                        continue;
                    }
                    m = Regex.Match(cmd, @"clip (" +
                        @"(?<elev>[-+]?([0-9]*\.)?[0-9]+) *$" +
                        "|" +                        
                        @"(?<px>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                        @"(?<py>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                        @"(?<pz>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                        @"(?<nx>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                        @"(?<ny>[-+]?([0-9]*\.)?[0-9]+) *, *" +
                        @"(?<nz>[-+]?([0-9]*\.)?[0-9]+)" +
                        ")", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        double px = 0, py = 0, pz = 0;
                        double nx = 0, ny = 0, nz = -1;

                        if (m.Groups["elev"].Value != string.Empty)
                        {
                            pz = Convert.ToDouble(m.Groups["elev"].Value);
                        }
                        else
                        {
                            px = Convert.ToDouble(m.Groups["px"].Value);
                            py = Convert.ToDouble(m.Groups["py"].Value);
                            pz = Convert.ToDouble(m.Groups["pz"].Value);
                            nx = Convert.ToDouble(m.Groups["nx"].Value);
                            ny = Convert.ToDouble(m.Groups["ny"].Value);
                            nz = Convert.ToDouble(m.Groups["nz"].Value);
                        }
                        

                        ParentWindow.DrawingControl.ClearCutPlane();
                        ParentWindow.DrawingControl.SetCutPlane(
                            px, py, pz,
                            nx, ny, nz
                            );

                        txtOut.Text += "Clip command sent\r\n";
                        ParentWindow.Activate();
                        continue;
                    }

                    m = Regex.Match(cmd, @"clip off", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        ParentWindow.DrawingControl.ClearCutPlane();
                        txtOut.Text += "Clip removed\r\n";
                        ParentWindow.Activate(); 
                        continue;
                    }

                    m = Regex.Match(cmd, @"Visual (?<action>list|on|off)( (?<Name>[^ ]+))*", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string Name = m.Groups["Name"].Value;
                        if (m.Groups["action"].Value == "list")
                        {
                            foreach (var item in ParentWindow.DrawingControl.ListItems(Name))
                            {
                                txtOut.Text += item + "\r\n";
                            }
                        }
                        else 
                        {
                            bool bVis = false;
                            if (m.Groups["action"].Value == "on")
                                bVis = true;
                            ParentWindow.DrawingControl.SetVisibility(Name, bVis);
                        }
                        continue;
                    }



                    m = Regex.Match(cmd, @"test", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        txtOut.Text = RunTestCode();
                        continue;
                    }
                    txtOut.Text += string.Format("Command not understood: {0}\r\n", cmd);
                }
            }
        }

        int[] tointarray(string value, char sep)
        {
            string[] sa = value.Split(new char[] { sep}, StringSplitOptions.RemoveEmptyEntries);
            List<int> ia = new List<int>();
            for (int i = 0; i < sa.Length; ++i)
            {
                int j;
                if (int.TryParse(sa[i], out j))
                {
                    ia.Add(j);
                }
            }
            return ia.ToArray();
        }

        private string RunTestCode()
        {
            StringBuilder sb = new StringBuilder();

            ParentWindow.DrawingControl.ClearCutPlane();
            if (true)
                ParentWindow.DrawingControl.SetCutPlane(
                    0.0, 0.0, 0.0,
                    0.0, 0.0, 1.0
                    );

            return sb.ToString();
        }

        private void DisplayHelp()
        {
            txtOut.Text += "Commands:\r\n";
            txtOut.Text += "  select [count|list|short] <#startingElement> [Properties...]\r\n";
            txtOut.Text += "  EntityLabel label [recursion]\r\n";
            txtOut.Text += "  IfcType type\r\n";
            txtOut.Text += "  clip [off|<Elevation>|<px>, <py>, <pz>, <nx>, <ny>, <nz>] (unstable feature)\r\n";
            txtOut.Text += "  Visual [list|[on|off <name>]]\r\n";
            txtOut.Text += "  clear [on|off]\r\n";
            
            txtOut.Text += "  test\r\n";
            txtOut.Text += "\r\n";
            txtOut.Text += "Commands are executed on <ctrl>+<Enter>\r\n";
            txtOut.Text += "Lines starting with double slash are ignored\r\n";
        }

        private string ReportType(string type)
        {
            StringBuilder sb = new StringBuilder();
            List<int> Values = QueryEngine.EntititesForType(type, Model);
            sb.AppendFormat("=== Type: {0}, {1} items:\r\n", type, Values.Count);
            sb.AppendFormat("EntityLabels: {0}\r\n", string.Join(",", Values.ToArray()));
            return sb.ToString();
        }

        private string ReportEntity(int EntityLabel, int RecursiveDepth = 0, int IndentationLevel = 0, bool Verbose = false)
        {
            // Debug.WriteLine("EL: " + EntityLabel.ToString());
            StringBuilder sb = new StringBuilder();
            string IndentationHeader = new String('\t', IndentationLevel);
            try
            {
                var entity = Model.Instances[EntityLabel];
                if (entity != null)
                {
                    IfcType ifcType = IfcMetaData.IfcType(entity);
                    var props = ifcType.IfcProperties.Values;

                    sb.AppendFormat(IndentationHeader + "=== {0} [#{1}]\r\n", ifcType, EntityLabel.ToString());

                    foreach (var prop in props)
                    {
                        var PropLabels = ReportProp(sb, IndentationHeader, entity, prop, Verbose);

                        foreach (var PropLabel in PropLabels)
                        {
                            if (
                                PropLabel != EntityLabel &&
                                (RecursiveDepth > 0 || RecursiveDepth < 0)
                                && PropLabel != 0
                                )
                            {
                                sb.Append(ReportEntity(PropLabel, RecursiveDepth - 1, IndentationLevel + 1));
                            }
                        }

                    }
                    var Invs = ifcType.IfcInverses;
                    if (Invs.Count > 0)
                        sb.AppendFormat(IndentationHeader + "= Inverses count: {0}\r\n", Invs.Count);
                    foreach (var inverse in Invs)
                    {
                        ReportProp(sb, IndentationHeader, entity, inverse, Verbose);
                    }
                }
                else
                {
                    sb.AppendFormat(IndentationHeader + "=== Entity #{0} is null\r\n", EntityLabel);
                }
            }
            catch (Exception ex)
            {
                sb.AppendFormat(IndentationHeader + "\r\n{0}\r\n", ex.Message);
            }
            return sb.ToString();
        }

        private static IEnumerable<int> ReportProp(StringBuilder sb, string IndentationHeader, IPersistIfcEntity entity, IfcMetaProperty prop, bool Verbose)
        {
            List<int> RetIds = new List<int>();

            string propName = prop.PropertyInfo.Name;
            if (propName == "Representation")
                Debug.WriteLine("");
            // Debug.WriteLine(propName);
            Type propType = prop.PropertyInfo.PropertyType;

            string ShortTypeName = propType.FullName;
            var m = Regex.Match(ShortTypeName, @"^((?<Mod>.*)`\d\[\[)*Xbim\.(?<Type>[\w\.]*)");
            if (m.Success)
            {
                ShortTypeName = m.Groups["Type"].Value; // + m.Groups["Type"].Value + 
                if (m.Groups["Mod"].Value != string.Empty)
                {
                    string[] GetLast =  m.Groups["Mod"].Value.Split(new string[] {"."}, StringSplitOptions.RemoveEmptyEntries);
                    ShortTypeName += " (" + GetLast[GetLast.Length - 1] + ")";
                }
            }

            // System.Diagnostics.Debug.WriteLine(ShortTypeName);

            object propVal = prop.PropertyInfo.GetValue(entity, null);


            if (propVal == null)
                propVal = "<null>";

            if (prop.IfcAttribute.IsEnumerable)
            {
                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                propVal = propVal.ToString() + " [not an enumerable]";
                if (propCollection != null)
                {
                    propVal = "<empty>";
                    int iCntProp = 0;
                    foreach (var item in propCollection)
                    {
                        iCntProp++;
                        if (iCntProp == 1)
                            propVal = ReportPropValue(item, ref RetIds);
                        else
                        {
                            if (iCntProp == 2)
                            {
                                propVal = "\r\n" + IndentationHeader + "  " + propVal;
                            }
                            propVal += "\r\n" + IndentationHeader + "  " + ReportPropValue(item, ref RetIds);
                        }
                    }
                }
            }
            else
                propVal = ReportPropValue(propVal, ref RetIds);

            if (Verbose)
                sb.AppendFormat(IndentationHeader + "{0} ({1}): {2}\r\n",
                    propName,  // 0
                    ShortTypeName,  // 1
                    propVal // 2
                    );
            else
            {
                if ((string)propVal != "<null>" && (string)propVal != "<empty>")
                {
                    sb.AppendFormat(IndentationHeader + "{0}: {1}\r\n",
                        propName,  // 0
                        propVal // 1
                        );
                }
            }
            return RetIds;
        }

        private static string ReportPropValue(object propVal, ref List<int> RetIds)
        {
            IPersistIfcEntity pe = propVal as IPersistIfcEntity;
            int PropLabel = 0;
            if (pe != null)
            {
                PropLabel = Math.Abs(pe.EntityLabel);
                RetIds.Add(PropLabel);
            }
            string ret = propVal.ToString() + ((PropLabel != 0) ? " [#" + Math.Abs(PropLabel).ToString() + "]" : "");
            return ret;
        }

    }
}
