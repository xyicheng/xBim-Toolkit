%{
	
%}
%namespace Xbim.Query
%partial   
%parsertype Parser
%output=Parser.cs
%visibility internal
%using Xbim.XbimExtensions.Interfaces
%using System.Linq.Expressions


%start expressions

%union{
		public string strVal;
		public int intVal;
		public double doubleVal;
		public bool boolVal;
		public Type typeVal;
		public object val;
	  }


%token	INTEGER	
%token	DOUBLE	
%token	STRING	
%token	BOOLEAN		
%token	NONDEF			/*not defined, null*/
%token	DEFINED			/*not null*/
%token	IDENTIFIER	
%token  OP_EQ			/*is equal, equals, is, =*/
%token  OP_NEQ			/*is not equal, is not, !=*/
%token  OP_GT			/*is greater than, >*/
%token  OP_LT			/*is lower than, <*/
%token  OP_GTE			/*is greater than or equal, >=*/
%token  OP_LTQ			/*is lower than or equal, <=*/
%token  OP_CONTAINS		/*contains, is like, */
%token  OP_NOT_CONTAINS	/*doesn't contain*/
%token  OP_AND
%token  OP_OR
%token  PRODUCT
%token  PRODUCT_TYPE
%token  FILE
%token  MODEL

/*operations and keywords*/
%token  WHERE
%token  WITH_NAME /*with name, called*/
%token  DESCRIPTION /*and description, described as*/
%token  NEW /*is new*/
%token  ADD
%token  TO
%token  REMOVE
%token  FROM
%token  FOR
%token  NAME /*name*/
%token  PREDEFINED_TYPE
%token  TYPE
%token  MATERIAL

/* commands */
%token  SELECT
%token  SET
%token  CREATE
%token  DUMP
%token  CLEAR
%token  OPEN
%token  CLOSE
%token  SAVE


%%
expressions
	: expressions expression
	| expression
	;

expression
	: selection ';'
	| creation ';'
	| addition ';'
	| attr_setting ';'
	| variables_actions ';'
	| model_actions ';'
	| error
	;

attr_setting
	: SET value_setting_list FOR IDENTIFIER				{EvaluateSetExpression($4.strVal, ((List<Expression>)($2.val)));}
	;

value_setting_list
	: value_setting_list ',' value_setting				{((List<Expression>)($1.val)).Add((Expression)($3.val)); $$.val = $1.val;}
	| value_setting										{$$.val = new List<Expression>(){((Expression)($1.val))};}
	;

value_setting
	: attribute TO value		{$$.val = GenerateSetExpression($1.strVal, $3.val);}
	| STRING TO value			{$$.val = GenerateSetExpression($1.strVal, $3.val);}
	;	

value
	: STRING							{$$.val = $1.strVal;}
	| BOOLEAN							{$$.val = $1.boolVal;}
	| INTEGER							{$$.val = $1.intVal;}
	| DOUBLE								{$$.val = $1.doubleVal;}
	| NONDEF							{$$.val = null;}
	;

model_actions
	: OPEN MODEL FROM FILE STRING									{OpenModel($5.strVal);}
	| CLOSE MODEL													{CloseModel();}
	| SAVE MODEL TO FILE STRING										{SaveModel($5.strVal);}
	;

variables_actions
	: DUMP IDENTIFIER												{DumpIdentifier($2.strVal);}
	| CLEAR IDENTIFIER												{ClearIdentifier($2.strVal);}
	| DUMP string_list FROM IDENTIFIER								{DumpAttributes($4.strVal, ((List<string>)($2.val)));}
	| DUMP string_list FROM IDENTIFIER TO FILE STRING				{DumpAttributes($4.strVal, ((List<string>)($2.val)), $7.strVal);}
	;

string_list
	: string_list ',' STRING										{((List<string>)($1.val)).Add($3.strVal); $$.val = $1.val;}
	| string_list ',' attribute										{((List<string>)($1.val)).Add($3.strVal); $$.val = $1.val;}
	| STRING														{$$.val = new List<string>(){$1.strVal};}
	| attribute														{$$.val = new List<string>(){$1.strVal};}
	;

selection
	: SELECT selection_statement									{Variables.Set("$$", ((IEnumerable<IPersistIfcEntity>)($2.val)));}
	| IDENTIFIER op_bool selection_statement						{AddOrRemoveFromSelection($1.strVal, ((Tokens)($2.val)), $3.val);}
	;

selection_statement
	: object														{$$.val = Select($1.typeVal);}
	| object STRING													{$$.val = Select($1.typeVal, $2.strVal);}
	| object WHERE conditions										{$$.val = Select($1.typeVal, ((Expression)($3.val)));}
	;
	
creation
	: CREATE creation_statement										{Variables.Set("$$", ((IPersistIfcEntity)($2.val)));}
	| IDENTIFIER OP_EQ creation_statement							{Variables.Set($1.strVal, ((IPersistIfcEntity)($3.val)));}
	;

