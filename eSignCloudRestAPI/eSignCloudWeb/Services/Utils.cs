using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace eSignCloudWeb.Services
{
    public static class Utils
    {
        public static long CurrentTimeMillis()
        {
            var jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (long)(DateTime.UtcNow - jan1st1970).TotalMilliseconds;
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Encode(byte[] rawData)
        {
            return Convert.ToBase64String(rawData);
        }

        public static byte[] Base64Decode(string base64EncodedData)
        {
            return Convert.FromBase64String(base64EncodedData);
        }

        public static string GetPKCS1Signature(string dataToSign, string keyStorePath, string keyStorePassword)
        {
            if (!File.Exists(keyStorePath))
            {
                throw new FileNotFoundException($"Keystore file not found at path: {keyStorePath}");
            }

            // Strategy 1: BouncyCastle (cross-platform, reliable under IIS without user profile loading)
            try
            {
                using var stream = File.OpenRead(keyStorePath);
                var pkcs12Store = new Org.BouncyCastle.Pkcs.Pkcs12StoreBuilder().Build();
                pkcs12Store.Load(stream, keyStorePassword.ToCharArray());

                Org.BouncyCastle.Crypto.AsymmetricKeyParameter? privateKey = null;
                foreach (string alias in pkcs12Store.Aliases)
                {
                    if (pkcs12Store.IsKeyEntry(alias))
                    {
                        privateKey = pkcs12Store.GetKey(alias).Key;
                        break;
                    }
                }

                if (privateKey != null)
                {
                    var signer = Org.BouncyCastle.Security.SignerUtilities.GetSigner("SHA1withRSA");
                    signer.Init(true, privateKey);
                    byte[] data = Encoding.UTF8.GetBytes(dataToSign);
                    signer.BlockUpdate(data, 0, data.Length);
                    byte[] signature = signer.GenerateSignature();
                    return Convert.ToBase64String(signature);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Utils.GetPKCS1Signature BouncyCastle Warning] {ex.Message}");
            }

            // Strategy 2: .NET Native RSA
            try
            {
                X509Certificate2? cert = null;
                var flagOptions = new[]
                {
                    X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable,
                    X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable,
                    X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable,
                    X509KeyStorageFlags.Exportable
                };

                Exception? lastEx = null;
                foreach (var flags in flagOptions)
                {
                    try
                    {
                        cert = new X509Certificate2(keyStorePath, keyStorePassword, flags);
                        if (cert.HasPrivateKey) break;
                    }
                    catch (Exception ex)
                    {
                        lastEx = ex;
                    }
                }

                if (cert == null || !cert.HasPrivateKey)
                {
                    throw lastEx ?? new InvalidOperationException("Could not load private key from certificate.");
                }

                using (cert)
                {
                    using var rsa = cert.GetRSAPrivateKey();
                    if (rsa == null)
                    {
                        throw new InvalidOperationException("Could not obtain RSA private key from keystore.");
                    }

                    byte[] data = Encoding.UTF8.GetBytes(dataToSign);
                    byte[] signature = rsa.SignData(data, HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                    return Convert.ToBase64String(signature);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating PKCS1 signature from keystore: {ex.Message}", ex);
            }
        }
    }
}
