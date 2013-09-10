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
"$"[a-z$][a-z0-9_]*		            { return (int)SetValue(Tokens.IDENTIFIER); }

/* ********************** Operators ************************** */
"="	|
"equals" |
"is" |
"is equal to"		{  return ((int)Tokens.OP_EQ); }

"!=" |
"is not equal to" |
"is not" |
"does not equal" |
"doesn't equal"		{ return ((int)Tokens.OP_NEQ); }

">" |
"is greater than"		{  return ((int)Tokens.OP_GT); }

"<"	|
"is less than"		{  return ((int)Tokens.OP_LT); }

">=" |
"is greater than or equal to"		{  return ((int)Tokens.OP_GTE); }

"<=" |
"is less than or equal to"		{  return ((int)Tokens.OP_LTQ); }

"&&" |
"and"		{  return ((int)Tokens.OP_AND); }

"||" |
"or"		{  return ((int)Tokens.OP_OR); }

"~"	|
"contains" |
"is like"			{return ((int)Tokens.OP_CONTAINS);}

"!~" |
"does not contain" |
"doesn't contain"			{return ((int)Tokens.OP_NOT_CONTAINS);}

";"		{  return (';'); }
","		{  return (','); }


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
"export" |
"dump"			{ return (int)Tokens.DUMP; }
"clear"			{ return (int)Tokens.CLEAR; }
"open"			{ return (int)Tokens.OPEN; }
"close"			{ return (int)Tokens.CLOSE; }
"save"			{ return (int)Tokens.SAVE; }

"name"			{ return (int)Tokens.NAME; }									
"predefined type"			{ return (int)Tokens.PREDEFINED_TYPE; }
"type"			{ return (int)Tokens.TYPE; }
"material"			{ return (int)SetValue(Tokens.MATERIAL); }
"file"			{ return (int)Tokens.FILE; }
"model"			{ return (int)Tokens.MODEL; }

"null" |
"undefined" |
"unknown"			{return (int)Tokens.NONDEF;}

"defined"			{return (int)Tokens.DEFINED;}

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


