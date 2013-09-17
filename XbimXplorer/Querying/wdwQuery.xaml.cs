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
                        catch (Exception )
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

                    m = Regex.Match(cmd, @"(ifctype|it) (?<type>.+)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string type = m.Groups["type"].Value;
                        txtOut.Text += ReportType(type);
                        continue;
                    }

                    m = Regex.Match(cmd, @"(select|se) (?<count>count )*(?<start>([\d,]+|[^ ]+)) *(?<props>.*)", RegexOptions.IgnoreCase);
                    if (m.Success)
                    {
                        string start = m.Groups["start"].Value;
                        string props = m.Groups["props"].Value;
                        string count = m.Groups["count"].Value;
                        

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
                            //TreeQueryItem tq = new TreeQueryItem(items, props);
                            //ret = tq.Run(Model);
                        }
                        if (count.ToLower() == "count ")
                        {
                            txtOut.Text += string.Format("Count: {0}\r\n", ret.Count());
                        }
                        else
                        {
                            foreach (var item in ret)
                            {
                                txtOut.Text += ReportEntity(item, 0) + "\r\n";
                            }
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
            Xbim.Ifc2x3.ActorResource.IfcOrganization org = (Xbim.Ifc2x3.ActorResource.IfcOrganization)Model.Instances[50];
            var v = org.IsRelatedBy;
            sb.AppendLine(v.Count().ToString());

            var v2 = org.Relates;
            sb.AppendLine(v2.Count().ToString());

            var v3 = org.Engages;
            sb.AppendLine(v3.Count().ToString());

            return sb.ToString();
        }

        private void DisplayHelp()
        {
            txtOut.Text += "Commands:\r\n";
            txtOut.Text += "  EntityLabel label [recursion]\r\n";
            txtOut.Text += "  IfcType type\r\n";
            txtOut.Text += "  clear\r\n";
            txtOut.Text += "  test\r\n";
            txtOut.Text += "\r\n";
            txtOut.Text += "Commands are executed on <ctrl>+<Enter>\r\n";
        }

        private string ReportType(string type)
        {
            StringBuilder sb = new StringBuilder();
            List<int> Values = QueryEngine.EntititesForType(type, Model);
            sb.AppendFormat("=== Type: {0}, {1} items:\r\n", type, Values.Count);
            sb.AppendFormat("EntityLabels: {0}\r\n", string.Join(",", Values.ToArray()));
            return sb.ToString();
        }

        

        private string ReportEntity(int EntityLabel, int RecursiveDepth = 0, int IndentationLevel = 0)
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
                        var PropLabels = ReportProp(sb, IndentationHeader, entity, prop);

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
                        ReportProp(sb, IndentationHeader, entity, inverse);
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

        private static IEnumerable<int> ReportProp(StringBuilder sb, string IndentationHeader, IPersistIfcEntity entity, IfcMetaProperty prop)
        {
            List<int> RetIds = new List<int>();

            string propName = prop.PropertyInfo.Name;
            if (propName == "RelatedBuildingElement")
                Debug.WriteLine("");
            Debug.WriteLine(propName);
            Type propType = prop.PropertyInfo.PropertyType;

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


            sb.AppendFormat(IndentationHeader + "{0} ({1}): {2}\r\n",
                propName,  // 0
                propType.Name,  // 1
                propVal // 2
                );
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
