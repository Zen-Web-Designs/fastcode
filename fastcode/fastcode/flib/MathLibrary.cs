using fastcode.runtime;
using System;
using System.Collections.Generic;

namespace fastcode.flib
{
    class PolynomialLibrary:Library
    {
        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            functions.Add("printPolynomial", PrintPolynomial);
            functions.Add("parsePolynomial", ParsePolynomial);
            functions.Add("evaluatePolynomial", EvaluatePolynomial);
            functions.Add("multiplyPolynomial", MultiplyPolynomial);
            functions.Add("polymul", MultiplyPolynomial);
        }

        private static bool IsPolynomial(Value val)
        {
            if (val.Type != runtime.ValueType.Array)
            {
                return false;
            }
            foreach(Value value in val.Array)
            {
                if(value.Type != fastcode.runtime.ValueType.Double)
                {
                    return false;
                }
            }
            return true;
        }

        public static Value EvaluatePolynomial(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (!IsPolynomial(arguments[0]) || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            double total = 0;
            for (int i = 0; i < arguments[0].Array.Count; i++)
            {
                if (arguments[0].Array[i].Double != 0)
                {
                    total = total + Math.Pow(arguments[0].Array[i].Double * arguments[1].Double, i);
                }
            }
            return new Value(total);
        }

        public static Value MultiplyPolynomial(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (!IsPolynomial(arguments[0]) || !IsPolynomial(arguments[1]))
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            List<Value> poly = new List<Value>(new Value[arguments[0].Array.Count + arguments[1].Array.Count-1]);
            for (int i = 0; i < poly.Count; i++)
            {
                poly[i] = new Value(0);
            }
            for (int j = 0; j < arguments[0].Array.Count; j++)
            {
                for (int k = 0; k < arguments[1].Array.Count; k++)
                {
                    poly[j + k] = new Value((arguments[0].Array[j].Double * arguments[1].Array[k].Double) + poly[j+k].Double);
                }
            }
            return new Value(poly);
        }

        public static Value ParsePolynomial(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Queue<char> all = new Queue<char>(arguments[0].String.ToCharArray());
            List<Tuple<double, int>> terms = new List<Tuple<double, int>>();
            while(all.Count > 0)
            {
                int fact = 1;
                while(all.Peek() == ' ' || all.Peek() == '+' || all.Peek() == '-')
                {
                    if(all.Dequeue() == '-')
                    {
                        fact = -1;
                    }
                }
                string coef_string = string.Empty;
                while(char.IsDigit(all.Peek()) || all.Peek() == '.')
                {
                    coef_string += all.Dequeue();
                    if(all.Count <= 0)
                    {
                        terms.Add(new Tuple<double, int>(fact * double.Parse(coef_string), 0));
                        goto escape;
                    }
                }
                if(all.Peek() == 'X' || all.Peek() == 'x')
                {
                    all.Dequeue(); 
                    if (all.Count <= 0)
                    {
                        if (coef_string == string.Empty)
                        {
                            terms.Add(new Tuple<double, int>(fact, 1));
                        }
                        else
                        {
                            terms.Add(new Tuple<double, int>(fact * int.Parse(coef_string), 1));
                        }
                        break;
                    }
                    if (all.Peek() =='^')
                    {
                        all.Dequeue();
                        string power_string = string.Empty;
                        while (char.IsDigit(all.Peek()))
                        {
                            power_string += all.Dequeue();
                            if (all.Count <= 0)
                            {
                                break;
                            }
                        }
                        if(coef_string == string.Empty)
                        {
                            terms.Add(new Tuple<double, int>(fact, int.Parse(power_string)));
                        }
                        else
                        {
                            terms.Add(new Tuple<double, int>(fact*int.Parse(coef_string), int.Parse(power_string)));
                        }
                    }
                    else if(coef_string == string.Empty)
                    {
                        terms.Add(new Tuple<double, int>(fact, 1));
                    }
                    else
                    {
                        terms.Add(new Tuple<double, int>(fact*double.Parse(coef_string), 1));
                    }
                }
                else
                {
                    terms.Add(new Tuple<double, int>(fact*double.Parse(coef_string), 0));
                }
            }
            escape:
                
            Value polynomial = new Value(new List<Value>());
            foreach(Tuple<double,int> term in terms)
            {
                while(term.Item2 >= polynomial.Array.Count)
                {
                    polynomial.Array.Add(new Value(0));
                }
                polynomial.Array[term.Item2] = new Value(polynomial.Array[term.Item2].Double + term.Item1);
            }
            return polynomial;
        }

        public static Value PrintPolynomial(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (!IsPolynomial(arguments[0]))
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            bool isfirst = true;
            for (int i = arguments[0].Array.Count-1; i >= 0; i--)
            {
                if (arguments[0].Array[i].Double != 0)
                {
                    if(isfirst)
                    {
                        isfirst = false;
                    }
                    else if(arguments[0].Array[i].Double > 0)
                    {
                        Console.Write(" + ");
                    }
                    else
                    {
                        Console.Write(" - ");
                    }
                    if (!(i > 0 && arguments[0].Array[i].Double == 1))
                    {
                        Console.Write(Math.Abs(arguments[0].Array[i].Double));
                    }
                    if (i > 0)
                    {
                        Console.Write("X");
                    }
                    if (i > 1)
                    {
                        Console.Write("^" + i);
                    }
                }
            }
            return Value.Null;
        }
    }

