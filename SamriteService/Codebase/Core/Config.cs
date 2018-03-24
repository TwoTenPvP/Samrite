namespace SamriteService.Codebase.Core
{
    internal static class Config
    {
        internal static string DNS_HOST_NAME = "localhost";
        internal static uint INCOMMING_NETWORK_BUFFER_SIZE = 65536;
        internal static int[] PORT_NUMBERS = {
            8033
        };
        internal static float MITM_WINDOW_COOLDOWN_HOURS = 3f;
        internal static int NETWORK_RECONNECT_DELAY_MS = 100;
        internal static int MITM_RECONNECT_PUNISHMENT_SECONDS = 5; //If a MITM attack gets detected. We will wait this long before reconnecting. Otherwise uses the normal delay.
        internal static string GOOGLE_LOCATION_API_KEY = "AIzaSyD335f04yG2UskcURqlA5Xd3yY1SRcZZoE";
    }
}
