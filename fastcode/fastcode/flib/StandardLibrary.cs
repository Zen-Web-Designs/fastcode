using System;
using System.Collections.Generic;
using System.IO;
using fastcode.runtime;

namespace fastcode.flib
{
    public abstract class Library
    {
        public abstract void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter);
    }

    class StandardLibrary : Library
    {
        static private TextWriter OutputWriter;
        static private TextReader InputReader;

        public override void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter)
        {
            functions.Add("split", Split);
            functions.Add("type", GetTypeID);
            functions.Add("stod", StringToDouble);
            functions.Add("dtos", DoubleToString);
            functions.Add("toCharArray", ToCharArray);
            functions.Add("len", Length);
            functions.Add("output", Output);
            functions.Add("out", Output);
            functions.Add("input", Input);
            functions.Add("clone", Clone);
            functions.Add("range", Range);
            OutputWriter = interpreter.Output;
            InputReader = interpreter.Input;
        }

        public static Value Split(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String || arguments[1].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            string[] tokens = arguments[0].String.Split(new string[] { arguments[1].String }, StringSplitOptions.RemoveEmptyEntries);
            Value toret = new Value(new List<Value>());
            foreach(string tok in tokens)
            {
                toret.Array.Add(new Value(tok));
            }
            return toret;
        }

        public static Value Range(List<Value> arguments)
        {
            if (arguments.Count == 2)
            {
                arguments.Add(new Value(1));
            }
            else if (arguments.Count != 3)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double || arguments[1].Type != runtime.ValueType.Double || arguments[2].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            List<Value> returnRange = new List<Value>();
            if((arguments[0].Double > arguments[1].Double))
            {
                if(arguments[2].Double >= 0)
                {
                    throw new Exception("Invalid stepsize. Perhaps you should the switch start and stop values?");
                }
                for (double i = arguments[0].Double; i > arguments[1].Double; i = i + arguments[2].Double)
                {
                    returnRange.Add(new Value(i));
                }
            }
            else
            {
                if (arguments[2].Double <= 0)
                {
                    throw new Exception("Invalid stepsize. Perhaps you should switch the start and stop values?");
                }
                for (double i = arguments[0].Double; i < arguments[1].Double; i = i + arguments[2].Double)
                {
                    returnRange.Add(new Value(i));
                }
            }
            return new Value(returnRange);
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
            throw new Exception("An invalid argument type has been passed into a built in function.");
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
            return Value.Null;
        }

        public static Value GetTypeID(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            return new Value("fastcode.types."+arguments[0].Type);
        }

        public static Value ToCharArray(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            List<Value> value = new List<Value>();
            for (int i = 0; i < arguments[0].String.Length; i++)
            {
                value.Add(new Value(arguments[0].String[i]));
            }
            return new Value(value);
        }

        public static Value StringToDouble(List<Value> arguments)
        {
            if(arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if(arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            try
            {
                return new Value(double.Parse(arguments[0].String));
            }
            catch
            {
                return Value.Null;
            }
        }

        public static Value DoubleToString(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(arguments[0].Double.ToString());
        }

        public Value Clone(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
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
