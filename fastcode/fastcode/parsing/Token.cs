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
        Import,
        In,

        If,  //control structures
        Else,
        Elif, //it's a good keyword
        For,
        While,

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
