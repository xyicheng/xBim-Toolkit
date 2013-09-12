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
    public class SelectChildrenNodeMot : AstNode
    {
        ParseTreeNode thisNode = null;
        SelectChildrenNodeMot child = null;
        // public object parentNodeValue = null;
        
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            
            //if (parentNodeValue == null)
            //    return null;

            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            if (nodes.Count == 2)
            {
                if (nodes[0] != null)
                {
                    thisNode = nodes[0];
                    // AddChild(string.Empty, nodes[0]);
                }
                if (nodes[1].AstNode != null)
                {
                    child = (SelectChildrenNodeMot)nodes[1].AstNode;
                    AddChild(string.Empty, nodes[1]);
                    // this.AsString += "(child children: " + child.ChildNodes.Count);
                }
            }
            else
                this.AsString = "nochildren";
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
