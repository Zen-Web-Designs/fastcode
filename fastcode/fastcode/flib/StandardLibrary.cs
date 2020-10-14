using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastcode.runtime;

namespace fastcode.flib
{
    abstract class Library
    {
        public abstract void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter);
    }

    class StandardLibrary : Library
    {
        static private TextWriter OutputWriter;
        static private TextReader InputReader;

        public override void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter)
        {
            functions.Add("isd", IsDouble);
            functions.Add("iss", IsString);
            functions.Add("stod", StringToDouble);
            functions.Add("dtos", DoubleToString);
            functions.Add("tochararray", ToCharArray);
            functions.Add("len", Length);
            functions.Add("append", Append);
            functions.Add("del", Delete);
            functions.Add("out", Output);
            functions.Add("in", Input);
            functions.Add("clone", Clone);
            OutputWriter = interpreter.Output;
            InputReader = interpreter.Input;
        }

        public static Value Input(List<Value> arguments)
        {
            if(arguments.Count != 0)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            return new Value(InputReader.ReadLine());
        }

        public static Value Output(List<Value> arguments)
        {
            foreach(Value argument in arguments)
            {
                OutputWriter.Write(argument.ToString());
            }
            return null;
        }

        public static Value IsDouble(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            return (arguments[0].Type == runtime.ValueType.Double ? new Value(1) : new Value(0));
        }

        public static Value ToCharArray(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            List<Value> value = new List<Value>();
            for (int i = 0; i < arguments[0].String.Length; i++)
            {
                value.Add(new Value(arguments[0].String[i]));
            }
            return new Value(value);
        }

        public static Value IsString(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            return (arguments[0].Type == runtime.ValueType.String ? new Value(1) : new Value(0));
        }

        public static Value StringToDouble(List<Value> arguments)
        {
            if(arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if(arguments[0].Type != runtime.ValueType.String)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            return new Value(double.Parse(arguments[0].String));
        }

        public static Value DoubleToString(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            return new Value(arguments[0].Double.ToString());
        }

        public static Value Length(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type == runtime.ValueType.String)
            {
                return new Value(arguments[0].String.Length);
            }
            else if (arguments[0].Type == runtime.ValueType.Array)
            {
                return new Value(arguments[0].Array.Count);
            }
            throw new runtime.InvalidOperandTypeException();
        }

        public static Value Append(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            arguments[0].Array.Add(arguments[1]);
            return arguments[1];
        }

        public static Value Delete(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            Value value = arguments[0].Array[(int)arguments[1].Double];
            arguments[0].Array.RemoveAt((int)arguments[1].Double);
            return value;
        }

        public Value Clone(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new runtime.InvalidOperandTypeException();
            }
            return Clone(arguments[0]);
        }

        private Value Clone(Value argument)
        {
            Value val = new Value(new List<Value>());
            for (int i = 0; i < argument.Array.Count; i++)
            {
                if (argument.Array[i].Type == runtime.ValueType.Array)
                {
                    val.Array[i] = Clone(argument.Array[i]);
                }
                else
                {
                    val.Array[i] = argument.Array[i];
                }
            }
            return val;
        }
    }
}
