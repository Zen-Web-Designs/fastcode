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

        static void PrintTitle()
        {
            Console.WriteLine(" ________                      __       ______                   __           ");
            Console.WriteLine("|        \\                    |  \\     /      \\                 |  \\          ");
            Console.WriteLine("| $$$$$$$$______    _______  _| $$_   |  $$$$$$\\  ______    ____| $$  ______  ");
            Console.WriteLine("| $$__   |      \\  /       \\|   $$ \\  | $$   \\$$ /      \\  /      $$ /      \\ ");
            Console.WriteLine("| $$  \\   \\$$$$$$\\|  $$$$$$$ \\$$$$$$  | $$      |  $$$$$$\\|  $$$$$$$|  $$$$$$\\");
            Console.WriteLine("| $$$$$  /      $$ \\$$    \\   | $$ __ | $$   __ | $$  | $$| $$  | $$| $$    $$");
            Console.WriteLine("| $$    |  $$$$$$$ _\\$$$$$$\\  | $$|  \\| $$__/  \\| $$__/ $$| $$__| $$| $$$$$$$$");
            Console.WriteLine("| $$     \\$$    $$|       $$   \\$$  $$ \\$$    $$ \\$$    $$ \\$$    $$ \\$$     \\");
            Console.WriteLine(" \\$$      \\$$$$$$$ \\$$$$$$$     \\$$$$   \\$$$$$$   \\$$$$$$   \\$$$$$$$  \\$$$$$$$");
            Console.WriteLine("FASTCODE prototype version 1, Written by Michael Wang\n");
        }

        static void PrintHelp()
        {
            Console.WriteLine("Synopsis:");
            Console.WriteLine("A perfect blend of C, Java, and Python tailored for those who desire a simple yet powerful programming language.");
        }

        static void Main(string[] args)
        {
            Console.Title = "FastCode";
            defaultColor = Console.ForegroundColor;
            if (args.Length == 0)
            {
                PrintTitle();
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
                                run(string.Join("\n", Lines),Directory.GetCurrentDirectory());
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
                if (File.Exists(args[0]))
                {
                    FileInfo info = new FileInfo(args[0]);
                    string code = File.ReadAllText(info.FullName);
                    Lines = code.Split('\n').ToList();
                    run(code,info.DirectoryName);
                }
                else if(args[0] == "help" || args[0] == "?" || args[0] == "-h")
                {
                    PrintTitle();
                    PrintHelp();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("The file \""+args[0]+"\" doesn't exist.");
                    Console.ForegroundColor = defaultColor;
                }
            }
        }

        static void run(string source, string workingDir)
        {
            Interpreter interpreter = new Interpreter(Console.Out,Console.In,source, workingDir);
            //interpreter.Start();
            try
            {
                interpreter.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR at ROW: " + (interpreter.Position.Row + 1) + ", COL: " + (interpreter.Position.Collumn + 1) + ".");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(Lines[interpreter.Position.Row] + "\n");
                Console.ForegroundColor = defaultColor;
                Console.WriteLine(e.GetType());
                Console.WriteLine(e.Message);
            }
        }
    }
}
