using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ActorResource;

namespace Xbim.Ifc2x3.Extensions
{
    public static class PersonExtensions
    {
        public static string GetFullName(this IfcPerson ifcPerson)
        {
            string name = string.Empty;
            if (ifcPerson.PrefixTitles != null)
            {
                foreach (var item in ifcPerson.PrefixTitles)
                {
                    name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
                } 
            }

            if (ifcPerson.GivenName.HasValue)
                name += ifcPerson.GivenName + " ";

            if (ifcPerson.MiddleNames != null)
            {
                foreach (var item in ifcPerson.MiddleNames)
                {
                    name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
                } 
            }

            if (ifcPerson.FamilyName.HasValue)
                name += ifcPerson.FamilyName + " ";

            if (ifcPerson.SuffixTitles != null)
            {
                foreach (var item in ifcPerson.SuffixTitles)
                {
                    name += string.IsNullOrEmpty(item) ? "" : item.ToString() + " ";
                } 
            }
            return name;
        }
    }
}
