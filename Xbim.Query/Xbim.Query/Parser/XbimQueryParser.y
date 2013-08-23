%{
 
%}
%namespace Xbim.Query
%partial   
%parsertype XbimQueryParser
%output=XbimQueryParser.cs

%start expression

%union{
		public string strVal;
	  }


%token	INTEGER	
%token	FLOAT	
%token	STRING	
%token	BOOLEAN		
%token	NONDEF			/*not defined, null*/
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

/*operations and keywords*/
%token  SELECT
%token  WHERE
%token  CREATE
%token  WITH_NAME /*with name, called*/
%token  DESCRIPTION /*and description, described as*/
%token  NEW /*is new*/
%token  ADD
%token  TO
%token  REMOVE
%token  FROM
%token  NAME /*name*/
%token  DESCRIPTION
%token  PREDEFINED_TYPE
%token  TYPE
%token  MATERIAL

%%
expression
	: expression selection ';'
	| expression creation ';'
	| expression addition ';'
	| /*EMPTY*/
	;

selection
	: SELECT PRODUCT 
	| SELECT PRODUCT WHERE conditions
	| IDENTIFIER OP_EQ PRODUCT WHERE conditions
	| IDENTIFIER OP_NEQ PRODUCT WHERE conditions
	;
	
creation
	: CREATE PRODUCT WITH_NAME STRING
	| CREATE PRODUCT WITH_NAME STRING DESCRIPTION STRING
	| IDENTIFIER NEW PRODUCT STRING
	;
	
addition
	: ADD IDENTIFIER TO IDENTIFIER
	| REMOVE IDENTIFIER FROM IDENTIFIER
	;

conditions
	: condition OP_AND condition
	| condition OP_OR condition
	| condition
	;
	
condition
	: 
	| attributeCondidion
	| materialCondition
	| typeCondition
	| propertyCondition
	;

attributeCondidion	
	: attribute op_bool STRING
	| attribute op_cont STRING
	;

attribute
	: NAME
	| DESCRIPTION
	| PREDEFINED_TYPE
	;	
	
materialCondition	
	: MATERIAL op_bool STRING
	| MATERIAL op_cont STRING
	;
	
typeCondition	
	: TYPE OP_EQ PRODUCT_TYPE
	| TYPE OP_NEQ PRODUCT_TYPE
	;

propertyCondition	
	: STRING op_bool    INTEGER
	| STRING op_num_rel INTEGER
	
	| STRING  op_bool    FLOAT	
	| STRING op_num_rel FLOAT	
	
	| STRING op_bool STRING	
	| STRING op_cont STRING	
	
	| STRING op_bool BOOLEAN	
    | STRING op_bool NONDEF	
	;

op_bool
	: OP_EQ
	| OP_NEQ
	;
	
op_num_rel
	: OP_GT
    | OP_LT
    | OP_GTE
    | OP_LTQ
	;
	
op_cont
	: OP_CONTAINS
	| OP_NOT_CONTAINS
	;
	
%%