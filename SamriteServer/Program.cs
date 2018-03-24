using NetworkCommsDotNet;
using SamriteServer.Core;
using System;

namespace SamriteServer
{
    class Program
    {
        static void Main(string[] args)
        {
            NetworkManager.Start();
            Console.ReadKey();
            NetworkComms.Shutdown();
        }
    }
}
