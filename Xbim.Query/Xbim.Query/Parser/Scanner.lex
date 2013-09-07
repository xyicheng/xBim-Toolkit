%namespace Xbim.Query

%option verbose, summary, caseinsensitive, noPersistBuffer, out:Scanner.cs
%visibility internal

%{
	//all the user code is in XbimQueryScanerHelper

%}

%%

%{
		
%}
/* ************  skip white chars and line comments ************** */
"\t"	     {}
" "		     {}
[\n]		 {} 
[\r]         {} 
[\0]+		 {} 
\/\/[^\r\n]* {}   /*One line comment*/



/* ********************** Identifiers ************************** */
"$"[a-z][a-z0-9_]*		            { return (int)SetValue(Tokens.IDENTIFIER); }

/* ********************** Operators ************************** */
"="		{  return ((int)Tokens.OP_EQ); }
"equals"		{  return ((int)Tokens.OP_EQ); }
"is"		{  return ((int)Tokens.OP_EQ); }
"is equal to"		{  return ((int)Tokens.OP_EQ); }

"!="		{ return ((int)Tokens.OP_NEQ); }
"is not equal to"		{ return ((int)Tokens.OP_NEQ); }
"is not"		{ return ((int)Tokens.OP_NEQ); }
"does not equal"		{ return ((int)Tokens.OP_NEQ); }
"doesn't equal"		{ return ((int)Tokens.OP_NEQ); }

">"		{  return ((int)Tokens.OP_GT); }
"is greater than"		{  return ((int)Tokens.OP_GT); }

"<"		{  return ((int)Tokens.OP_LT); }
"is lower than"		{  return ((int)Tokens.OP_LT); }

">="		{  return ((int)Tokens.OP_GTE); }
"is greater than or equal to"		{  return ((int)Tokens.OP_GTE); }

"<="		{  return ((int)Tokens.OP_LTQ); }
"is lower than or equal to"		{  return ((int)Tokens.OP_LTQ); }

"&&"		{  return ((int)Tokens.OP_AND); }
"and"		{  return ((int)Tokens.OP_AND); }

"||"		{  return ((int)Tokens.OP_OR); }
"or"		{  return ((int)Tokens.OP_OR); }

"~"					{return ((int)Tokens.OP_CONTAINS);}
"contains"			{return ((int)Tokens.OP_CONTAINS);}
"is like"			{return ((int)Tokens.OP_CONTAINS);}

"!~"			{return ((int)Tokens.OP_NOT_CONTAINS);}
"does not contain"			{return ((int)Tokens.OP_NOT_CONTAINS);}
"doesn't contain"			{return ((int)Tokens.OP_NOT_CONTAINS);}

";"		{  return (';'); }


/* ********************** Keywords ************************** */
"select"			{ return (int)Tokens.SELECT;}
"where"			{ return (int)Tokens.WHERE;}
"create"			{ return (int)Tokens.CREATE;}
"with name" |
"called"			{ return (int)Tokens.WITH_NAME; }
"description" |
"described as"			{ return (int)Tokens.DESCRIPTION ;} 
"new"			{ return (int)Tokens.NEW;}  /*is new*/								
"add"			{ return (int)Tokens.ADD;}
"to"			{ return (int)Tokens.TO; }
"remove"			{ return (int)Tokens.REMOVE; }
"from"			{ return (int)Tokens.FROM; }
"name"			{ return (int)Tokens.NAME; }									
"predefined type"			{ return (int)Tokens.PREDEFINED_TYPE; }
"type"			{ return (int)Tokens.TYPE; }
"material"			{ return (int)Tokens.MATERIAL; }

"null" |
"not defined" |
"unknown"			{return (int)Tokens.NONDEF;}

/* ********************     values        ****************** */
[\-\+]?[0-9]+	    {  return (int)SetValue(Tokens.INTEGER); }
[\-\+]?[0-9]*[\.][0-9]*	|
[\-\+\.0-9][\.0-9]+E[\-\+0-9][0-9]* { return (int)SetValue(Tokens.FLOAT); }
[\"]([\n]|[\000\011-\046\050-\176\201-\237\240-\377]|[\047][\047])*[\"]	{ return (int)SetValue(); }
[\']([\n]|[\000\011-\046\050-\176\201-\237\240-\377]|[\047][\047])*[\']	{ return (int)SetValue(); }
".T." |
".F." |
true |
false	    { return (int)SetValue(Tokens.BOOLEAN); }
[a-z]+[a-z_\-0-9]*	{ return (int)ProcessString(); }


/* -----------------------  Epilog ------------------- */
%{
	//yylloc = new LexLocation(tokLin,tokCol,tokELin,tokECol);
%}
/* --------------------------------------------------- */
%%


