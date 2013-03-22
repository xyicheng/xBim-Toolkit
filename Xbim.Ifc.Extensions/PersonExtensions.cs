using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ActorResource;

namespace Xbim.Ifc.Extensions
{
    public static class PersonExtensions
    {
        public static string GetFullName(this IfcPerson ifcPerson)
        {
            string name = string.Empty;
            foreach (var item in ifcPerson.PrefixTitles)
            {
                name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
            }

            if (ifcPerson.GivenName.HasValue)
                name += ifcPerson.GivenName + " ";

            foreach (var item in ifcPerson.MiddleNames)
            {
                name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
            }

            if (ifcPerson.FamilyName.HasValue)
                name += ifcPerson.FamilyName + " ";

            foreach (var item in ifcPerson.SuffixTitles)
            {
                name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
            }
            return name;
        }
    }
}
