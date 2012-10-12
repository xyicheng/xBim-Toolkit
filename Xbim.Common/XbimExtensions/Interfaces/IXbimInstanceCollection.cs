using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;

namespace Xbim.XbimExtensions.Interfaces
{
    public interface IXbimInstanceCollection : IEnumerable<int>
    {
        IEnumerable<T> Where<T>(Expression<Func<T, bool>> expr) where T : IPersistIfcEntity;
        IEnumerable<T> OfType<T>() where T : IPersistIfcEntity;
        IEnumerable<T> OfType<T>(bool activate) where T : IPersistIfcEntity;
        IPersistIfcEntity New(Type t, int label);
        T New<T>(InitProperties<T> initPropertiesFunc) where T : IPersistIfcEntity, new();
        T New<T>() where T : IPersistIfcEntity, new();
        IPersistIfcEntity this[int label] { get; }
        long Count { get; }
        
    }
}
