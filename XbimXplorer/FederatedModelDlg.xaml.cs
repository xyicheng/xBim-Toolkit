using Microsoft.Win32;
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
using Xbim.Ifc2x3.ActorResource;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using XbimXplorer.Dialogs;

namespace XbimXplorer
{
    /// <summary>
    /// Interaction logic for FederatedModelDlg.xaml
    /// </summary>
    public partial class FederatedModelDlg : Window
    {
        public FederatedModelDlg()
        {
            InitializeComponent();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            XbimModel model = DataContext as XbimModel;
            if (model != null)
            {
                AddFederatedModel fdlg = new AddFederatedModel();
                bool? done = fdlg.ShowDialog();
                if (done.HasValue && done.Value == true)
                {
                    string fileName = fdlg.FileName;
                    string ext = System.IO.Path.GetExtension(fileName);
                    using (XbimModel refM = new XbimModel())
                    {
                        if (string.Compare(ext, ".xbim", true) != 0)
                        {
                            refM.CreateFrom(fileName, null, null, true);
                            XbimMesher.GenerateGeometry(refM);
                            fileName = System.IO.Path.ChangeExtension(fileName, "xbim");
                        }
                    }
                    IfcRole role = fdlg.Role;
                    if (role == IfcRole.UserDefined)
                        model.AddModelReference(fileName, fdlg.OrganisationName, fdlg.RoleName);
                    else
                        model.AddModelReference(fileName, fdlg.OrganisationName, role);
                }
            }
        }
    }
}
