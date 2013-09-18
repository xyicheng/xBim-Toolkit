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
    public class SelectMemberAccessNode : LeftObjectNode
    {
        SelectFunctionNode _TheFunction = null;
        SelectPropertyNode _PropNode = null;

        LeftObjectNode _queue = null;
        
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            if (Left != null)
            {
                if (_TheFunction != null)
                {
                    _TheFunction.Left = Left;
                    retval = _TheFunction.Evaluate(thread);
                }
                else if (_PropNode != null)
                {
                    _PropNode.Left = Left;
                    retval = _PropNode.Evaluate(thread);
                }
            }
            if (_queue != null)
            {
                _queue.Left = retval;
                retval = _queue.Evaluate(thread);
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
            var functionOrProperty = AddChild("FunctionOrId", nodes[0]);
            if (functionOrProperty is SelectFunctionNode)
            {
                _TheFunction = (SelectFunctionNode)functionOrProperty;
            }
            else if (functionOrProperty is SelectPropertyNode)
            {
                _PropNode = (SelectPropertyNode)functionOrProperty;
            }
            else
            {
                throw new Exception("Unmanaged type in selectmemberaccessnode");
            }
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