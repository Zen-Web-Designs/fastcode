using fastcode.runtime;
using System;
using System.Collections.Generic;
using System.IO;

namespace fastcode.flib
{
    public delegate void DebuggerCommand(params string[] arguments);

    class Debugger : Library
    {
        private TextWriter OutputWriter;
        private TextReader InputReader;

        Interpreter interpreter;
        Stack<CallFrame> callStack;
        Dictionary<string,FunctionFrame> userFunctions;
        Dictionary<string, BuiltInFunction> builtInFunctions;

        public Dictionary<string, DebuggerCommand> debuggerCommands;

        public bool RequestDebugInterrupt { get; private set; }

        public Debugger(ref Stack<CallFrame> callStack, ref Dictionary<string, FunctionFrame> userFunctions, ref Dictionary<string, BuiltInFunction> builtInFunctions)
        {
            this.callStack = callStack;
            this.userFunctions = userFunctions;
            this.builtInFunctions = builtInFunctions;
            this.RequestDebugInterrupt = false;
            this.debuggerCommands = new Dictionary<string, DebuggerCommand>();
            debuggerCommands.Add("callstack", PrintCallstack);
            debuggerCommands.Add("watch", PrintWatch);
            debuggerCommands.Add("functions", PrintFunctionList);
        }

        bool hasFlag(string[] args,string flag)
        {
            foreach(string arg in args)
            {
                if(arg.Trim() == flag)
                {
                    return true;
                }
            }
            return false;
        }

        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            this.interpreter = interpreter;
            OutputWriter = interpreter.Output;
            InputReader = interpreter.Input;
            functions.Add("assert", Assert);
            functions.Add("breakpoint", Breakpoint);
        }


        public Value Assert(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.Double )
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            if(arguments[0].Double == 0)
            {
                throw new AssertionFailedException();
            }
            return Value.Null;
        }

        public Value Breakpoint(List<Value> arguments)
        {
            if (arguments.Count != 0)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            OutputWriter.WriteLine("A break point has been hit on ROW: " + (interpreter.Position.Row) + ", COL: " + (interpreter.Position.Collumn+1)+ ".");
            RequestDebugInterrupt = true;
            return Value.Null;
        }

        public void PrintWatch(params string[] args)
        {
            PrintLine();
            PrintRow("Identifier", "Accessibility", "Type", "Value");
            if (hasFlag(args, "-g"))
            {
                foreach (string id in interpreter.GlobalVariables.Keys)
                {
                    PrintRow(id, "GLOBAL", interpreter.GlobalVariables[id].Type.ToString(), interpreter.GlobalVariables[id].ToString().Replace("\r\n", "").Replace("\n", ""));
                }
            }
            foreach (CallFrame structure in callStack.ToArray())
            {
                if (structure.GetType() == typeof(FunctionFrame))
                {
                    FunctionFrame function = (FunctionFrame)structure;
                    foreach (string id in function.LocalVariables.Keys)
                    {
                        PrintRow(id, "LOCAL(" + function.Identifier + ")", function.LocalVariables[id].Type.ToString(), function.LocalVariables[id].ToString().Replace("\r\n", "").Replace("\n", ""));
                    }
                }
            }
        }

        public void PrintCallstack(params string[] args)
        {
            OutputWriter.WriteLine("Stack Size: " + (callStack.Count));
            PrintLine();
            PrintRow("Type", "Identifier", "Arguments");
            PrintLine();
            foreach (CallFrame structure in callStack.ToArray())
            {
                if (structure.GetType() == typeof(FunctionFrame))
                {
                    FunctionFrame function = (FunctionFrame)structure;
                    PrintRow("Function", function.Identifier, function.ExpectedArguments.ToString());
                }
                else
                {
                    PrintRow(structure.GetType().ToString(), "N/A", "N/A");
                }
            }
            PrintLine();
        }

        public void PrintFunctionList(params string[] args)
        {
            OutputWriter.WriteLine(userFunctions.Count + " availible user-defined functions.");
            OutputWriter.WriteLine(builtInFunctions.Count + " availible built-in functions.");
            PrintLine();
            PrintRow("Identifier","Type");
            PrintLine();
            foreach(string id in builtInFunctions.Keys)
            {
                PrintRow(id, "imported from flib");
            }
            foreach(FunctionFrame function in userFunctions.Values)
            {
                PrintRow(function.Identifier, "user defined");
            }
        }

        public void StartDebugger()
        {
            while (true)
            {
                OutputWriter.Write("debuger>");
                string input = InputReader.ReadLine();
                if (input == string.Empty || input == "continue")
                {
                    RequestDebugInterrupt = false;
                    break;
                }
                else if(input == "next")
                {
                    OutputWriter.WriteLine("Fastcode has executed the next statement and halted further execution at ROW: " + (interpreter.Position.Row) + ", COL: " + (interpreter.Position.Collumn + 1) + ".");
                    RequestDebugInterrupt = true;
                    return;
                }
                else
                {
                    string[] args = input.Split(' ');
                    if (debuggerCommands.ContainsKey(args[0]))
                    {
                        debuggerCommands[args[0]].Invoke(args);
                    }
                    else
                    {
                        OutputWriter.WriteLine("Unrecognized debugger command.");
                    }
                }
            }
        }

        //table stuff copied from stack overflow
        void PrintLine()
        {
            OutputWriter.WriteLine(new string('-', Console.BufferWidth - 1));
            OutputWriter.WriteLine(new string('-', 25));
        }

        void PrintRow(params string[] columns)
        {
            int width = (Console.BufferWidth - columns.Length-1) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }
            OutputWriter.WriteLine(row);
        }

        static string AlignCentre(string text, int width)
        {
            text = text.Length > width ? text.Substring(0, width - 3) + "..." : text;

            if (string.IsNullOrEmpty(text))
            {
                return new string(' ', width);
            }
            else
            {
                return text.PadRight(width - (width - text.Length) / 2).PadLeft(width);
            }
        }
    }
}
