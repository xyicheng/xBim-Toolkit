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
    public class SelectStatement : AstNode
    {
        AstNode _root;
        SelectMemberAccessNode _Queue;
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            
            if (_root != null)
                retval = _root.Evaluate(thread);

            if (_Queue != null)
            {
                _Queue.Left = retval;
                retval = _Queue.Evaluate(thread);
            }

            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();

            if (nodes.Count > 0)
            {
                if (nodes[0].AstNode != null)
                {
                    _root = (AstNode)nodes[0].AstNode;
                    AddChild("Root", nodes[0]);
                }
                
                if (nodes.Count > 1 && nodes[1].ChildNodes.Count > 0 )
                {
                    _Queue = (SelectMemberAccessNode)nodes[1].ChildNodes[0].AstNode;
                    AddChild("Queue", nodes[1].ChildNodes[0]);
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
