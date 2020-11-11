using fastcode.runtime;
using System;
using System.Collections.Generic;

namespace fastcode.flib
{
    class Debugger : Library
    {
        Interpreter interpreter;
        Stack<ControlStructure> callStack;

        public bool RequestDebugInterrupt { get; private set; }

        public Debugger(ref Stack<ControlStructure> callStack)
        {
            this.callStack = callStack;
            this.RequestDebugInterrupt = false;
        }

        public override void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter)
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
                    Console.WriteLine("Stack Size: " + (callStack.Count - 1));
                    PrintLine();
                    PrintRow("Identifier", "Arguments");
                    PrintLine();
                    foreach (ControlStructure structure in callStack.ToArray())
                    {
                        if (structure.GetType() == typeof(FunctionStructure))
                        {
                            FunctionStructure function = (FunctionStructure)structure;
                            if (function.Identifier != "MAINSTRUCTURE")
                            {
                                PrintRow(function.Identifier, function.ExpectedArguments.ToString());
                            }
                        }
                    }
                    PrintLine();
                }
                else if (input == "watch")
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
                else if(input == "next")
                {
                    Console.WriteLine("Fastcode has executed the next statement and halted further execution at ROW: " + (interpreter.Position.Row) + ", COL: " + (interpreter.Position.Collumn + 1) + ".");
                    RequestDebugInterrupt = true;
                    return;
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
