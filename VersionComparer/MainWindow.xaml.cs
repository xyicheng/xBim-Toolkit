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
using Xbim.Analysis;
using Xbim.IO;

namespace VersionComparer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartComparison_Click(object sender, RoutedEventArgs e)
        {
            VersionComparison vc = new VersionComparison();
            vc.OnMessage += (x) => {
                Application.Current.Dispatcher.BeginInvoke(new Action( () => Messages.Items.Add(x)));
            };

            XbimModel Base = new XbimModel();
            XbimModel Revision = new XbimModel();

            Base.Open(BaselineTextBox.Text, Xbim.XbimExtensions.XbimDBAccess.Read);
            Revision.Open(RevisedTextBox.Text, Xbim.XbimExtensions.XbimDBAccess.Read);

            Messages.Items.Clear();
            vc.StartComparison(Base, Revision, Filter.Text);
        }

        private String BrowseFile()
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "Xbim Files (.xbim)|*.xbim";

            Nullable<bool> result = dlg.ShowDialog();
            if (result == true)
            {
                return dlg.FileName;
            }
            else {
                return String.Empty;
            }
        }
        private void BrowseBaseline_Click(object sender, RoutedEventArgs e)
        {
            BaselineTextBox.Text = BrowseFile();
        }

        private void BrowseRevision_Click(object sender, RoutedEventArgs e)
        {
            RevisedTextBox.Text = BrowseFile();
        }
    }
}
