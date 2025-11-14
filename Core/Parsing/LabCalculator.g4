grammar LabCalculator;

compileUnit : expression EOF;

expression
    : LPAREN expression RPAREN                                  # ParenthesizedExpr
    | <assoc=right> expression op=EXPONENT expression           # ExponentialExpr
    | op=(ADD | SUBTRACT) expression                            # UnaryExpr
    | op=(INC | DEC) LPAREN expression RPAREN                   # IncDecExpr
    | expression op=(MULTIPLY | DIVIDE) expression              # MultiplicativeExpr
    | expression op=(ADD | SUBTRACT) expression                 # AdditiveExpr
    | op=(MOD | DIV) LPAREN expression ',' expression RPAREN    # ModDivExpr
    | MMIN LPAREN paramlist RPAREN                              # MMinExpr
    | MMAX LPAREN paramlist RPAREN                              # MMaxExpr
    | NUMBER                                                    # NumberExpr
    | IDENTIFIER                                                # IdentifierExpr
    ;

paramlist
    : expression (',' expression)*
    ;

NUMBER      : INT ('.' INT)? | '.' INT ;
IDENTIFIER  : [a-zA-Z] [a-zA-Z0-9]* ;
LPAREN      : '(' ;
RPAREN      : ')' ;
COMMA       : ',' ;
EXPONENT    : '^' ;
MULTIPLY    : '*' ;
DIVIDE      : '/' ;
ADD         : '+' ;
SUBTRACT    : '-' ;
MOD         : 'mod' | 'MOD' ;   
DIV         : 'div' | 'DIV' ;   
MMAX        : 'mmax' | 'MMAX' ; 
MMIN        : 'mmin' | 'MMIN' ; 
INC         : 'inc' | 'INC' ;   
DEC         : 'dec' | 'DEC' ;   
fragment INT : [0-9]+ ; 
WS          : [ \t\r\n]+ -> channel(HIDDEN) ;