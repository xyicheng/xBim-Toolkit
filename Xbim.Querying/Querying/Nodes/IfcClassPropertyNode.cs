using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Interpreter.Ast;
using Irony.Parsing;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Querying.Extensions;

namespace Xbim.Querying.Nodes
{
    public class IfcClassPropertyNode : AstNode
    {
        string propName = "";
        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            try
            {
                IPersistIfcEntity entity = thread.CurrentScope.Parameters[0] as IPersistIfcEntity;
                retval = entity.GetPropertyByName(propName);
            }
            catch (Exception)
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
            if (nodes.Count > 0)
            {
                propName = nodes[0].FindTokenAndGetText();
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
