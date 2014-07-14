using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.WeXplorer.MVC4.Models
{
    public interface IXbimJsonResult
    {
        string Type { get; }
        uint? Label { get; }

    }
}
