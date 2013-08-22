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
    public class SelectExpressionNode : AstNode
    {
        AstNode actualnode = null;
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            if (nodes.Count == 1)
            {
                actualnode = (AstNode)nodes[0].AstNode;
                AddChild(string.Empty, nodes[0]);
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
