using SamriteService.Codebase.Core.Helpers;
using System.Threading;

namespace SamriteService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
#if !DEBUG
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new SamriteService()
            };
            ServiceBase.Run(ServicesToRun);
#else
            SamriteService service = new SamriteService();
            service.Start();

            while (true)
            {
                Thread.Sleep(100);
            }
#endif
        }
    }
}
