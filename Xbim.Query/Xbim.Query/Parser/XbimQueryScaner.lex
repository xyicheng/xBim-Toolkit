%namespace Xbim.Query

%option verbose, summary, caseinsensitive, noPersistBuffer, out:XbimQueryScanner.cs

%{
	public static int Pass = 1;
	public static bool emitPass = true;
	public static bool comment = false;
	public void SetValue()
	{
		if (!comment) {
		yylval.strVal=yytext;
		
		}
	}
%}

%%

%{
		
%}
/* ************  skip white chars  ************** */
"\t"	     {}
" "		     {}
[\n]		 {} 
[\r]         {} 
[\0]+		 {} 
\/\/[^\r\n]* {}   /*One line comment*/

/* ******************** values ****************** */
[\-\+0-9][0-9]*	    { if (!comment) {SetValue();  return((int)Tokens.INTEGER); } }
[\-\+\.0-9][\.0-9]+	{ if (!comment) {SetValue(); return((int)Tokens.FLOAT); } }
[\-\+\.0-9][\.0-9]+E[\-\+0-9][0-9]* {if (!comment) { SetValue(); return((int)Tokens.FLOAT); } }
[\']([\n]|[\000\011-\046\050-\176\201-\237\240-\377]|[\047][\047])*[\']	{ if (!comment) { SetValue();  return((int)Tokens.STRING); } }

[\.][TF][\.]	    {if (!comment) {SetValue(); return((int)Tokens.BOOLEAN); } }

[$]		            {if (!comment) {return((int)Tokens.NONDEF); } }
[(]		{ if (!comment) return ('('); }
[)]		{ if (!comment) return (')'); }
[,]		{ if (!comment) return (','); }
[=]		{ if (!comment) return ('='); }
[;]		{ if (!comment) return (';'); }
"/*"		{ comment=true;  }
"*/"		{ comment=false;  }



%%

