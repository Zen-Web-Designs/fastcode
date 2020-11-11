using System;
using System.Collections.Generic;
using fastcode.parsing;

namespace fastcode.runtime
{
    public enum ValueType
    {
        Array,
        Double,
        String,
        Char,
        Null
    }

    public class Value
    {
        public static Value Null { get { return new Value(); } } //NULL will be defined as it's own type
        public static Value True { get { return new Value(1); } }
        public static Value False { get { return new Value(0); } }

        public ValueType Type { get; private set; }

        public string String { get; private set; }
        public double Double { get; private set; }
        public char Character { get; private set; }
        public List<Value> Array { get; set; } //adding arrays

        public override string ToString()
        {
            if(Type == ValueType.Double)
            {
                return Double.ToString();
            }
            else if(Type == ValueType.String)
            {
                return String;
            }
            else if(Type == ValueType.Char)
            {
                return Character.ToString();
            }
            else if(Type == ValueType.Array)
            {
                string ret = "[";
                for (int i = 0; i < Array.Count; i++)
                {
                    if(Array[i].Type == ValueType.Double|| Array[i].Type == ValueType.Double || Array[i].Type == ValueType.Array)
                    {
                        ret += Array[i].ToString();
                    }
                    else if(Array[i].Type == ValueType.String)
                    {
                        ret += "\"" + Array[i].ToString() + "\"";
                    }
                    else if(Array[i].Type == ValueType.Char)
                    {
                        ret += "\'" + Array[i].ToString() + "\'";
                    }
                    if(i != Array.Count-1)
                    {
                        ret += ",";
                    }
                }
                return ret + "]";
            }
            return "null";
        }

        public Value(string str) //string constructor
        {
            this.Type = ValueType.String;
            this.String = str;
        }

        public Value(double d) //double constructor
        {
            this.Type = ValueType.Double;
            this.Double = d;
        }

        public Value(char c)
        {
            this.Type = ValueType.Char;
            this.Character = c;
        }

        public Value(List<Value> array)
        {
            this.Type = ValueType.Array;
            this.Array = new List<Value>();
            for (int i = 0; i < array.Count; i++)
            {
                this.Array.Add(array[i]);
            }
        }

        public Value()
        {
            this.Type = ValueType.Null;
        }

        public Value PerformUniaryOperation(Token token)
        {
            if(Type != ValueType.Double)
            {
                throw new InvalidOperandTypeException();
            }
            switch (token)
            {
                case Token.Plus:
                    return new Value(Double);
                case Token.Minus:
                    return new Value(-Double);
                case Token.Not:
                    if(Double == 0)
                    {
                        return new Value(1);
                    }
                    return new Value(0);
            }
            throw new ArgumentException("Unrecognized operand token.");
        }

        public Value PerformBinaryOperation(Token token, Value b)
        {
            Value a = this;
            if (a.Type != b.Type)
            {
                if(a.Type == ValueType.String && b.Type == ValueType.Char)
                {
                    return new Value(a.String + b.Character);
                }
                else if (a.Type == ValueType.Char && b.Type == ValueType.String)
                {
                    return new Value(a.Character + b.String);
                }
                if (token == Token.Equals)
                {
                    return new Value(0);
                }
                else if (token == Token.NotEqual)
                {
                    return new Value(1);
                }
                throw new InvalidCastException();
            }

            if (token == Token.Plus)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double + b.Double);
                else if (a.Type == ValueType.String)
                    return new Value(a.String + b.String);
                else if (a.Type == ValueType.Char)
                    return new Value((char)(a.Character + b.Character));
            }
            else if (token == Token.Equals)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double == b.Double ? 1 : 0);
                else if (a.Type == ValueType.String)
                    return new Value(a.String == b.String ? 1 : 0);
                else if (a.Type == ValueType.Char)
                    return new Value(a.Character == b.Character ? 1 : 0);
                else if (a.Type == ValueType.Null)
                    return new Value(a.Type == ValueType.Null ? 1 : 0);
            }
            else if (token == Token.NotEqual)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double == b.Double ? 0 : 1);
                else if (a.Type == ValueType.String)
                    return new Value(a.String == b.String ? 0 : 1);
                else if (a.Type == ValueType.Null)
                    return new Value(a.Type == ValueType.Null ? 0 : 1);
            }
            else
            {
                if (a.Type == ValueType.String)
                    throw new InvalidOperandTypeException();

                switch (token)
                {
                    case Token.Minus: return new Value(a.Double - b.Double);
                    case Token.Asterisk: return new Value(a.Double * b.Double);
                    case Token.Slash: return new Value(a.Double / b.Double);
                    case Token.Caret: return new Value(Math.Pow(a.Double, b.Double));
                    case Token.Modulous: return new Value(a.Double % b.Double);
                    case Token.Less: return new Value(a.Double < b.Double ? 1 : 0);
                    case Token.More: return new Value(a.Double > b.Double ? 1 : 0);
                    case Token.LessEqual: return new Value(a.Double <= b.Double ? 1 : 0);
                    case Token.MoreEqual: return new Value(a.Double >= b.Double ? 1 : 0);
                    case Token.And: return new Value((a.Double != 0) && (b.Double != 0) ? 1 : 0);
                    case Token.Or: return new Value((a.Double != 0) || (b.Double != 0) ? 1 : 0);
                }
            }

            throw new ArgumentException("Unrecognized operand token.");
        }
    }
}