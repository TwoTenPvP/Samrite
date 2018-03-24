using SamriteShared.Messages;
using System.IO;
using System.Security.Cryptography;

namespace SamriteShared
{
    public static class Serializer
    {
        private static byte[] AESEncryptBytes(byte[] clearBytes, byte[] passBytes, byte[] saltBytes)
        {
            if (passBytes == null || saltBytes == null)
                return clearBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged alg = new RijndaelManaged())
                {
                    alg.Key = passBytes;
                    alg.IV = saltBytes;
                    using (CryptoStream cs = new CryptoStream(ms, alg.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                        return ms.ToArray();
                    }
                }
            }
        }

        private static byte[] AESDecryptBytes(byte[] cryptBytes, byte[] passBytes, byte[] saltBytes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged alg = new RijndaelManaged())
                {
                    alg.Key = passBytes;
                    alg.IV = saltBytes;
                    using (CryptoStream cs = new CryptoStream(ms, alg.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cryptBytes, 0, cryptBytes.Length);
                        cs.Close();
                        return ms.ToArray();
                    }
                }
            }
        }

        public static T Deserialize<T>(byte[] binary, byte[] aesKey)
        {
            using (MemoryStream baseStream = new MemoryStream(binary))
            {
                MessageBase baseMsg = ProtoBuf.Serializer.Deserialize<MessageBase>(baseStream);
                using (MemoryStream payloadStream = new MemoryStream(AESDecryptBytes(baseMsg.Payload, aesKey, baseMsg.Salt)))
                {
                    return ProtoBuf.Serializer.Deserialize<T>(payloadStream);
                }
            }
        }

        public static byte[] Serialize<T>(T message, byte[] aesKey)
        {
            using (MemoryStream payloadStream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<T>(payloadStream, message);
                byte[] salt = new byte[16];
                using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
                {
                    rng.GetBytes(salt);
                }
                using (MemoryStream encryptedPayloadStream = new MemoryStream(AESEncryptBytes(payloadStream.ToArray(), aesKey, salt)))
                {
                    MessageBase baseMsg = new MessageBase()
                    {
                        Payload = encryptedPayloadStream.ToArray(),
                        Salt = salt
                    };
                    using (MemoryStream messageStream = new MemoryStream())
                    {
                        ProtoBuf.Serializer.Serialize<MessageBase>(messageStream, baseMsg);
                        return messageStream.ToArray();
                    }
                }
            }
        }
    }
}
