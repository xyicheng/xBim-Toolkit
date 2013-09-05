using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Ast;
using Irony.Interpreter;
using Irony.Interpreter.Ast;
using Irony.Parsing;

namespace Xbim.Querying.Nodes
{
    public class SelectFunctionNode : LeftObjectNode
    {
        ParseTreeNode function = null;
        AstNode[] Arguments = null;

        protected override object DoEvaluate(Irony.Interpreter.ScriptThread thread)
        {
            object retval = null;
            thread.CurrentNode = this;  //standard prolog
            IEnumerable<object> o = null;

            string functionName = function.ChildNodes[0].FindTokenAndGetText();
            switch (functionName.ToLowerInvariant())
            {
                case "count":
                    o = Left as IEnumerable<object>;
                    if (o != null)
                        retval = o.Count();
                    else
                        retval = -1;
                    break;
                case "range":
                    o = Left as IEnumerable<object>;
                    if (o != null)
                    {
                        // by default return the first
                        int iFrom = GetIndex(thread, Arguments[0], o, 0);
                        int iTo = GetIndex(thread, Arguments[1], o, iFrom);

                        if (iFrom < 0)
                            iFrom = 0;
                        if (iTo >= o.Count())
                            iTo = o.Count();

                        List<object> tretval = new List<object>();
                        for (int i = iFrom; i <= iTo; i++)
                        {
                            tretval.Add(o.ElementAt(i));
                        }
                        retval = tretval;
                    }
                    else
                        retval = null;
                    break;
                case "where":
                    
                    o = Left as IEnumerable<object>;
                    if (o != null)
                    {

                        List<object> tretvalWhere = new List<object>();
                        foreach (var item in o)
                        {
                            // todo: bonghi: needs to check the context is set here.
                            var scpInfo = new ScopeInfo(this, false);
                            object[] oIfc = new object[] { item };
                            thread.PushScope(scpInfo, oIfc);
                            object oCondition = Arguments[0].Evaluate(thread);
                            try
                            {
                                if ((bool)oCondition)
                                {
                                    tretvalWhere.Add(item);
                                }
                            }
                            catch (Exception)
                            {
                                
                                throw;
                            }
                            thread.PopScope();
                        }
                            
                        
                        retval = tretvalWhere;
                    }
                    else
                        retval = null;
                    break;
                default:
                    throw new Exception("Not implemented: " + functionName);
            }

            thread.CurrentNode = Parent; //standard epilog
            return retval;
        }

        private int GetIndex(Irony.Interpreter.ScriptThread thread, AstNode astNode, IEnumerable<object> o, int defaultValue)
        {
            object ret = astNode.Evaluate(thread);
            int retIndex = 0;
            try
            {
                retIndex = Convert.ToInt32(ret);
                if (retIndex < 0)
                    retIndex = o.Count() + retIndex; // -1 becomes the last; -2 next to last...
            }
            catch (Exception)
            {
                retIndex = defaultValue;
            }
            return retIndex;
        }

        // this is where data is initially passed to the item
        public override void Init(AstContext context, ParseTreeNode treeNode)
        {
            base.Init(context, treeNode);
            var nodes = treeNode.GetMappedChildNodes();

            // gets the argument list
            if (nodes.Count > 1)
            {
                Arguments = new AstNode[nodes[1].ChildNodes.Count];
                for (int i = 0; i < nodes[1].ChildNodes.Count; i++)
                {
                    var nd = AddChild("Arg" + i.ToString(), nodes[1].ChildNodes[i]);
                    Arguments[i] = nd;
                }
            }
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
