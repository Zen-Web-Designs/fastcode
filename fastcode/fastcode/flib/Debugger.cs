using fastcode.runtime;
using System;
using System.Collections.Generic;

namespace fastcode.flib
{
    class Debugger : Library
    {
        Interpreter interpreter;
        Stack<ControlStructure> callStack;
        Stack<DateTime> timerLaps;
        Dictionary<string,FunctionStructure> userFunctions;
        Dictionary<string, BuiltInFunction> builtInFunctions;

        public bool RequestDebugInterrupt { get; private set; }

        public Debugger(ref Stack<ControlStructure> callStack, ref Dictionary<string, FunctionStructure> userFunctions, ref Dictionary<string, BuiltInFunction> builtInFunctions)
        {
            this.callStack = callStack;
            this.userFunctions = userFunctions;
            this.builtInFunctions = builtInFunctions;
            this.timerLaps = new Stack<DateTime>();
            this.RequestDebugInterrupt = false;
        }

        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            this.interpreter = interpreter;
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
            Console.WriteLine("A break point has been hit on ROW: " + (interpreter.Position.Row) + ", COL: " + (interpreter.Position.Collumn+1)+ ".");
            RequestDebugInterrupt = true;
            return Value.Null;
        }

        public Value StartTimer(List<Value> arguments)
        {
            if (arguments.Count != 0)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            timerLaps.Push(DateTime.Now);
            return Value.Null;
        }

        public void PrintWatch()
        {
            PrintLine();
            PrintRow("Identifier", "Accessibility", "Type", "Value");
            foreach (string id in interpreter.GlobalVariables.Keys)
            {
                PrintRow(id, "GLOBAL", interpreter.GlobalVariables[id].Type.ToString(), interpreter.GlobalVariables[id].ToString().Replace("\r\n", "").Replace("\n", ""));
            }
            foreach (ControlStructure structure in callStack.ToArray())
            {
                if (structure.GetType() == typeof(FunctionStructure))
                {
                    FunctionStructure function = (FunctionStructure)structure;
                    if (function.Identifier != "MAINSTRUCTURE")
                    {
                        foreach (string id in function.LocalVariables.Keys)
                        {
                            PrintRow(id, "LOCAL(" + function.Identifier + ")", function.LocalVariables[id].Type.ToString(), function.LocalVariables[id].ToString().Replace("\r\n", "").Replace("\n", ""));
                        }
                    }
                }
            }
        }

        public void PrintCallstack()
        {
            Console.WriteLine("Stack Size: " + (callStack.Count));
            PrintLine();
            PrintRow("Type", "Identifier", "Arguments");
            PrintLine();
            foreach (ControlStructure structure in callStack.ToArray())
            {
                if (structure.GetType() == typeof(FunctionStructure))
                {
                    FunctionStructure function = (FunctionStructure)structure;
                    PrintRow("Function", function.Identifier, function.ExpectedArguments.ToString());
                }
                else
                {
                    PrintRow(structure.GetType().ToString(), "N/A", "N/A");
                }
            }
            PrintLine();
        }

        public void PrintFunctionList()
        {
            Console.WriteLine(userFunctions.Count + " availible user-defined functions.");
            Console.WriteLine(builtInFunctions.Count + " availible built-in functions.");
            PrintLine();
            PrintRow("Identifier","Type");
            PrintLine();
            foreach(string id in builtInFunctions.Keys)
            {
                PrintRow(id, "imported from flib");
            }
            foreach(FunctionStructure function in userFunctions.Values)
            {
                PrintRow(function.Identifier, "user defined");
            }
        }

        public void StartDebugger()
        {
            while (true)
            {
                Console.Write("debuger>");
                string input = Console.ReadLine();
                if (input == string.Empty || input == "continue")
                {
                    RequestDebugInterrupt = false;
                    break;
                }
                else if (input == "callstack")
                {
                    PrintCallstack();
                }
                else if (input == "watch")
                {
                    PrintWatch();
                }
                else if(input == "functions")
                {
                    PrintFunctionList();
                }
                else if(input == "next")
                {
                    Console.WriteLine("Fastcode has executed the next statement and halted further execution at ROW: " + (interpreter.Position.Row) + ", COL: " + (interpreter.Position.Collumn + 1) + ".");
                    RequestDebugInterrupt = true;
                    return;
                }
                else
                {
                    Console.WriteLine("Unrecognized debugger command.");
                }
            }
        }

        //table stuff copied from stack overflow
        static void PrintLine()
        {
            Console.WriteLine(new string('-', Console.BufferWidth-1));
        }

        static void PrintRow(params string[] columns)
        {
            int width = (Console.BufferWidth - columns.Length-1) / columns.Length;
            string row = "|";

            foreach (string column in columns)
            {
                row += AlignCentre(column, width) + "|";
            }
            Console.WriteLine(row);
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
