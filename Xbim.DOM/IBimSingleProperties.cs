using System;
using Xbim.DOM.PropertiesQuantities;
namespace Xbim.DOM
{
    public interface IBimSingleProperties
    {
        System.Collections.Generic.IEnumerable<IBimPropertySingleValue> FlatProperties { get; }
        bool? GetProperty_bool(string propertySetName, string propertyName);
        double? GetProperty_double(string propertySetName, string propertyName);
        long? GetProperty_long(string propertySetName, string propertyName);
        string GetProperty_string(string propertySetName, string propertyName);
        void SetProperty_bool(string propertySetName, string propertyName, bool? value);
        void SetProperty_double(string propertySetName, string propertyName, double? value);
        void SetProperty_long(string propertySetName, string propertyName, long? value);
        void SetProperty_string(string propertySetName, string propertyName, string value);
        void SetProperty(IBimPropertySingleValue property);
    }
}
