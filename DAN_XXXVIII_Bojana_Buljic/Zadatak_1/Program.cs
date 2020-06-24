using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zadatak_1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Routes are generating...");
            Console.WriteLine("Manager selecting best routes...\n");
            Truck truck = new Truck();
            truck.PerformDelivery();

            Console.ReadLine();
        }
    }
}
