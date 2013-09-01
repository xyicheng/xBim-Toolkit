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
    public class SelectMemberAccessNode : AstNode
    {
        public object Left;
        SelectFunctionNode _TheFunction = null;
        SelectPropertyNode _PropNode = null;

        SelectMemberAccessNode _queue = null;
        
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            if (Left != null)
            {
                if (_TheFunction != null)
                {
                    _TheFunction.BaseObject = Left;
                    retval = _TheFunction.Evaluate(thread);
                }
                else if (_PropNode != null)
                {
                    _PropNode.BaseObject = Left;
                    retval = _PropNode.Evaluate(thread);
                }
            }
            else if (_queue != null)
            {

            }
            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();
            var functionOrId = AddChild("FunctionOrId", nodes[0]);
            if (functionOrId is SelectFunctionNode)
            {
                _TheFunction = (SelectFunctionNode)functionOrId;
            }
            else if (functionOrId is SelectPropertyNode)
            {
                _PropNode = (SelectPropertyNode)functionOrId;
            }
            else
            {
                throw new Exception("Unmanaged type in selectmemberaccessnode");
            }

            var child = AddChild("Queue", nodes[1]);
            string sval = nodes[1].FindTokenAndGetText();
            if (child is SelectMemberAccessNode)
            {
                _queue = (SelectMemberAccessNode)child;
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