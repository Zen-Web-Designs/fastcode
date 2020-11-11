﻿using System;
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
        public override void Install(ref Dictionary<string, fastcode.runtime.Interpreter.BuiltInFunction> functions, Interpreter interpreter)
        {
            functions.Add("system", runCommand);
            functions.Add("cmd", runCommand);
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
            CMDInstance.StartInfo.Arguments = "/c " + argumentStr;
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

        public Value ReadFile(List<Value> arguments)
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
            foreach(string line in File.ReadLines(arguments[0].String))
            {
                lines.Array.Add(new Value(line));
            }
            return lines;
        }
    }
}
