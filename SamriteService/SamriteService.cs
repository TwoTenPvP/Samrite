using SamriteService.Codebase.Core;
using SamriteService.Codebase.Core.Networking;
using System.ServiceProcess;
using System.Timers;

namespace SamriteService
{
    public partial class SamriteService : ServiceBase
    {
        public SamriteService()
        {
            InitializeComponent();
        }

        internal void Start()
        {
            OnStart(new string[0]);
        }

        protected override void OnStart(string[] args)
        {
            NetworkManager.Init();
            Timer timer = new Timer
            {
                Interval = Config.NETWORK_RECONNECT_DELAY_MS
            };
            timer.Elapsed += new ElapsedEventHandler((sender, e) => 
            {
                NetworkManager.CheckConnection();
            });
            timer.Start();
        }

        protected override void OnStop()
        {
            NetworkManager.Shutdown();
        }
    }
}