creation_statement
	: NEW object STRING												{$$.val = CreateObject($2.typeVal, $3.strVal);}			
	| NEW object WITH_NAME STRING 									{$$.val = CreateObject($2.typeVal, $4.strVal);}			
	| NEW object WITH_NAME STRING OP_AND DESCRIPTION STRING			{$$.val = CreateObject($2.typeVal, $4.strVal, $7.strVal);}			
	;

addition
	: ADD IDENTIFIER TO IDENTIFIER									{AddOrRemoveToGroupOrType(Tokens.ADD, $2.strVal, $4.strVal);}
	| REMOVE IDENTIFIER FROM IDENTIFIER								{AddOrRemoveToGroupOrType(Tokens.REMOVE, $2.strVal, $4.strVal);}
	;

conditions
	: conditions OP_AND condition			{$$.val = Expression.AndAlso(((Expression)($1.val)), ((Expression)($3.val)));}
	| conditions OP_OR condition			{$$.val = Expression.OrElse(((Expression)($1.val)), ((Expression)($3.val)));}
	| condition								{$$.val = $1.val;}
	;
	
condition
	: 
	| attributeCondidion					{$$.val = $1.val;}
	| materialCondition						{$$.val = $1.val;}
	| typeCondition							{$$.val = $1.val;}
	| propertyCondition						{$$.val = $1.val;}
	;

attributeCondidion	
	: attribute op_bool STRING				{$$.val = GenerateAttributeCondition($1.strVal, $3.strVal, ((Tokens)($2.val)));}
	| attribute op_bool NONDEF				{$$.val = GenerateAttributeCondition($1.strVal, null, ((Tokens)($2.val)));}
	| attribute op_cont STRING				{$$.val = GenerateAttributeCondition($1.strVal, $3.strVal, ((Tokens)($2.val)));}
	;

attribute
	: NAME						{$$.strVal = "Name";}
	| DESCRIPTION				{$$.strVal = "Description";}
	| PREDEFINED_TYPE			{$$.strVal = "PredefinedType";}
	;	
	
materialCondition	
	: MATERIAL op_bool STRING			{$$.val = GenerateMaterialCondition($3.strVal, ((Tokens)($2.val)));}
	| MATERIAL op_cont STRING			{$$.val = GenerateMaterialCondition($3.strVal, ((Tokens)($2.val)));}
	;
	
typeCondition	
	: TYPE op_bool PRODUCT_TYPE			{$$.val = GenerateTypeObjectTypeCondition($3.typeVal, ((Tokens)($2.val)));}
	| TYPE op_bool STRING				{$$.val = GenerateTypeObjectNameCondition($3.strVal, ((Tokens)($2.val)));}
	;

propertyCondition	
	: STRING op_bool    INTEGER			{$$.val = GeneratePropertyCondition($1.strVal, $3.intVal, ((Tokens)($2.val)));}
	| STRING op_num_rel INTEGER			{$$.val = GeneratePropertyCondition($1.strVal, $3.intVal, ((Tokens)($2.val)));}
	
	| STRING  op_bool    DOUBLE			{$$.val = GeneratePropertyCondition($1.strVal, $3.doubleVal, ((Tokens)($2.val)));}
	| STRING op_num_rel DOUBLE			{$$.val = GeneratePropertyCondition($1.strVal, $3.doubleVal, ((Tokens)($2.val)));}
	
	| STRING op_bool STRING				{$$.val = GeneratePropertyCondition($1.strVal, $3.strVal, ((Tokens)($2.val)));}
	| STRING op_cont STRING				{$$.val = GeneratePropertyCondition($1.strVal, $3.strVal, ((Tokens)($2.val)));}
	
	| STRING op_bool BOOLEAN			{$$.val = GeneratePropertyCondition($1.strVal, $3.boolVal, ((Tokens)($2.val)));}
    | STRING op_bool NONDEF				{$$.val = GeneratePropertyCondition($1.strVal, null, ((Tokens)($2.val)));}
    | STRING OP_NEQ DEFINED				{$$.val = GeneratePropertyCondition($1.strVal, null, Tokens.OP_EQ);}
    | STRING OP_EQ DEFINED				{$$.val = GeneratePropertyCondition($1.strVal, null, Tokens.OP_NEQ);}
	;

op_bool
	: OP_EQ			{$$.val = Tokens.OP_EQ;}
	| OP_NEQ		{$$.val = Tokens.OP_NEQ;}
	;
	
op_num_rel
	: OP_GT			{$$.val = Tokens.OP_GT;}
    | OP_LT			{$$.val = Tokens.OP_LT;}
    | OP_GTE		{$$.val = Tokens.OP_GTE;}
    | OP_LTQ		{$$.val = Tokens.OP_LTQ;}
	;
	
op_cont
	: OP_CONTAINS		{$$.val = Tokens.OP_CONTAINS;}
	| OP_NOT_CONTAINS	{$$.val = Tokens.OP_NOT_CONTAINS;}
	;

object
	: PRODUCT				{$$.typeVal = $1.typeVal;}
	| PRODUCT_TYPE			{$$.typeVal = $1.typeVal;}
	| MATERIAL				{$$.typeVal = $1.typeVal;}
	;
	
%%