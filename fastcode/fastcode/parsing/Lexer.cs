﻿using System;
using fastcode.runtime;

namespace fastcode.parsing
{
    public class Lexer
    {
        public const char EOF = (char)0;

        private readonly string source;
        private char lastChar; //last character read from the lexer stream

        public Marker Position { get; private set; }
        public string TokenIdentifier { get;  set; }
        public Value TokenValue { get;  set; }

        public Lexer(string source)
        {
            this.source = source;
            Position = new Marker(0, 0, 0);
            if (source.Length > 0)
            {
                lastChar = source[0];
            }
            else
            {
                lastChar = EOF;
            }
        }

        //shifts the cursor position
        public void ShiftCurrentPosition(Marker marker)
        {
            Position = new Marker(marker.Index,marker.Collumn,marker.Row);
            if (marker.Index < source.Length)
            {
                lastChar = source[marker.Index];
            }
            else
            {
                lastChar = EOF;
            }
        }
        
        //peeking to examine the next character (doesn't remove from stream)
        public char Peek()
        {
            if(Position.Index + 1 >= source.Length)
            {
                return EOF;
            }
            return source[Position.Index + 1];
        }

        //like a stream, reads one char
        public char ReadChar()
        {
            //increment cursor position
            Position.Index++;
            Position.Collumn++;

            //check if you've read till the end of the code.
            if(Position.Index >= source.Length)
            {
                lastChar = EOF;
                return lastChar;
            }
          
            lastChar = source[Position.Index]; //sets last character
          
            //checks for newlines
            if(lastChar == '\n')
            {
                //update row and col data
                Position.Collumn = 0;
                Position.Row++;
            }
            return lastChar;
        }

        //tokenization stuff
        public Token ReadNextToken()
        {
            //trim the front of the buffer
            while(lastChar == ' ' || lastChar == '\t' || lastChar == '\r')
            {
                ReadChar(); //takes a character out of the buffer
            }

            //check if the token is an identifier or keyword by verifying whether the first character is a letter
            if(char.IsLetter(lastChar))
            {
                TokenIdentifier = string.Empty;
                TokenIdentifier += lastChar;
                while(char.IsLetterOrDigit(ReadChar()) || lastChar == '_')
                {
                    TokenIdentifier += lastChar;
                }

                switch(TokenIdentifier)
                {
                    case "and":
                        return Token.And;
                    case "or":
                        return Token.Or;
                    case "in":
                        return Token.In;
                    case "if":
                        return Token.If;
                    case "else":
                        return Token.Else;
                    case "elif":
                        return Token.Elif;
                    case "for":
                        return Token.For;
                    case "while":
                        return Token.While;
                    case "function":
                        return Token.Function;
                    case "break":
                        return Token.Break;
                    case "return":
                        return Token.Return;
                    case "stop":
                        return Token.Stop;
                    case "import":
                        return Token.Import;
                    case "global":
                        return Token.Global;
                    case "abstract":
                        return Token.Abstract;
                    case "ref":
                        return Token.Refrence;
                    case "refrence":
                        return Token.Refrence;
                    case "rem":
                        while(lastChar != '\n')
                        {
                            ReadChar();
                        }
                        ReadChar();
                        return ReadNextToken();
                    default:
                        return Token.Identifier;
                }
            }
            else if(char.IsDigit(lastChar))
            {
                string numstr = string.Empty;
                numstr += lastChar;

                while(char.IsDigit(ReadChar()) || lastChar == '.')
                {
                    numstr += lastChar;
                }
                TokenValue = new Value(double.Parse(numstr));
                return Token.Value;
            }
            else
            {
                Token token = Token.Unkown; //we'll identify it in the following
                switch (lastChar)
                {
                    case '\n':
                        token = Token.Newline;
                        break;
                    case ';':
                        token = Token.Semicolon;
                        break;
                    case ',':
                        token = Token.Comma;
                        break;
                    case '(':
                        token = Token.OpenParenthesis;
                        break;
                    case ')':
                        token = Token.CloseParenthesis;
                        break;
                    case '{':
                        token = Token.OpenBrace;
                        break;
                    case '}':
                        token = Token.CloseBrace;
                        break;
                    case '[':
                        token = Token.OpenBracket;
                        break;
                    case ']':
                        token = Token.CloseBracket;
                        break;
                    case '+':
                        token = Token.Plus;
                        break;
                    case '-':
                        token = Token.Minus;
                        break;
                    case '*':
                        token = Token.Asterisk;
                        break;
                    case '/':
                        if(Peek() == '/')
                        {
                            while(lastChar != '\n')
                            {
                                ReadChar();
                            }
                            ReadChar();
                            return ReadNextToken(); 
                        }
                        token = Token.Slash;
                        break;
                    case '^':
                        token = Token.Caret;
                        break;
                    case '%':
                        token = Token.Modulous;
                        break;
                    case '&':
                        if(Peek() == '&')
                        {
                            ReadChar();
                            token = Token.And;
                            break;
                        }
                        token = Token.Refrence;
                        break;
                    case '|':
                        if (Peek() == '|')
                        {
                            ReadChar();
                            token = Token.Or;
                        }
                        break;
                    case '=':
                        if(Peek() == '=')
                        {
                            ReadChar();
                            token = Token.Equals;
                            break;
                        }
                        token = Token.Set;
                        break;
                    case '!':
                        if(Peek() == '=')
                        {
                            ReadChar();
                            token = Token.NotEqual;
                            break;
                        }
                        token = Token.Not;
                        break;
                    case '>':
                        if(Peek() == '=')
                        {
                            ReadChar();
                            token = Token.MoreEqual;
                            break;
                        }
                        token = Token.More;
                        break;
                    case '<':
                        if (Peek() == '=')
                        {
                            ReadChar();
                            token = Token.LessEqual;
                            break;
                        }
                        token = Token.Less;
                        break;
                    case '"':
                        string str = string.Empty;
                        while(ReadChar() != '"')
                        {
                            if(lastChar == '\\')
                            {
                                switch (Peek())
                                {
                                    case 'n':
                                        str += '\n';
                                        ReadChar();
                                        break;
                                    case 'r':
                                        str += '\r';
                                        ReadChar();
                                        break;
                                    case 't':
                                        str += '\t';
                                        ReadChar();
                                        break;
                                    case '\\':
                                        str += '\\';
                                        ReadChar();
                                        break;
                                    case '"':
                                        str += '"';
                                        ReadChar();
                                        break;
                                }
                            }
                            else if(lastChar == EOF)
                            {
                                throw new Exception("Expected \" at the end of the string value.");
                            }
                            else
                            {
                                str += lastChar;
                            }
                        }
                        TokenValue = new Value(str);
                        token = Token.Value;
                        break;
                    case '\'':
                        TokenValue = new Value(ReadChar());
                        token = Token.Value;
                        ReadChar();
                        break;
                    case EOF:
                        return Token.EndOfFile;
                }

                ReadChar();    //empty lastchar
                
                //handle uniedentified token
                if(token == Token.Unkown)
                {
                    throw new UnidentifiedTokenExcepion();
                }
                return token;
            }
        }
    }
}