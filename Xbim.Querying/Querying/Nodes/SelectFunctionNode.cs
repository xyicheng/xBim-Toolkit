using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Interpreter.Ast;
using Irony.Parsing;

namespace Xbim.Querying.Nodes
{
    public class SelectFunctionNode : LeftObjectNode
    {
        ParseTreeNode function = null;
        
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog

            string functionName = function.ChildNodes[0].FindTokenAndGetText();
            switch (functionName.ToLowerInvariant())
            {
                case "count":
                    IEnumerable<object> o = Left as IEnumerable<object>;
                    if (o != null)
                        retval = o.Count();
                    else
                        retval = -1;
                    break;
                case "where":
                    throw new Exception("Not implemented: Where");
                    break;
                default:
                    throw new Exception("Not implemented: " + functionName);
            }

            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            function = treeNode;
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
