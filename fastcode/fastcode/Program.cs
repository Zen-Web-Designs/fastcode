using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using fastcode.runtime;

namespace fastcode
{
    class Program
    {
        public static List<string> Lines = new List<string>();
        static ConsoleColor defaultColor;

        static void Main(string[] args)
        {
            Console.Title = "FastCode";
            defaultColor = Console.ForegroundColor;

            if (args.Length == 0)
            {
                Console.WriteLine("FASTCODE prototype version 1\nWritten by Michael Wang\n");
                while (true)
                {
                    Console.Write((Lines.Count + 1) + ": ");
                    string input = Console.ReadLine();
                    if (input == string.Empty)
                    {
                        Console.Write("RUN?(Y/N)");
                        while (true)
                        {
                            ConsoleKeyInfo key = Console.ReadKey();
                            if (key.Key == ConsoleKey.Y)
                            {
                                Console.WriteLine();
                                run(string.Join("\n", Lines));
                                Console.WriteLine();
                                break;
                            }
                            else if (key.Key == ConsoleKey.N)
                            {
                                Console.WriteLine("\n");
                                break;
                            }
                            else if(key.Key == ConsoleKey.Enter)
                            {
                                Console.Write("\b\b\b\b\b\b\b\b\bRUN?(Y/N)");
                            }
                            else
                            {
                                Console.Write("\b\b\b\b\b\b\b\b\b\bRUN?(Y/N) \b");
                            }
                        }
                        Console.Write("ERASE?(Y/N)");
                        while (true)
                        {
                            ConsoleKeyInfo key = Console.ReadKey();
                            if (key.Key == ConsoleKey.Y)
                            {
                                Lines.Clear();
                                Console.WriteLine();
                                break;
                            }
                            else if (key.Key == ConsoleKey.N)
                            {
                                Console.WriteLine();
                                break;
                            }
                            else if (key.Key == ConsoleKey.Enter)
                            {
                                Console.Write("\b\b\b\b\b\b\b\b\b\b\bERASE?(Y/N)");
                            }
                            else
                            {
                                Console.Write("\b\b\b\b\b\b\b\b\b\b\b\bERASE?(Y/N) \b");
                            }
                        }
                    }
                    else
                    {
                        Lines.Add(input);
                    }
                }
            }
            else
            {
                string code = File.ReadAllText(args[0]);
                Lines = code.Split('\n').ToList();
                run(code);
            }
        }

        static void run(string source)
        {
            Interpreter interpreter = new Interpreter(Console.Out,Console.In,source);
            //interpreter.Start();
            try
            {
                interpreter.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR at ROW: " + (interpreter.Position.Row + 1) + ", COL: " + (interpreter.Position.Collumn + 1)+".");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\"" + Lines[interpreter.Position.Row] + "\"\n");
                Console.ForegroundColor = defaultColor;
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
            }
            
        }
    }
}
