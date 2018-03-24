using AForge.Video.DirectShow;
using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections.TCP;
using SamriteService.Codebase.Core.Helpers;
using SamriteShared;
using SamriteShared.Messages;
using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace SamriteService.Codebase.Core.Networking
{
    internal static class NetworkManager
    {
        private static TCPConnection connection;
        private static ECDiffieHellmanCng diffieHellman;
        private static byte[] encryptionKey;
        internal static DateTime punishmentReconnectTime = DateTime.MinValue; //This is the time where we are ALLOWED to recheck the connection.

        internal static bool IsConnected()
        {
            return connection != null && connection.ConnectionAlive();
        }

        internal static void Init()
        {
            connection = null;
            diffieHellman = null;
        }

        private static bool isConnecting = false;
        internal static void CheckConnection()
        {
            //We are already connected.
            if ((connection != null && connection.ConnectionAlive()) || DateTime.UtcNow < punishmentReconnectTime || isConnecting)
            {
                return;
            }

            IPHostEntry hostEntry = null;
            Dns.BeginGetHostEntry(Config.DNS_HOST_NAME == "localhost" ? string.Empty : Config.DNS_HOST_NAME, (callback) =>
            {
                isConnecting = true;
                hostEntry = Dns.EndGetHostEntry(callback);
                bool connected = false;
                for (ushort i = 0; i < hostEntry.AddressList.Length; i++)
                {
                    for (byte j = 0; j < Config.PORT_NUMBERS.Length; j++)
                    {
                        try
                        {
                            connection = TCPConnection.GetConnection(new ConnectionInfo(new IPEndPoint(hostEntry.AddressList[i], Config.PORT_NUMBERS[j])));
                            connected = true; //We use this to exit the outer loop
                            OnConnected();
                            break;
                        }
                        catch { }
                    }
                    if (connected)
                        break;
                }
                isConnecting = false;
            }, null);
        }

        internal static void Shutdown()
        {
            NetworkComms.Shutdown();
        }

        private static void OnConnected()
        {
            if (diffieHellman != null)
                diffieHellman.Dispose();

            diffieHellman = new ECDiffieHellmanCng();
            byte[] servicePublicPart = diffieHellman.PublicKey.ToByteArray();
            using (MemoryStream sendStream = new MemoryStream(servicePublicPart.Length + 2))
            {
                using (BinaryWriter writer = new BinaryWriter(sendStream))
                {
                    writer.Write((ushort)servicePublicPart.Length);
                    writer.Write(servicePublicPart);
                }
                connection.SendObject<byte[]>("Handshake", sendStream.GetBuffer());
                connection.AppendIncomingPacketHandler<byte[]>("HandshakeResponse", (header, connection, bytes) =>
                {
                    connection.RemoveIncomingPacketHandler("HandshakeResponse");
                    using (MemoryStream receiveStream = new MemoryStream(bytes))
                    {
                        using (BinaryReader reader = new BinaryReader(receiveStream))
                        {
                            ushort saltLength = reader.ReadUInt16();
                            byte[] salt = reader.ReadBytes(saltLength);
                            ushort keyBlobLength = reader.ReadUInt16();
                            byte[] keyBlob = reader.ReadBytes(keyBlobLength);
                            ushort signatureLength = reader.ReadUInt16();
                            byte[] signature = reader.ReadBytes(signatureLength);

                            using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider())
                            {
                                try
                                {
                                    StringReader stringReader = new StringReader(Properties.Resources.publickey);
                                    XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
                                    RSAParameters rsaParams = (RSAParameters)serializer.Deserialize(stringReader);
                                    csp.ImportParameters(rsaParams);
                                    if (!csp.VerifyData(keyBlob, new SHA512CryptoServiceProvider(), signature))
                                    {
                                        //Something is wrong here. The public blob does not match the signature. Possible MITM.
                                        connection.CloseConnection(true);
                                        connection = null;
                                    }
                                    else
                                    {
                                        //Connection was fine. Key exchange worked. Let's set the encryption up!
                                        byte[] sharedMaterial = diffieHellman.DeriveKeyMaterial(CngKey.Import(keyBlob, CngKeyBlobFormat.EccPublicBlob));
                                        using (Rfc2898DeriveBytes keyDerive = new Rfc2898DeriveBytes(sharedMaterial, salt, 1000))
                                        {
                                            encryptionKey = keyDerive.GetBytes(32); // 32 bytes = 256 bits, for AES encryption. Salt is generated per message
                                        }
                                        SetupMessageHandlers();
                                    }
                                }
                                finally
                                {
                                    csp.PersistKeyInCsp = false;
                                    if (diffieHellman != null)
                                    {
                                        diffieHellman.Dispose();
                                        diffieHellman = null;
                                    }
                                }
                            }
                        }
                    }
                });
            }
        }

        private static void SetupMessageHandlers()
        {
            connection.AppendIncomingPacketHandler<byte>("GetCameraDevices", (header, connection, data) =>
            {
                string[] names = WebcamHelper.GetDeviceNames();
                VideoDevices devices = new VideoDevices()
                {
                    videoDevices = new VideoDevice[names.Length]
                };
                for (int i = 0; i < devices.videoDevices.Length; i++)
                {
                    VideoCapabilities[] capabilities = WebcamHelper.GetDeviceCapabilities(i);
                    devices.videoDevices[i] = new VideoDevice()
                    {
                        name = names[i],
                        capabilities = new VideoDeviceCapabilities[capabilities.Length]
                    };
                    for (int j = 0; j < capabilities.Length; j++)
                    {
                        devices.videoDevices[i].capabilities[j] = new VideoDeviceCapabilities()
                        {
                            averageFramerate = capabilities[j].AverageFrameRate,
                            maxFramerate = capabilities[j].MaximumFrameRate,
                            height = capabilities[j].FrameSize.Height,
                            width = capabilities[j].FrameSize.Width
                        };
                    }
                }
                connection.SendObject<byte[]>("CameraDevices", Serializer.Serialize<VideoDevices>(devices, encryptionKey));
            });

            connection.AppendIncomingPacketHandler<byte[]>("GetCameraImage", (header, connection, data) =>
            {
                GetImage image = Serializer.Deserialize<GetImage>(data, encryptionKey);
                WebcamHelper.GetWebcamImage(image.deviceIndex, image.width, image.height, (bitmapImage) =>
                {
                    connection.SendObject<byte[]>("CameraImage", Serializer.Serialize<ByteData>(new ByteData()
                    {
                        data = WebcamHelper.GetImagePNGBytes(bitmapImage)
                    }, encryptionKey));
                });
            });

            connection.AppendIncomingPacketHandler<byte>("GetDesktopImage", (header, connection, data) =>
            {
                connection.SendObject<byte[]>("DesktopImage", Serializer.Serialize<ByteData>(new ByteData()
                {
                    data = WebcamHelper.GetImagePNGBytes(WebcamHelper.GetDesktopScreenshot())
                }, encryptionKey));
            });

            connection.AppendIncomingPacketHandler<byte>("GetLocation", (header, connection, data) =>
            {
                LocationHelper.GetPosition((location) =>
                {
                    connection.SendObject<byte[]>("Location", Serializer.Serialize<DeviceLocation>(location, encryptionKey));
                });
            });
        }
    }
}
