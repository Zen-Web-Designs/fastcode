using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Process CMDInstance;
            CMDInstance = new Process();
            CMDInstance.StartInfo.FileName = "cmd.exe";
            CMDInstance.StartInfo.RedirectStandardInput = true;
            CMDInstance.StartInfo.RedirectStandardOutput = true;
            CMDInstance.StartInfo.CreateNoWindow = false;
            CMDInstance.StartInfo.UseShellExecute = false;
            CMDInstance.StartInfo.Arguments = "/C " + argumentStr;
            CMDInstance.Start();
            CMDInstance.WaitForExit();
            return new Value(CMDInstance.StandardOutput.ReadToEnd());
        }

        public Value ReadFile()
        {
            return Value.Null;
        }
    }
}
