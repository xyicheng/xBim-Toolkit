using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Querying.Nodes
{
    public class SelectPropertyNode : LeftObjectNode
    {
        ParseTreeNode property = null;
        LeftObjectNode _queue = null;

        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            List<object> retval = new List<object>();
            thread.CurrentNode = this;  //standard prolog

            string propertyName = property.ChildNodes[0].FindTokenAndGetText();
            IEnumerable<object> o = Left as IEnumerable<object>;
            if (o != null)
            {
                foreach (var origItem in o)
                {
                    IPersistIfcEntity entity = origItem as IPersistIfcEntity;
                    IfcType ifcType = IfcMetaData.IfcType(entity);
                    var prop = ifcType.IfcProperties.Where(x => x.Value.PropertyInfo.Name == propertyName).FirstOrDefault().Value;
                    if (prop == null) // otherwise test inverses
                    {
                        prop = ifcType.IfcInverses.Where(x => x.PropertyInfo.Name == propertyName).FirstOrDefault();
                    }
                    // then populate the return value
                    if (prop != null)
                    {
                        object propVal = prop.PropertyInfo.GetValue(entity, null);
                        if (propVal != null)
                        {
                            if (prop.IfcAttribute.IsEnumerable)
                            {
                                IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                                if (propCollection != null)
                                {
                                    foreach (var item in propCollection)
                                    {
                                        IPersistIfcEntity pe = item as IPersistIfcEntity;
                                        retval.Add(pe);
                                    }
                                }
                            }
                            else
                            {
                                IPersistIfcEntity pe = propVal as IPersistIfcEntity;
                                retval.Add(pe);
                            }
                        }
                    }
                }
            }
            thread.CurrentNode = Parent; //standard epilog
            if (retval.Count == 0)
                return null;
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            string sVal = treeNode.FindTokenAndGetText();

            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            property = treeNode;

            if (nodes.Count > 1)
            {
                var child = AddChild("Queue", nodes[1]);
                string sval = nodes[1].FindTokenAndGetText();
                if (child is LeftObjectNode)
                {
                    _queue = (LeftObjectNode)child;
                }
            }

        }

        // fairly default behaviour
        public override void SetIsTail()
        {
            base.SetIsTail();
            if (ChildNodes.Count > 0)
                ChildNodes[ChildNodes.Count - 1].SetIsTail();
        }
    }
}
