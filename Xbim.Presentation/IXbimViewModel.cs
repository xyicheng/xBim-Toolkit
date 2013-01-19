using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Xbim.XbimExtensions.Interfaces;
using System.ComponentModel;

namespace Xbim.Presentation
{
    public interface IXbimViewModel : INotifyPropertyChanged
    {
        IEnumerable<IXbimViewModel> Children { get; }
        string Name {get;}
        int EntityLabel { get; }
        IPersistIfcEntity Entity { get; }

        bool IsExpanded { get; set; }
        bool IsSelected { get; set; }
    }
}
