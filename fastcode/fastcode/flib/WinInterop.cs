using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using fastcode.runtime;

namespace fastcode.flib
{
    class WinInterop : Library
    {
        public string WorkingDirectory { get; set; }

        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            WorkingDirectory = Program.CurrentDicectory;
            functions.Add("system", runCommand);
            functions.Add("cmd", runCommand);
            functions.Add("getWorkingDir", GetWorkingDirectory);
            functions.Add("setWorkingDir", SetWorkingDirectory);
            functions.Add("readFileText", readFileText);
            functions.Add("readFileLines", readFileLines);
            functions.Add("writeFileText", writeFileText);
            functions.Add("writeFileLines", writeFileLines);
        }

        public Value runCommand(List<Value> arguments)
        {
            string argumentStr = string.Empty;
            foreach(Value argument in arguments)
            {
                if(argumentStr != string.Empty)
                {
                    argumentStr += ' ';
                }
                if(argument.Type == runtime.ValueType.String)
                {
                    if(argument.String.Contains(' ') && !(argument.String.StartsWith("\"") || argument.String.EndsWith("\"")))
                    {
                        argumentStr += "\"" + argument.String + "\"";
                    }
                    else
                    {
                        argumentStr += argument.String;
                    }
                }
                else if(argument.Type == runtime.ValueType.Double)
                {
                    argumentStr += argument.Double.ToString();
                }
                else
                {
                    throw new Exception("An invalid argument type has been passed into a built in function.");
                }
            }

            Process CMDInstance = new Process();
            CMDInstance.StartInfo.FileName = "cmd.exe";
            CMDInstance.StartInfo.RedirectStandardInput = true;
            CMDInstance.StartInfo.RedirectStandardOutput = true;
            CMDInstance.StartInfo.RedirectStandardError = true;
            CMDInstance.StartInfo.UseShellExecute = false;
            CMDInstance.StartInfo.Arguments = "/c \"cd /d \""+WorkingDirectory+"\" && " + argumentStr+"\"";
            StringBuilder output = new StringBuilder();
            CMDInstance.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                {
                    output.AppendLine(e.Data);
                }
            };
            CMDInstance.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                { 
                    output.AppendLine(e.Data);
                }
            };
            CMDInstance.Start();
            CMDInstance.BeginOutputReadLine();
            CMDInstance.BeginErrorReadLine();
            CMDInstance.WaitForExit();
            return new Value(output.ToString());
        }

        public Value GetWorkingDirectory(List<Value> arguments)
        {
            if (arguments.Count != 0)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            return new Value(WorkingDirectory);
        }

        public Value SetWorkingDirectory(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            WorkingDirectory = arguments[0].String;
            return Value.Null;
        }

        public Value readFileText(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(File.ReadAllText(arguments[0].String));
        }

        public Value readFileLines(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Value lines = new Value(new List<Value>());
            foreach(string line in File.ReadAllLines(arguments[0].String))
            {
                lines.Array.Add(new Value(line));
            }
            return lines;
        }

        public Value writeFileText(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String || arguments[1].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            File.WriteAllText(arguments[0].String, arguments[1].String);
            return Value.Null;
        }

        public Value writeFileLines(List<Value> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String || arguments[1].Type != runtime.ValueType.Array)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            string[] lines = new string[arguments[1].Array.Count];
            int i = 0;
            foreach (Value value in arguments[1].Array)
            {
                lines[i] = value.ToString();
                i++;
            }
            File.WriteAllLines(arguments[0].String, lines);
            return Value.Null;
        }
    }
}
