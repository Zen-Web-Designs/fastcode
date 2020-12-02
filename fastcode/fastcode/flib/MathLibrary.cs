using fastcode.runtime;
using System;
using System.Collections.Generic;

namespace fastcode.flib
{
    class MathLibrary : Library
    {
        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            interpreter.GlobalVariables.Add("pi", new Value(Math.PI));
            interpreter.GlobalVariables.Add("e", new Value(Math.E));
            functions.Add("pow", Power);
            functions.Add("root", Root);
            functions.Add("log", Log);
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
            functions.Add("substitute", Expression.Substitute);
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

        public static Value Log(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Math.Log(arguments[0].Double) / Math.Log(arguments[1].Double));
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
