﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

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

                foreach (var cmd_f in CommandArray)
                {
                    var cmd = cmd_f;
                    int i = cmd.IndexOf("//");
                    if (i > 0)
                    {
                        cmd = cmd.Substring(0, i);
                    }
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
                        catch (Exception)
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
                        catch (Exception)
                        {
                        }

                        txtOut.Text += ReportEntity(v, recursion) + "\r\n";
                        continue;
                    }

                    m = Regex.Match(cmd, @"(IfcSchema|is) (?<type>.+)", RegexOptions.IgnoreCase);
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
                            if (ret.Count() > iIndex)
                            {
                                int iVal = ret.ElementAt(iIndex);
                                ret = new int[] { iVal };
                            }
                            else
                                ret = new int[] { };
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

                    m = Regex.Match(cmd, @"zoom (" +
                        @"(?<RegionName>.+$)" +
                        ")", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string RName = m.Groups["RegionName"].Value;
                        var regionData = Model.GetGeometryData(Xbim.XbimExtensions.XbimGeometryType.Region).FirstOrDefault();
                        if (regionData == null)
                        {
                            txtOut.Text += "data not found\r\n";
                        }
                        XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                        var reg = regions.Where(x => x.Name == RName).FirstOrDefault();
                        if (reg != null)
                        {
                            XbimMatrix3D mcp = XbimMatrix3D.Copy(ParentWindow.DrawingControl.wcsTransform);
                            var bb = reg.Centre;
                            var tC = mcp.Transform(reg.Centre);
                            var tS = mcp.Transform(reg.Size);
                            XbimRect3D r3d = new XbimRect3D(
                                tC.X - tS.X / 2, tC.Y - tS.Y / 2, tC.Z - tS.Z / 2,
                                tS.X, tS.X, tS.Z
                                );
                            ParentWindow.DrawingControl.ZoomTo(r3d);
                            ParentWindow.Activate();
                            continue;
                        }
                        else
                        {
                            txtOut.Text += string.Format("Something wrong with region name: '{0}'\r\n", RName);
                            txtOut.Text += "Names that should work are: \r\n";
                            foreach (var str in regions)
                            {
                                txtOut.Text += string.Format(" - '{0}'\r\n", str.Name);
                            }
                            continue;
                        }
                    }

                    m = Regex.Match(cmd, @"clip off", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        ParentWindow.DrawingControl.ClearCutPlane();
                        txtOut.Text += "Clip removed\r\n";
                        ParentWindow.Activate();
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
                        "|" +
                        @"(?<StoreyName>.+$)" +
                        ")", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        double px = 0, py = 0, pz = 0;
                        double nx = 0, ny = 0, nz = -1;

                        if (m.Groups["elev"].Value != string.Empty)
                        {
                            pz = Convert.ToDouble(m.Groups["elev"].Value);
                        }
                        else if (m.Groups["StoreyName"].Value != string.Empty)
                        {
                            string msg = "";
                            string storName = m.Groups["StoreyName"].Value;
                            var storey = Model.Instances.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>().Where(x => x.Name == storName).FirstOrDefault();
                            if (storey != null)
                            {
                                //get the object position data (should only be one)
                                Xbim.XbimExtensions.XbimGeometryData geomdata = Model.GetGeometryData(storey.EntityLabel, Xbim.XbimExtensions.XbimGeometryType.TransformOnly).FirstOrDefault();
                                if (geomdata != null)
                                {
                                    Xbim.Common.Geometry.XbimPoint3D pt = new Xbim.Common.Geometry.XbimPoint3D(0, 0, geomdata.Transform.OffsetZ);
                                    Xbim.Common.Geometry.XbimMatrix3D mcp = Xbim.Common.Geometry.XbimMatrix3D.Copy(ParentWindow.DrawingControl.wcsTransform);
                                    var transformed = mcp.Transform(pt);
                                    msg = string.Format("Clip 1m above storey elevation {0} (height: {1})\r\n", pt.Z, transformed.Z + 1);
                                    pz = transformed.Z + 1;
                                }
                            }
                            if (msg == "")
                            {
                                txtOut.Text += string.Format("Something wrong with storey name: '{0}'\r\n", storName);
                                txtOut.Text += "Names that should work are: \r\n";
                                var strs = Model.Instances.OfType<Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey>();
                                foreach (var str in strs)
	                            {
                                    txtOut.Text += string.Format(" - '{0}'\r\n", str.Name);
	                            }
                                continue;
                            }
                            txtOut.Text += msg;
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


                    m = Regex.Match(cmd, @"SimplifyGUI", RegexOptions.IgnoreCase);
                    if (m.Success)  
                    {
                        XbimXplorer.Simplify.IfcSimplify s = new Simplify.IfcSimplify();
                        s.Show();
                        continue;
                    }

                    m = Regex.Match(cmd, @"test", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        int iPass = -728;
                        txtOut.Text = RunTestCode(iPass);
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

        private string RunTestCode(int i)
        {
            StringBuilder sb = new StringBuilder();
            byte[] bval = new byte[] {196};
            var eBase = Encoding.GetEncoding("iso-8859-1");
            var outV = eBase.GetChars(bval, 0, 1);

            bval = new byte[] { 0, 196 };
            var e16 = Encoding.GetEncoding("unicodeFFFE");
            var out2 = e16.GetChars(bval, 0, 2);

            //var v = Model.Instances.OfType<Xbim.Ifc2x3.MaterialResource.IfcMaterialLayerSetUsage>(true).Where(ent => ent.ForLayerSet.EntityLabel == i);
            //foreach (var item in v)
            //{
            //    sb.AppendFormat("{0}\r\n", item.EntityLabel);
            //}

            return sb.ToString();
        }

        private void DisplayHelp()
        {
            txtOut.Text += "Commands:\r\n";
            txtOut.Text += "  select [count|list|short] <#startingElement> [Properties...]\r\n";
            txtOut.Text += "  EntityLabel label [recursion]\r\n";
            txtOut.Text += "  IfcSchema type\r\n";
            txtOut.Text += "  clip [off|<Elevation>|<px>, <py>, <pz>, <nx>, <ny>, <nz>|<Storey name>] (unstable feature)\r\n";
            txtOut.Text += "  zoom <Region name>\r\n";
            txtOut.Text += "  Visual [list|[on|off <name>]]\r\n";
            txtOut.Text += "  clear [on|off]\r\n";
            txtOut.Text += "  SimplifyGUI - rough GUI interface to simplify IFC files for debugging purposes.\r\n";
            txtOut.Text += "\r\n";
            txtOut.Text += "Commands are executed on <ctrl>+<Enter>.\r\n";
            txtOut.Text += "Lines starting with double slash (//) are ignored.\r\n";
            txtOut.Text += "If a portion of text is selected, only selected text will be the executed.\r\n";
        }

        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private string ReportType(string type, string indentationHeader = "")
        {
            StringBuilder sb = new StringBuilder();
            List<int> Values = QueryEngine.EntititesForType(type, Model);
            IfcType ot = IfcMetaData.IfcType(type.ToUpper());
            if (ot != null)
            {
                sb.AppendFormat(indentationHeader + "=== {0}\r\n", ot.Name);
                // sb.AppendFormat(indentationHeader + "Xbim Type Id: {0}\r\n", ot.TypeId);
                if (ot.IfcSuperType != null)
                    sb.AppendFormat(indentationHeader + "Supertype: {0}\r\n", ot.IfcSuperType.Name);
                if (ot.IfcSubTypes.Count > 0)
                {
                    sb.AppendFormat(indentationHeader + "Subtypes: {0}\r\n", ot.IfcSubTypes.Count);
                    foreach (var item in ot.IfcSubTypes)
                    {
                        sb.AppendFormat(indentationHeader + "- {0}\r\n", item);
                    }
                }

                sb.AppendFormat(indentationHeader + "Interfaces: {0}\r\n", ot.Type.GetInterfaces().Count());
                foreach (var item in ot.Type.GetInterfaces())
                {
                    sb.AppendFormat(indentationHeader + "- {0}\r\n", item.Name);
                }
                sb.AppendFormat(indentationHeader + "Properties: {0}\r\n", ot.IfcProperties.Count());
                foreach (var item in ot.IfcProperties.Values)
                {
                    sb.AppendFormat(indentationHeader + "- {0} ({1})\r\n", item.PropertyInfo.Name, CleanPropertyName(item.PropertyInfo.PropertyType.FullName));
                }
                sb.AppendFormat(indentationHeader + "Inverses: {0}\r\n", ot.IfcInverses.Count());
                foreach (var item in ot.IfcInverses)
                {
                    sb.AppendFormat(indentationHeader + "- {0} ({1})\r\n", item.PropertyInfo.Name, CleanPropertyName(item.PropertyInfo.PropertyType.FullName));
                }

                sb.AppendFormat("\r\n");
            }
            else
            {
                // test to see if it's a select type...

                Module ifcModule2 = typeof(IfcMaterialSelect).Module;
                var SelectType = ifcModule2.GetTypes().Where(
                        t => t.Name.Contains(type)
                        ).FirstOrDefault();

                if (SelectType != null)
                {
                    sb.AppendFormat("=== {0} is a Select type\r\n", type);
                    Module ifcModule = typeof(IfcActor).Module;
                    IEnumerable<Type> types = ifcModule.GetTypes().Where(
                            t => t.GetInterfaces().Contains(SelectType)
                            );
                    foreach (var item in types)
                    {
                        sb.Append(ReportType(item.Name, indentationHeader + "  "));
                    }
                }
            }

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

            string ShortTypeName = CleanPropertyName(propType.FullName);
            

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

        private static string CleanPropertyName(string ShortTypeName)
        {
            var m = Regex.Match(ShortTypeName, @"^((?<Mod>.*)`\d\[\[)*Xbim\.(?<Type>[\w\.]*)");
            if (m.Success)
            {
                ShortTypeName = m.Groups["Type"].Value; // + m.Groups["Type"].Value + 
                if (m.Groups["Mod"].Value != string.Empty)
                {
                    string[] GetLast = m.Groups["Mod"].Value.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                    ShortTypeName += " (" + GetLast[GetLast.Length - 1] + ")";
                }
            }
            return ShortTypeName;
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
