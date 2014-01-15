#region License
/* **********************************************************************************
 * Copyright (c) Roman Ivantsov
 * This source code is subject to terms and conditions of the MIT License
 * for Irony. A copy of the license can be found in the License.txt file
 * at the root of this distribution. 
 * By using this source code in any fashion, you are agreeing to be bound by the terms of the 
 * MIT License.
 * You must not remove this notice from this software.
 * **********************************************************************************/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Irony.Parsing;
using Irony.Interpreter;
using Irony.Interpreter.Ast;
using Irony.Interpreter.Evaluator;
using Xbim.IO;
using Xbim.Querying.Nodes;

namespace Xbim.Querying
{
    [Language("xBimQL", "0.11", "xBimQueryLanguage with expression evaluator")]
    public class xBimQueryLanguage : InterpretedLanguageGrammar // Grammar // InterpretedLanguageGrammar // 
    {
        XbimModel _model;

        public xBimQueryLanguage()
            : base(caseSensitive: false)
        {
            _model = new XbimModel();
#if debug
            _model.Open(@"F:\dev\Codeplex\xBIM\XbimFramework\Head\Xbim.Querying\Querying\Test\Duplex-Handover.xBIM");
#endif
            InitGrammar();
        }

        public xBimQueryLanguage(XbimModel model)
            : base(caseSensitive: false)
        {
            this._model = model;
            InitGrammar();
        }

