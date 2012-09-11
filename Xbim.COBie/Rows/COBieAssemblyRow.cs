﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Xbim.COBie.Rows
{

    // TODO: According to NN @ AEC3 AssemblyType should be Column D, not G (i.e. at Ordinal 3)
    [Serializable()]
    public class COBieAssemblyRow : COBieRow
    {
        public COBieAssemblyRow(ICOBieSheet<COBieAssemblyRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName { get; set; }

        [COBieAttributes(4, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "ParentName", 255, COBieAllowedType.AlphaNumeric)]
        public string ParentName { get; set; }

        [COBieAttributes(5, COBieKeyType.None, "", COBieAttributeState.Required, "ChildNames", 255, COBieAllowedType.AlphaNumeric)]
        public string ChildNames { get; set; }

        [COBieAttributes(6, COBieKeyType.None, "", COBieAttributeState.System, "AssemblyType", 255, COBieAllowedType.AlphaNumeric)]
        public string AssemblyType { get; set; }

        [COBieAttributes(7, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.As_Specified, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }




    }
}
