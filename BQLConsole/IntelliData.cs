using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BQLConsole
{
    public class IntelliData
    {
        public Dictionary<string, List<string>> Predict{ get; set; }
        public List<string> KeyWords { get; set; }

        public IntelliData()
        {
            Predict = new Dictionary<string, List<string>>();
            KeyWords = new List<string>();

            
            KeyWords.AddRange(new List<string>() {  "EQUAL",
                                                    "EQUALS",
                                                    "IS",
                                                    "TO",
                                                    "NOT",
                                                    "DOES",
                                                    "DOESN'T",
                                                    "GREATER",
                                                    "THAN",
                                                    "LESS",
                                                    "OR",
                                                    "AND",
                                                    "CONTAIN",
                                                    "CONTAINS",
                                                    "LIKE",
                                                    "ISN'T",
                                                    "THE",
                                                    "SAME",
                                                    "DELETED",
                                                    "INSERTED",
                                                    "EDITED",
                                                    "SELECT",
                                                    "SET",
                                                    "FOR",
                                                    "WHERE",
                                                    "CREATE",
                                                    "WITH",
                                                    "NAME",
                                                    "CALLED",
                                                    "DESCRIPTION",
                                                    "DESCRIBED",
                                                    "AS",
                                                    "NEW",
                                                    "ADD",
                                                    "REMOVE",
                                                    "FROM",
                                                    "EXPORT",
                                                    "DUMP",
                                                    "COUNT",
                                                    "CLEAR",
                                                    "OPEN",
                                                    "CLOSE",
                                                    "SAVE",
                                                    "IN",
                                                    "IT",
                                                    "EVERY",
                                                    "VALIDATE",
                                                    "COPY",
                                                    "PROPERTY",
                                                    "SET",
                                                    "PROPERTY_SET",
                                                    "PROPERTYSET",
                                                    "PREDEFINED",
                                                    "PREDEFINEDTYPE",
                                                    "PREDEFINED_TYPE",
                                                    "TYPE",
                                                    "FAMILY",
                                                    "MATERIAL",
                                                    "THICKNESS",
                                                    "FILE",
                                                    "MODEL",
                                                    "REFERENCE",
                                                    "CLASSIFICATION",
                                                    "GROUP",
                                                    "ORGANIZATION",
                                                    "OWNER",
                                                    "LAYER",
                                                    "LAYERSET",
                                                    "LAYER_SET",
                                                    "NULL",
                                                    "UNDEFINED",
                                                    "UNKNOWN",
                                                    "DEFINED",
                                                    "SYSTEM",
                                                    "TRUE",
                                                    "FALSE",
                                                    //======multi=====
                                                    //"PREDEFINED TYPE",
                                                    //"LAYER SET",
                                                    //"DESCRIBED AS",
                                                    //"WITH NAME",
                                                    //"THE SAME",
                                                    //"DOES NOT CONTAIN",
                                                    //"DOESN'T CONTAIN",
                                                    //"DOES NOT EQUAL",
                                                    //"DOESN'T EQUAL",
                                                    //"IS NOT LIKE",
                                                    //"IS LIKE",
                                                    //"IS GREATER THAN",
                                                    //"IS LESS THAN",
                                                    //"IS GREATER THAN OR EQUAL TO",
                                                    //"IS LESS THAN OR EQUAL TO",
                                                    //"IS EQUAL TO",
                                                    //"IS NOT EQUAL TO",
                                                    //"IS NOT",
                                                    //"ISN'T LIKE",
                                                    });

            //Predict Lists
            Predict["PREDEFINED"] = new List<string>() { "TYPE" };
            Predict["LAYER"] = new List<string>() { "SET" };
            Predict["DESCRIBED"] = new List<string>() { "AS" };
            Predict["WITH"] = new List<string>() { "NAME" };
            Predict["THE"] = new List<string>() { "SAME" };
            Predict["DOES"] = new List<string>() { "NOT" };
            Predict["NOT"] = new List<string>() { "CONTAIN", "EQUAL", "EVERY", "LIKE", "DEFINED" };
            Predict["DOESN'T"] = new List<string>() { "CONTAIN", "EQUAL" };
            Predict["IS"] = new List<string>() { "NOT", "LIKE", "GREATER", "LESS", "EQUAL", "NEW", "EVERY", "DEFINED" };
            Predict["ISN'T"] = new List<string>() { "LIKE" };
            Predict["GREATER"] = new List<string>() { "THAN" };
            Predict["LESS"] = new List<string>() { "THAN" };
            Predict["EQUAL"] = new List<string>() { "TO" };
            Predict["THAN"] = new List<string>() { "OR" };
            Predict["OR"] = new List<string>() { "EQUAL", "GREATER", "LESS", "IS", "DESCRIBED", "DESCRIPTION", "THICKNESS" };
            Predict["AND"] = new List<string>() { "EQUAL", "GREATER", "LESS", "IS", "DESCRIBED", "DESCRIPTION", "THICKNESS", "OWNER" };
            
            Predict["OPEN"] = new List<string>() { "MODEL" };
            Predict["MODEL"] = new List<string>() { "FROM", "TO", "OWNER", "ORGANIZATION" };
            Predict["OWNER"] = new List<string>() { "IS" };
            Predict["ORGANIZATION"] = new List<string>() { "IS" };
            Predict["FROM"] = new List<string>() { "FILE" };
            Predict["CLOSE"] = new List<string>() { "MODEL" };
            Predict["VALIDATE"] = new List<string>() { "MODEL" };
            Predict["SAVE"] = new List<string>() { "MODEL" };
            Predict["TO"] = new List<string>() { "FILE", "MODEL", "NULL" };
            Predict["ADD"] = new List<string>() { "REFERENCE" };
            Predict["REFERENCE"] = new List<string>() { "MODEL" };

            Predict["WHERE"] = new List<string>() { "GROUP", "CLASSIFICATION", "ORGANIZATION", "TYPE", "NAME", "DESCRIPTION", "THICKNESS", "PREDEFINED", "PREDEFINED_TYPE", "FAMILY" };
            Predict["NEW"] = new List<string>() { "MATERIAL", "GROUP", "CLASSIFICATION", "ORGANIZATION", "SYSTEM" };
            Predict["MATERIAL"] = new List<string>() { "LAYER_SET", "LAYER", "CONTAINS", "IS" };
            Predict["CREATE"] = new List<string>() { "NEW", "CLASSIFICATION", "GROUP" };
            Predict["SELECT"] = new List<string>() { "INTO", "IS", "EVERY" };
            Predict["GROUP"] = new List<string>() { "NAME", "PREDEFINED", "WITH" };
            Predict["TYPE"] = new List<string>() { "NAME", "PREDEFINED", "DESCRIPTION", "IS" };
            Predict["DESCRIPTION"] = new List<string>() { "CONTAINS", "TO", "IS" };
            Predict["NAME"] = new List<string>() { "IS", "TO", "CONTAINS", "EQUAL" };
            Predict["PREDEFINED_TYPE"] = new List<string>() { "IS" }; 
            Predict["SET"] = new List<string>() { "MATERIAL", "NAME", "DESCRIPTION" };
            Predict["IN"] = new List<string>() { "PROPERTY" };
            Predict["PROPERTY"] = new List<string>() { "SET" };
            Predict["CLASSIFICATION"] = new List<string>() { "NRM", "UNICLASS" };
            
            Predict["$"] = new List<string>() { "IS", "TO", "FOR", "FROM" };

        }
    }
}