        private void InitGrammar()
        {
            this.GrammarComments = @"One first go at it.";

            // 1. Non-terminals
            var Expr = new NonTerminal("Expr"); //declare it here to use in template definition 
            var Term = new NonTerminal("Term");
            var BinExpr = new NonTerminal("BinExpr", typeof(BinaryOperationNode));
            var ParExpr = new NonTerminal("ParExpr");
            var UnExpr = new NonTerminal("UnExpr", typeof(UnaryOperationNode));
            var TernaryIfExpr = new NonTerminal("TernaryIf", typeof(IfNode));
            var ArgList = new NonTerminal("ArgList", typeof(ExpressionListNode));
            var FunctionCall = new NonTerminal("FunctionCall", typeof(FunctionCallNode));
            var MemberAccess = new NonTerminal("MemberAccess", typeof(MemberAccessNode));
            // var IndexedAccess = new NonTerminal("IndexedAccess", typeof(IndexedAccessNode));
            // var ObjectRef = new NonTerminal("ObjectRef"); // foo, foo.bar or f['bar']
            var UnOp = new NonTerminal("UnOp");
            var BinOp = new NonTerminal("BinOp", "operator");
            var PrefixIncDec = new NonTerminal("PrefixIncDec", typeof(IncDecNode));
            var PostfixIncDec = new NonTerminal("PostfixIncDec", typeof(IncDecNode));
            var IncDecOp = new NonTerminal("IncDecOp");
            var AssignmentStmt = new NonTerminal("AssignmentStmt", typeof(AssignmentNode));
            var AssignmentOp = new NonTerminal("AssignmentOp", "assignment operator");

            // selects
            var SelectStatement = new NonTerminal("SelectStatement", typeof(SelectStatement));
            var SelectRoot = new NonTerminal("SelectRoot", typeof(SelectRoot));
            var SelectRootModel = new NonTerminal("SelectRootModel", typeof(EmptyStatementNode));
            var SelectRootElementList = new NonTerminal("SelectRootElementList", typeof(EmptyStatementNode));
            var SelectRootElement = new NonTerminal("SelectRootElement", typeof(EmptyStatementNode));

            var SelectExpression = new NonTerminal("SelectExpression", typeof(SelectExpressionNode));
            var SelectFunction = new NonTerminal("SelectFunction", typeof(SelectFunctionNode));
            var SelectMemberAccess = new NonTerminal("SelectMemberAccess", typeof(SelectMemberAccessNode));
            var SelectProperty = new NonTerminal("SelectProperty", typeof(SelectPropertyNode));
            var SelectFunctionName = new NonTerminal("SelectFunctionName", typeof(EmptyStatementNode));
            var SelectTerm = new NonTerminal("SelectItem", typeof(EmptyStatementNode));
            var NumberOrEmpty = new NonTerminal("NumberOrEmpty", typeof(EmptyStatementNode));
            var IfcClassProperty = new NonTerminal("IfcClassProperty", typeof(IfcClassPropertyNode));


            // core
            var Statement = new NonTerminal("Statement");
            var Program = new NonTerminal("Program", typeof(StatementListNode));

            // 2. Terminals
            var number = new NumberLiteral("number");
            number.DefaultIntTypes = new TypeCode[] { TypeCode.Int32, TypeCode.Int64 }; //Let's allow big integers (with unlimited number of digits):
            var identifier = new IdentifierTerminal("identifier");
            var comma = ToTerm(",");
            var PositiveIntegerNumber = new NumberLiteral("IntegerNumber", NumberOptions.IntOnly);
            var stringLit = new StringLiteral("string", "\"", StringOptions.AllowsAllEscapes);
            var ElementIdStart = ToTerm("@");
            stringLit.AddStartEnd("'", StringOptions.AllowsAllEscapes);

            /*
             * for embedded expressions:
            var templateSettings = new StringTemplateSettings(); //by default set to Ruby-style settings 
            templateSettings.ExpressionRoot = Expr; //this defines how to evaluate expressions inside template
            this.SnippetRoots.Add(Expr);
            stringLit.AstConfig.Data = templateSettings;
             */

            // 3. BNF rules
            Expr.Rule = Term | UnExpr | BinExpr | PrefixIncDec | PostfixIncDec | TernaryIfExpr;
            Term.Rule = number | ParExpr | stringLit | FunctionCall | identifier | IfcClassProperty | MemberAccess; // | IndexedAccess; // | MemberAccess;
            ParExpr.Rule = "(" + Expr | SelectStatement + ")";
            UnExpr.Rule = UnOp + Term + ReduceHere();
            UnOp.Rule = ToTerm("+") | "-" | "!";
            BinExpr.Rule = Expr + BinOp + Expr;
            BinOp.Rule = ToTerm("+") | "-" | "*" | "/" | "**" | "==" | "<" | "<=" | ">" | ">=" | "!=" | "&&" | "||" | "&" | "|";
            PrefixIncDec.Rule = IncDecOp + identifier;
            PostfixIncDec.Rule = identifier + PreferShiftHere() + IncDecOp;
            IncDecOp.Rule = ToTerm("++") | "--";
            TernaryIfExpr.Rule = Expr + "?" + Expr + ":" + Expr;
            MemberAccess.Rule = Expr + PreferShiftHere() + "." + identifier;
            IfcClassProperty.Rule = ElementIdStart + identifier;

            AssignmentStmt.Rule = identifier + AssignmentOp + Expr;
            AssignmentOp.Rule = ToTerm("=") | "+=" | "-=" | "*=" | "/=";
            Statement.Rule = AssignmentStmt | Expr | SelectStatement | Empty;
            ArgList.Rule = MakeStarRule(ArgList, comma, Expr);
            FunctionCall.Rule = Expr + PreferShiftHere() + "(" + ArgList + ")";
            FunctionCall.NodeCaptionTemplate = "call #{0}(...)";

            // select
            var dot = ToTerm("."); dot.SetFlag(TermFlags.IsTransient);
            var select = ToTerm("select"); select.SetFlag(TermFlags.IsTransient);

            SelectStatement.Rule = select + SelectRoot + SelectExpression;

            var ModelAt = ToTerm("@"); ModelAt.SetFlag(TermFlags.IsTransient);
            SelectRoot.Rule = SelectRootModel +  ModelAt + SelectRootElementList;
            SelectRootModel.Rule = identifier | Empty;
            SelectRootElementList.Rule = MakeStarRule(SelectRootElementList, comma, SelectRootElement); // can be empty
            SelectRootElement.Rule = identifier | PositiveIntegerNumber | ToTerm("*");
            

            SelectExpression.Rule = Empty | SelectMemberAccess;
            SelectMemberAccess.Rule = dot + PreferShiftHere() + SelectTerm + SelectExpression;
            SelectTerm.Rule = SelectFunction | SelectProperty;
            SelectProperty.Rule = identifier;
            SelectFunction.Rule = SelectFunctionName + PreferShiftHere() + ToTerm("(") + ArgList + ")";
            SelectFunctionName.Rule = ToTerm("Where") | "Range" | "Count";


            Program.Rule = MakePlusRule(Program, NewLine, Statement);

            this.Root = Program;       // Set grammar root

            // 4. Operators precedence
            RegisterOperators(10, "?");
            RegisterOperators(15, "&", "&&", "|", "||");
            RegisterOperators(20, "==", "<", "<=", ">", ">=", "!=");
            RegisterOperators(30, "+", "-");
            RegisterOperators(40, "*", "/");
            RegisterOperators(50, Associativity.Right, "**");
            RegisterOperators(60, "!");
            // For precedence to work, we need to take care of one more thing: BinOp. 
            //For BinOp which is or-combination of binary operators, we need to either 
            // 1) mark it transient or 2) set flag TermFlags.InheritPrecedence
            // We use first option, making it Transient.  

            // 5. Punctuation and transient terms
            MarkPunctuation("(", ")", "?", ":"); //, "[", "]");
            RegisterBracePair("(", ")");
            // RegisterBracePair("[", "]");
            MarkTransient(Term, Expr, Statement, BinOp, UnOp, IncDecOp, AssignmentOp, ParExpr); //, ObjectRef);
            MarkTransient(NumberOrEmpty, SelectExpression);
            MarkTransient(SelectTerm);

            // 7. Syntax error reporting
            MarkNotReported("++", "--");
            AddToNoReportGroup("(", "++", "--");
            AddToNoReportGroup(NewLine);
            AddOperatorReportGroup("operator");
            AddTermsReportGroup("assignment operator", "=", "+=", "-=", "*=", "/=");

            //8. Console
            ConsoleTitle = "mah";
            ConsoleGreeting = @"Irony Expression Evaluator";
            ConsolePrompt = "?";
            ConsolePromptMoreInput = "?";

            //9. Language flags. 
            // Automatically add NewLine before EOF so that our BNF rules work correctly when there's no final line break in source
            this.LanguageFlags = LanguageFlags.NewLineBeforeEOF | LanguageFlags.SupportsBigInt | LanguageFlags.CreateAst;
        }

        //public override LanguageRuntime CreateRuntime(LanguageData language)
        //{
        //    return new ExpressionEvaluatorRuntime(language);
        //}

        public object Run(RunSampleArgs args)
        {
            if (_evaluator == null)
            {
                _evaluator = new ExpressionEvaluator(this);
                _evaluator.Globals.Add("null", _evaluator.Runtime.NoneValue);
                _evaluator.Globals.Add("true", true);
                _evaluator.Globals.Add("false", false);
                _evaluator.Globals.Add("model", _model);
            }
            _evaluator.ClearOutput();
            //for (int i = 0; i < 1000; i++)  //for perf measurements, to execute 1000 times
            return _evaluator.Evaluate(args.ParsedSample);
        }


        #region Running in Grammar Explorer
        private static ExpressionEvaluator _evaluator;
        public override string RunSample(RunSampleArgs args)
        {
            Run(args);
            return _evaluator.GetOutput();
        }
        #endregion
    }//class
}