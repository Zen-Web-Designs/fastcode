using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using fastcode.runtime;

namespace fastcode.flib
{
    class Linq : Library
    {
        public override void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter)
        {
            functions.Add("clear", Append);
            functions.Add("append", Append);
            functions.Add("push", Append);
            functions.Add("peek", Peek);
            functions.Add("pop", Pop);
            functions.Add("insert", Insert);
            functions.Add("del", Delete);
            functions.Add("delete", Delete);
        }

        public static Value Clear(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            arguments[0].Array.Clear();
            return Value.Null;
        }

        public static Value Append(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            arguments[0].Array.Add(arguments[1]);
            return arguments[1];
        }

        public static Value Pop(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Value toret = arguments[0].Array[arguments[0].Array.Count - 1];
            arguments[0].Array.RemoveAt(arguments[0].Array.Count - 1);
            return toret;
        }

        public static Value Peek(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            if (arguments[0].Array.Count > 0)
            {
                return arguments[0].Array[arguments[0].Array.Count - 1];
            }
            else
            {
                return Value.Null;
            }
        }

        public static Value Insert(List<Value> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            arguments[0].Array.Insert((int)arguments[1].Double, arguments[2]);
            return arguments[2];
        }

        public static Value Delete(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Array || arguments[1].Type != runtime.ValueType.Double)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Value value = arguments[0].Array[(int)arguments[1].Double];
            arguments[0].Array.RemoveAt((int)arguments[1].Double);
            return value;
        }
    }
}