    class MathLibrary : Library
    {
        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            interpreter.GlobalVariables.Add("pi", new Value(Math.PI));
            interpreter.GlobalVariables.Add("e", new Value(Math.E));
            functions.Add("pow", Power);
            functions.Add("root", Root);
            functions.Add("floor", Floor);
            functions.Add("ceiling", Ceiling);
            functions.Add("ceil", Ceiling);
            functions.Add("min", Min);
            functions.Add("max", Max);
            functions.Add("sin", Sine);
            functions.Add("sine", Sine);
            functions.Add("cos", Cosine);
            functions.Add("cosine", Cosine);
            functions.Add("tan", Tangent);
            functions.Add("tangent", Tangent);
        }

        public static Value Floor(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Math.Floor(arguments[0].Double));
        }

        public static Value Ceiling(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Math.Ceiling(arguments[0].Double));
        }

        public static Value Power(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Math.Pow(arguments[0].Double, arguments[1].Double));
        }

        public static Value Root(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Math.Pow(arguments[0].Double, (double)1 / arguments[1].Double));
        }

        public static Value Min(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            if(arguments[1].Double < arguments[0].Double)
            {
                return arguments[1];
            }
            return arguments[0];
        }

        public static Value Max(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            if (arguments[1].Double > arguments[0].Double)
            {
                return arguments[1];
            }
            return arguments[0];
        }

        public static Value Sine(List<Value> arguments)
        {
            if (arguments.Count == 1)
            {
                if (arguments[0].Type != runtime.ValueType.Double)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                return new Value(Math.Sin(arguments[0].Double * Math.PI / 180));
            }
            else if(arguments.Count == 2)
            {
                if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.String)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                if(arguments[1].String.ToUpper() == "DEG")
                {
                    return new Value(Math.Sin(arguments[0].Double * Math.PI / 180));
                }
                else if(arguments[1].String.ToUpper() == "RAD")
                {
                    return new Value(Math.Sin(arguments[0].Double));
                }
                else if(arguments[1].String.ToUpper() == "GRAD" || arguments[1].String.ToUpper() == "GON")
                {
                    return new Value(Math.Sin(arguments[0].Double * Math.PI / 200));
                }
                else
                {
                    throw new Exception("Invalid angle measure unit, \"" + arguments[1].String);
                }
            }
            else
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
        }

        public static Value Cosine(List<Value> arguments)
        {
            if (arguments.Count == 1)
            {
                if (arguments[0].Type != runtime.ValueType.Double)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                return new Value(Math.Cos(arguments[0].Double * Math.PI / 180));
            }
            else if (arguments.Count == 2)
            {
                if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.String)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                if (arguments[1].String.ToUpper() == "DEG")
                {
                    return new Value(Math.Cos(arguments[0].Double * Math.PI / 180));
                }
                else if (arguments[1].String.ToUpper() == "RAD")
                {
                    return new Value(Math.Cos(arguments[0].Double));
                }
                else if (arguments[1].String.ToUpper() == "GRAD" || arguments[1].String.ToUpper() == "GON")
                {
                    return new Value(Math.Cos(arguments[0].Double * Math.PI / 200));
                }
                else
                {
                    throw new Exception("Invalid angle measure unit, \"" + arguments[1].String);
                }
            }
            else
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
        }

        public static Value Tangent(List<Value> arguments)
        {
            if (arguments.Count == 1)
            {
                if (arguments[0].Type != runtime.ValueType.Double)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                return new Value(Math.Tan(arguments[0].Double * Math.PI / 180));
            }
            else if (arguments.Count == 2)
            {
                if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.String)
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
                if (arguments[1].String.ToUpper() == "DEG")
                {
                    return new Value(Math.Tan(arguments[0].Double * Math.PI / 180));
                }
                else if (arguments[1].String.ToUpper() == "RAD")
                {
                    return new Value(Math.Tan(arguments[0].Double));
                }
                else if (arguments[1].String.ToUpper() == "GRAD" || arguments[1].String.ToUpper() == "GON")
                {
                    return new Value(Math.Tan(arguments[0].Double * Math.PI / 200));
                }
                else
                {
                    throw new Exception("Invalid angle measure unit, \"" + arguments[1].String);
                }
            }
            else
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
        }
    }
}
