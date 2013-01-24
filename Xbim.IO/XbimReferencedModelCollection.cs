using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Xbim.Common.Exceptions;
namespace Xbim.IO
{
    public class XbimReferencedModelCollection:KeyedCollection<string,XbimReferencedModel>
    {
        protected override string GetKeyForItem(XbimReferencedModel item)
        {
            return item.Identifier;
        }

        internal Ifc2x3.MeasureResource.IfcIdentifier NextIdentifer()
        {
            for (short i = 1; i < short.MaxValue; i++)
            {
                if (!this.Contains(i.ToString()))
                    return i.ToString();
            }
            throw new XbimException("Too many Reference Models added");
        }
    }
}
