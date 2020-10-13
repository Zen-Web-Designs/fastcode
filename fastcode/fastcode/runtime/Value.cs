using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastcode.parsing;

namespace fastcode.runtime
{
    public enum ValueType
    {
        Double = 1,
        String = 0,
        Null = -1
    }

    public class Value
    {
        public static Value Null { get { return new Value(); } } //NULL will be defined as it's own type

        public ValueType Type { get; private set; }
        public string String { get; private set; }
        public double Double { get; private set; }

        public override string ToString()
        {
            if(Type == ValueType.Double)
            {
                return Double.ToString();
            }
            else
            {
                return String;
            }
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

        public Value()
        {
            this.Type = ValueType.Null;
        }

        //converts this value to another type
        public Value Convert(ValueType type)
        {
            if(type != Type)
            {
                switch (type)
                {
                    case ValueType.Double:
                        return new Value(double.Parse(String));
                    case ValueType.String:
                        return new Value(ToString());
                    case ValueType.Null:
                        throw new Exception("Cannot convert to type null");
                }
            }
            return this;
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
                try
                {
                    if (a.Type > b.Type)
                        b = b.Convert(a.Type);
                    else
                        a = a.Convert(b.Type);
                }
                catch(Exception e)
                {
                    if(token == Token.Equals)
                    {
                        return new Value(0);
                    }
                    else if(token == Token.NotEqual)
                    {
                        return new Value(1);
                    }
                    throw e;
                }
            }

            if (token == Token.Plus)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double + b.Double);
                else
                    return new Value(a.String + b.String);
            }
            else if (token == Token.Equals)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double == b.Double ? 1 : 0);
                else
                    return new Value(a.String == b.String ? 1 : 0);
            }
            else if (token == Token.NotEqual)
            {
                if (a.Type == ValueType.Double)
                    return new Value(a.Double == b.Double ? 0 : 1);
                else
                    return new Value(a.String == b.String ? 0 : 1);
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