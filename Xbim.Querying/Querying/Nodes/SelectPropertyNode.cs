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
using Xbim.Querying.Extensions;

namespace Xbim.Querying.Nodes
{
    public class SelectPropertyNode : LeftObjectNode
    {
        ParseTreeNode property = null;
        LeftObjectNode _queue = null;

        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            // List<object> retval = new List<object>();
            object retval = null;
            thread.CurrentNode = this;  //standard prolog

            string propertyName = property.ChildNodes[0].FindTokenAndGetText();
            
            if (Left is IEnumerable<object>)
            {
                IEnumerable<object> o = Left as IEnumerable<object>;
                foreach (var origItem in o)
                {
                    IPersistIfcEntity entity = origItem as IPersistIfcEntity;
                    retval = entity.GetPropertyByName(propertyName);
                }
            }
            else if (Left is IPersistIfcEntity)
            {
                IPersistIfcEntity entity = Left as IPersistIfcEntity;
                retval = entity.GetPropertyByName(propertyName);
            }
            thread.CurrentNode = Parent; //standard epilog
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
