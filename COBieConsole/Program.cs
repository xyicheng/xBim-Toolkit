using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBieExtensions;

namespace COBieConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            XbimMemoryModel model = new XbimMemoryModel();
            model.Open("Munkerud_2x2.IFC");
            COBieP01Registration registration = new COBieP01Registration(model);
            Console.WriteLine("Facility");
            Console.WriteLine("Name = " + registration.Facility.Name);

            Console.WriteLine("Floors");
            foreach (var floor in registration.Floors)
            {
                Console.WriteLine("Name = " + floor.Name);
            }
            

            Console.ReadKey();
        }
    }
}
