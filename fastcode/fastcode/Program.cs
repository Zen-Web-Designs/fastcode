using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
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
                string Query = "SELECT Capacity FROM Win32_PhysicalMemory";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(Query);

                UInt64 Capacity = 0;
                foreach (ManagementObject WniPART in searcher.Get())
                {
                    Capacity += Convert.ToUInt64(WniPART.Properties["Capacity"].Value);
                }

                Console.WriteLine("FASTCODE prototype version 1\nWritten by Michael Wang\n");
                Console.WriteLine(Capacity + " Bytes of Memory Availible\n");

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
                            else
                            {
                                Console.Write("\b \b");
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
                            else
                            {
                                Console.Write("\b \b");
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
                run(code);
            }
        }

        static void run(string source)
        {
            Interpreter interpreter = new Interpreter(Console.Out,Console.In,source);
            interpreter.Start();
            //try
            //{
            //    interpreter.Start();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine("ERROR at ROW: " + (interpreter.Position.Row + 1) + ", COL: " + (interpreter.Position.Collumn + 1) + ", INDEX: " + interpreter.Position.Index + ". The program has been terminated.");
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine("\"" + Lines[interpreter.Position.Row] + "\"\n");
            //    Console.ForegroundColor = defaultColor;
            //    Console.WriteLine(e.GetType());
            //    Console.WriteLine(e.Message);
            //}
        }
    }
}
