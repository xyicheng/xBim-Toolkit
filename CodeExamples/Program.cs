#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Filename:    Program.cs
// Published:   31 Oct 2012
// (See accompanying copyright.rtf)

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CodeExamples
{
    class Program
    {
        // Harness that enables the user to see sample files in this project and choose which one to execute
        static void Main(string[] args)
        {
            while (true)
            {
                // See the Samples folder for all examples
                var sampleFiles = GetSamplesInThisAssembly().ToList<Type>();

                int choice = ChooseSample(sampleFiles);

                if (choice == Exit)
                {
                    Console.WriteLine("Quitting...");
                    break;
                }
                else if (choice == AllSamples)
                {
                    RunAllSamples(sampleFiles);
                }
                else
                {
                    RunSample(sampleFiles[choice - 1]);
                }
            }
           
        }

        private const int AllSamples = 0;
        private const int Exit = -1;

        private static int ChooseSample(List<Type> sampleFiles)
        {
            ListSamples(sampleFiles);

            int choice = -1;
            while (choice < 0 || choice > sampleFiles.Count)
            {
                Console.Write("Enter Sample number (or 'q' to quit): ");

                string answer = Console.ReadLine();
                if (String.IsNullOrEmpty(answer))
                    continue;
                if (answer.ToLower() == "q")
                    break;
                int.TryParse(answer, out choice);
            }
            return choice;
        }

        private static void ListSamples(List<Type> sampleFiles)
        {
            Console.WriteLine("Choose a Sample file to run:");
            Console.WriteLine();
            Console.WriteLine(" 0) All Samples");
            int idx = 0;
            foreach (Type item in sampleFiles)
            {
                Console.WriteLine("{0,2}) {1}", ++idx, item.Name);
            }
            Console.WriteLine(" q) Quit");
        }

        private static void RunSample(Type type)
        {
            ISample sample = CreateSample(type);

            if (sample != null)
            {
                // Run the sample
                sample.Run();
            }
        }

        private static void RunAllSamples(List<Type> sampleFiles)
        {
            foreach (Type type in sampleFiles)
            {
                RunSample(type);
            }
        }

        private static ISample CreateSample(Type t)
        {
            return (Activator.CreateInstance(t)) as ISample;
        }

        private static IEnumerable<Type> GetSamplesInThisAssembly()
        {
            // Load all implementations of ISample.

            var sampleFiles = from type in Assembly.GetExecutingAssembly().GetTypes()
                              where typeof(ISample).IsAssignableFrom(type) && type.IsClass && !type.IsAbstract
                              //orderby ((type.GetCustomAttribute<SampleFileAttribute>() ?? new SampleFileAttribute()).Sequence)
                              select type;

            return sampleFiles;
        }
    }


}
