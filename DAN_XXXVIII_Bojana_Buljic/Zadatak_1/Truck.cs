using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zadatak_1
{
    /// <summary>
    /// Class for simulating truck company operations.
    /// Creates threads for performing tasks of generating and selecting routes and Loading/Unloading trucks
    /// </summary>
    public class Truck
    {
        public Random rnd = new Random();
        public SemaphoreSlim semaphore = new SemaphoreSlim(2, 2);
        private readonly string routesFile = @"../../Routes.txt";
        //array for best selected routes
        public int[] bestRoutes = new int[10];
        //array of threads representing each delivery truck
        public Thread[] trucks = new Thread[10];

        static readonly object locker = new object();
        //List with all route numbers generated
        public static List<int> listOfRouteNo = new List<int>();
        double unloadingTime { get; set; }
        //counter for threads 
        int counterEnter = 0, counterRestart=0;

        //object for signaling all threads
        public CountdownEvent countdown = new CountdownEvent(10);
        //event for informing file handling
        private AutoResetEvent anEvent = new AutoResetEvent(false);
        

        /// <summary>
        /// Method for generating random route numbers and writing it into file Routes.txt
        /// </summary>
        public void GenerateRouteNo()
        {
            int[] routes = new int[1000];

            //locks the code until writing to file is finished
            lock (routesFile)
            {
                using (StreamWriter sw = new StreamWriter(routesFile))
                {

                    for (int i = 0; i < routes.Length; i++)
                    {
                        routes[i] = rnd.Next(1, 5001);
                        sw.WriteLine(routes[i]);
                    }
                }
                //signal that writing in file is finished
                anEvent.Set();
            }
        }

        /// <summary>
        /// Method reads file Routes and creates array with selected 10 best routes
        /// </summary>
        public void SelectBestRoutes()
        {
            int number;
            lock (routesFile)
            {
                //wait 3000 ms until file is created
                while (!File.Exists(routesFile))
                {
                    anEvent.WaitOne();
                }

                //reading lines from file
                using (StreamReader reader = File.OpenText(routesFile))
                {
                    string line = " ";
                    while ((line = reader.ReadLine()) != null)
                    {
                        //converting each string into number
                        bool convert = Int32.TryParse(line, out number);
                        if (convert && number % 3 == 0)
                        {
                            //Add into list only numbers divisible by 3
                            listOfRouteNo.Add(number);

                        }
                    }
                }
                //sorting list from lowest to largest number
                listOfRouteNo.Sort();
                //Filling array with 10 minimum and distinct values from list
                bestRoutes = listOfRouteNo.Distinct().Take(10).ToArray();

                Console.WriteLine("Best routes are selected. \nManager chooses next routes for trucks:\n");
                //writing selected best routes on Console
                for (int i = 0; i < bestRoutes.Length; i++)
                {
                    Console.WriteLine(bestRoutes[i]);
                }
                Console.WriteLine("\nYou can start loading trucks.\n");
            }
        }

        /// <summary>
        /// Method for controling that loading truks two by two
        /// </summary>
        public void ControlLoading()
        {
            while(true)
            {
                lock(locker)
                {
                    counterEnter++;
                    if (counterEnter > 2)
                        Thread.Sleep(0);
                    else
                    {
                        counterRestart++;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Method simulate trucks loading
        /// </summary>
        /// <param name="loadingTime">Truck loading time</param>
        public void LoadingTrucks(int loadingTime)
        {           
            var name = Thread.CurrentThread.Name;            
            semaphore.Wait();
            Console.WriteLine(name + " is loading for {0} ms ...", loadingTime);
            Thread.Sleep(loadingTime);

            Console.WriteLine(name + " is loaded.");
            semaphore.Release();
            //sending signal that loading is finished
            countdown.Signal();
        }

        /// <summary>
        /// Method assignig routes to trucks and performs truck operations - delivery & unloading
        /// </summary>
        /// <param name="routeNo"></param>
        public void Delivery(object routeNo)
        {
            var name = Thread.CurrentThread.Name;
            //call method for thread control so that 2 by 2 threads are in/trucks are loaded
            ControlLoading();

            int loadingTime = rnd.Next(500, 5001);
            //call method for loading trucks
            LoadingTrucks(loadingTime);

            //counter decrementing until all trucks are loaded
            counterRestart--;
            if (counterRestart== 0)
            {
                counterEnter = 0;
            }

            //wait for all trucks to load
            countdown.Wait();
            
            Console.WriteLine("{0} gets route {1}", name, routeNo);

            //delivery waiting time
            int waitTime = rnd.Next(500, 5001);
            Console.WriteLine("\n{0} started to destination. \nDelivery waiting time is {1} milliseconds.\n", name, waitTime);
            //Thread waits for 3000 ms
            Thread.Sleep(3000);

            if (waitTime > 3000)
            {
                Console.WriteLine("Order for {0} is cancelled.", name);
                Console.WriteLine("{0} returning to starting point.\n", name);
                //returning to starting point
                Thread.Sleep(3000);
                Console.WriteLine("{0} returned to starting point after 3000 ms.", name);
            }
            else
            {
                unloadingTime = Convert.ToDouble(loadingTime) / 1.5;
                Console.WriteLine(name + " arrived.\n Unloading will last " + Convert.ToInt32(unloadingTime) + " milliseconds.\n");
                //truck unloading
                Thread.Sleep(Convert.ToInt32(unloadingTime));
                Console.WriteLine(name + " is unloaded.\n");
            }
        }        

        /// <summary>
        /// Method creating and starting all threads
        /// </summary>
        public void PerformDelivery()
        {
            //Creating and naming thread for generating routes
            Thread getRoutes = new Thread(GenerateRouteNo)
            {
                Name = "routeGenerator"
            };

            //Creating and naming thread for manager's job-selecting best routes
            Thread manager = new Thread(SelectBestRoutes)
            {
                Name = "manager"
            };

            //starting threads
            getRoutes.Start();
            manager.Start();

            //Joining threads-wait till these threads are finished
            getRoutes.Join();
            manager.Join();

            //creating and starting truck threads from thread array
            for (int i = 0; i < 10; i++)
            {
                trucks[i] = new Thread(Delivery)
                {
                    //naming each thread
                    Name = String.Format("Truck_{0}", i + 1)
                };
                trucks[i].Start(bestRoutes[i]);
            }
            for (int i = 0; i < trucks.Length; i++)
            {
                trucks[i].Join();
            }
        }
    }
}
