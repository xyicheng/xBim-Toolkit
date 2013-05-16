using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO.TreeView
{
    public interface IXbimViewModel : INotifyPropertyChanged
    {
        IEnumerable<IXbimViewModel> Children { get; }
        string Name { get; }
        int EntityLabel { get; }
        IPersistIfcEntity Entity { get; }

        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
    }
}
