using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace eSignCloudRestAPI
{
    class MakeSignature
    {
        private String data;
        private String key;
        private String passKey;

        public MakeSignature(String data, String PriKeyPath, String PriKeyPass)
        {
            this.data = data;
            this.key = PriKeyPath;
            this.passKey = PriKeyPass;
        }

        public String getSignature()
        {
            RSACryptoServiceProvider key = GetKey();
            return Sign(this.data, key);
        }

        public static string Sign(string content, RSACryptoServiceProvider rsa)
        {
            RSACryptoServiceProvider crsa = rsa;
            byte[] Data = Encoding.UTF8.GetBytes(content);
            byte[] signData = crsa.SignData(Data, "sha1");
            return Convert.ToBase64String(signData);
        }
        private RSACryptoServiceProvider GetKey()
        {
            X509Certificate2 cert2 = new X509Certificate2(this.key, this.passKey, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)cert2.PrivateKey;
            return rsa;
        }
    }
}
