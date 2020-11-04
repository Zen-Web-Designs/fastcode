using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fastcode.parsing
{
    public enum Token
    {
        Unkown, //The token has yet to be identified. The function will not return an Unkown Token, but throw an exception instread.
        
        Identifier,  //stuff for refrences
        Value,
        Function,

        Break,
        Return,
        Stop,
        Assert,
        Import,
        In,

        If,  //control structures
        Else,
        Elif, //it's a good keyword
        For,
        While,
        Forever, //faster than while(true) because it doesn't have to evaluate true. Could save alot of resources 'specially if you loop alot.

        Newline,    //control tokens
        Semicolon,
        Comma,
        OpenParenthesis, //for arguments
        CloseParenthesis,
        OpenBrace, //for control structures
        CloseBrace,
        OpenBracket, //for arrays
        CloseBracket,

        Plus,   //operator related keywords 
        Minus,
        Slash,
        Asterisk,
        Modulous,
        Caret,
        Set,
        Equals,
        Less,
        More,
        NotEqual,
        LessEqual,
        MoreEqual,
        Or,
        And,
        Not,

        EndOfFile //end of stream marker
    }
}
