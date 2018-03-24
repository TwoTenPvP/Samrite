using NativeWifi;
using Newtonsoft.Json.Linq;
using SamriteShared.Messages;
using System;
using System.IO;
using System.Net;

namespace SamriteService.Codebase.Core.Helpers
{
    internal static class LocationHelper
    {
        internal static void GetPosition(Action<DeviceLocation> callback)
        {
            WlanClient client = new WlanClient();
            try
            {
                WlanClient.WlanInterface[] interfaces = client.Interfaces;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.googleapis.com/geolocation/v1/geolocate?key=AIzaSyCeVlO37S9XdcEBdSTATsRPWMcY9B0xJGc");
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";
                using (StreamWriter writer = new StreamWriter(request.GetRequestStream()))
                {
                    string json = "{ \"wifiAccessPoints\": [";
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        Wlan.WlanAvailableNetwork[] networks = interfaces[i].GetAvailableNetworkList(Wlan.WlanGetAvailableNetworkFlags.IncludeAllAdhocProfiles);
                        Wlan.WlanBssEntry[] bssEntries = interfaces[i].GetNetworkBssList();
                        for (int j = 0; j < bssEntries.Length; j++)
                        {
                            if (j > 0)
                                json += ",";
                            string mac = BitConverter.ToString(bssEntries[j].dot11Bssid).Replace('-', ':').ToLower();
                            json += "{\"macAddress\": \"" + mac + "\",\"signalStrength\": " + bssEntries[j].rssi + "}";

                        };
                    }
                    json += "]}";
                    writer.Write(json);
                    writer.Flush();
                }
                HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse();
                using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    JObject jObject = JObject.Parse(streamReader.ReadToEnd());
                    float lng = (float)jObject.SelectToken("location.lng");
                    float lat = (float)jObject.SelectToken("location.lat");
                    float accuracy = (float)jObject.SelectToken("accuracy");
                    callback(new DeviceLocation()
                    {
                        accuracy = accuracy,
                        isValid = true,
                        latitude = lat,
                        longitude = lng
                    });
                }
            }
            catch
            {
                callback(new DeviceLocation()
                {
                    isValid = false
                });
            }
        }
    }
}
