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
        Interpreter interpreter;


        public WinInterop(string workingDir)
        {
            this.WorkingDirectory = workingDir;
        }

        public override void Install(ref Dictionary<string, BuiltInFunction> functions, Interpreter interpreter)
        {
            this.interpreter = interpreter;
            functions.Add("system", runCommand); Help.addTopic("system", "system,cmd,windows", "*param array","Runs a windows system command.");
            functions.Add("cmd", runCommand); Help.addTopic("cmd", "system,cmd,windows", "*param array", "Runs a windows system command.");
            functions.Add("readFileText", readFileText); 
            functions.Add("readFileLines", readFileLines);
            functions.Add("writeFileText", writeFileText);
            functions.Add("writeFileLines", writeFileLines);
            functions.Add("fileExists", fileExists);
            functions.Add("dirExists", directoryExists);
            functions.Add("directoryExists", directoryExists);
            functions.Add("mkFile", makeFile);
            functions.Add("makeFile", makeFile);
            functions.Add("deleteFile", deleteFile);
            functions.Add("delFile", deleteFile);
            functions.Add("remFile", deleteFile);
            functions.Add("delDir", RemoveDirectory);
            functions.Add("removeDir", RemoveDirectory);
            functions.Add("remDir", RemoveDirectory);
            functions.Add("listFiles", listFiles);
            functions.Add("listDirectories", listDirectories);
            functions.Add("listDirs", listDirectories);
            interpreter.GlobalVariables.Add("cd", new Value(WorkingDirectory));
        }

        private string ascertainFilePath(string reqPath)
        {
            if(interpreter.GlobalVariables["cd"].Type != runtime.ValueType.String)
            {
                throw new Exception("Working Directory must be set to a string type.");
            }
            if(!Directory.Exists(interpreter.GlobalVariables["cd"].String))
            {
                throw new Exception("Working directory must be set to a valid directory.");
            }
            WorkingDirectory = interpreter.GlobalVariables["cd"].String;
            if (reqPath.Contains(":\\"))
            {
                return reqPath;
            }
            return WorkingDirectory + "\\"+ reqPath;
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
                    output.AppendLine(e.Data.Trim());
                }
            };
            CMDInstance.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (e.Data != null)
                { 
                    output.AppendLine(e.Data.Trim());
                }
            };
            CMDInstance.Start();
            CMDInstance.BeginOutputReadLine();
            CMDInstance.BeginErrorReadLine();
            CMDInstance.WaitForExit();
            return new Value(output.ToString());
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
            return new Value(File.ReadAllText(ascertainFilePath(arguments[0].String)));
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
            foreach(string line in File.ReadAllLines(ascertainFilePath(arguments[0].String)))
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
            File.WriteAllText(ascertainFilePath(arguments[0].String), arguments[1].String);
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
            File.WriteAllLines(ascertainFilePath(arguments[0].String), lines);
            return Value.Null;
        }

        public Value fileExists(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(File.Exists(ascertainFilePath(arguments[0].String)) ? 1 : 0);
        }

        public Value directoryExists(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            return new Value(Directory.Exists(ascertainFilePath(arguments[0].String)) ? 1 : 0);
        }

        public Value makeFile(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            File.Create(ascertainFilePath(arguments[0].String)).Close();
            return Value.Null;
        }

        public Value makeDirectory(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            Directory.CreateDirectory(ascertainFilePath(arguments[0].String));
            return Value.Null;
        }

        public Value deleteFile(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            File.Delete(ascertainFilePath(arguments[0].String));
            return Value.Null;
        }

        public Value RemoveDirectory(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            foreach(string file in Directory.GetFiles(ascertainFilePath(arguments[0].String)))
            {
                File.Delete(file);
            }
            foreach(string directory in Directory.GetDirectories(ascertainFilePath(arguments[0].String)))
            {
                invokeBuiltInFunction(RemoveDirectory, new Value(directory));
            }
            return Value.Null;
        }

        public Value listFiles(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                if (arguments.Count == 0)
                {
                    arguments.Add(new Value(""));
                }
                else
                {
                    throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
                }
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            List<Value> fileList = new List<Value>();
            foreach(string file in Directory.GetFiles(ascertainFilePath(arguments[0].String)))
            {
                fileList.Add(new Value(file));
            }
            return new Value(fileList);
        }

        public Value listDirectories(List<Value> arguments)
        {
            if (arguments.Count != 1)
            {
                if (arguments.Count == 0)
                {
                    arguments.Add(new Value(""));
                }
                else
                {
                    throw new ArgumentException("The amount of arguments passed into the function do not match the amount of expected arguments.");
                }
            }
            if (arguments[0].Type != runtime.ValueType.String)
            {
                throw new Exception("An invalid argument type has been passed into a built in function.");
            }
            List<Value> dirList = new List<Value>();
            foreach (string directory in Directory.GetDirectories(ascertainFilePath(arguments[0].String)))
            {
                dirList.Add(new Value(directory));
            }
            return new Value(dirList);
        }
    }
}
