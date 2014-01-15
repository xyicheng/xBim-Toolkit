using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ActorResource;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.Ifc2x3.Extensions
{
    public static class ActorSelectExtensions
    {

        public static String RoleName(this IfcActorSelect actor)
        {
            if (actor is IfcPerson)
            {
                return ((IfcPerson)actor).RolesString;
            }
            else if (actor is IfcPersonAndOrganization)
            {
                return ((IfcPersonAndOrganization)actor).RolesString;
               
            }
            else if (actor is IfcOrganization)
            {
                return ((IfcOrganization)actor).RolesString;
            }
            return "";
        }
    }
}
