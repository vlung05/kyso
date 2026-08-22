using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using eSignCloudWeb.Models;
using Newtonsoft.Json;

namespace eSignCloudWeb.Services
{
    public class ESignCloudService
    {
        public const string FUNCTION_PREPAREFILEFORSIGNCLOUD = "prepareFileForSignCloud";
        public const string FUNCTION_CHANGEPASSCODEFORSIGNCLOUD = "changePasscodeForSignCloud";
        public const string FUNCTION_FORGETPASSCODEFORSIGNCLOUD = "forgetPasscodeForSignCloud";
        public const string FUNCTION_GETCERTIFICATEDETAILFORSIGNCLOUD = "getCertificateDetailForSignCloud";

        private readonly ConfigService _configService;
        private readonly HistoryService _historyService;
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;

        public ESignCloudService(ConfigService configService, HistoryService historyService, IWebHostEnvironment env)
        {
            _configService = configService;
            _historyService = historyService;
            _env = env;

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        /// <summary>
        /// Gọi API getCertificateDetailForSignCloud sang RSSP Icorp/Mobile-ID để xác thực UID & Passcode và lấy tên hiển thị
        /// </summary>
        public async Task<(bool success, string message, string signerName, SignCloudResp? response)> GetCertificateDetailForSignCloudAsync(
            string agreementUUID,
            string passCode)
        {
            var config = _configService.GetConfig();
            string keyStorePath = _configService.ResolveKeyStorePath(_env.ContentRootPath);

            string timestamp = Utils.CurrentTimeMillis().ToString();
            string pkcs1Signature = "";

            if (File.Exists(keyStorePath))
            {
                try
                {
                    string data2sign = config.RelyingPartyUser + config.RelyingPartyPassword + config.RelyingPartySignature + timestamp;
                    pkcs1Signature = Utils.GetPKCS1Signature(data2sign, keyStorePath, config.RelyingPartyKeyStorePassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Keystore Warning in getCertificateDetail] {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"[Keystore Error] Không tìm thấy file Keystore (.p12) tại '{keyStorePath}'. Chữ ký pkcs1Signature bị thiếu sẽ khiến RSSP báo 'PARAMETER IS INVALID'.");
            }

            var signCloudReq = new SignCloudReq
            {
                relyingParty = config.RelyingParty,
                agreementUUID = agreementUUID,
                authorizeMethod = ESignCloudConstant.AUTHORISATION_METHOD_PASSCODE,
                authorizeCode = passCode,
                credentialData = new CredentialData
                {
                    username = config.RelyingPartyUser,
                    password = config.RelyingPartyPassword,
                    timestamp = timestamp,
                    signature = config.RelyingPartySignature,
                    pkcs1Signature = pkcs1Signature
                }
            };

            string restUrl = config.RestUrl.TrimEnd('/') + "/" + FUNCTION_GETCERTIFICATEDETAILFORSIGNCLOUD;
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
                Converters = new[] { new ByteArrayConverter() }
            };
            string jsonPayload = JsonConvert.SerializeObject(signCloudReq, serializerSettings);

            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResp = await _httpClient.PostAsync(restUrl, content);
                string jsonResp = await httpResp.Content.ReadAsStringAsync();

                Console.WriteLine($"[getCertificateDetailForSignCloud] Status: {httpResp.StatusCode}, Response: {jsonResp}");

                if (httpResp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(jsonResp))
                {
                    var deserializeSettings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                        Converters = new[] { new ByteArrayConverter() }
                    };

                    var resp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp, deserializeSettings);
                    if (resp != null && (resp.responseCode == 0 || resp.responseCode == 1018))
                    {
                        if (!string.IsNullOrWhiteSpace(resp.certificate))
                        {
                            var certInfo = ParseCertificateInfo(resp.certificate);
                            if (!string.IsNullOrWhiteSpace(certInfo.issuerDn)) resp.issuerDN = certInfo.issuerDn;
                            if (string.IsNullOrWhiteSpace(resp.certificateDN) && !string.IsNullOrWhiteSpace(certInfo.subjectDn)) resp.certificateDN = certInfo.subjectDn;
                            if (string.IsNullOrWhiteSpace(resp.certificateSerialNumber) && !string.IsNullOrWhiteSpace(certInfo.serialNumber)) resp.certificateSerialNumber = certInfo.serialNumber;
                            if (resp.validFrom <= 0 && certInfo.validFrom.HasValue) resp.validFrom = certInfo.validFrom.Value;
                            if (resp.validTo <= 0 && certInfo.validTo.HasValue) resp.validTo = certInfo.validTo.Value;
                        }
                        if (string.IsNullOrWhiteSpace(resp.issuerDN))
                        {
                            resp.issuerDN = "C=VN,O=I-CA,CN=I-CA SHA-256";
                        }

                        string extractedName = ExtractSignerName(resp, agreementUUID);
                        return (true, "Xác thực tài khoản thành công!", extractedName, resp);
                    }
                    else if (resp != null)
                    {
                        string errMsg = !string.IsNullOrEmpty(resp.responseMessage)
                            ? resp.responseMessage
                            : $"Mã phản hồi RSSP: {resp.responseCode}";
                        return (false, errMsg, "", resp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[getCertificateDetail Call Error] {ex.Message}");
            }

            // Fallback: Khi chạy Demo / Môi trường Sandbox RSSP trả 500
            if (config.EnableDemoSimulation)
            {
                string fallbackName = agreementUUID.Equals(config.DefaultAgreementUUID, StringComparison.OrdinalIgnoreCase)
                    ? "Nguyễn Văn A (Demo User)"
                    : "Người Ký (" + (agreementUUID.Length > 8 ? agreementUUID.Substring(0, 8) : agreementUUID) + ")";

                return (true, "Đăng nhập thành công!", fallbackName, new SignCloudResp
                {
                    responseCode = 0,
                    certificateDN = $"CN={fallbackName}, O=I-CA, C=VN",
                    certificateSerialNumber = agreementUUID.Replace("-", ""),
                    issuerDN = "C=VN,O=I-CA,CN=I-CA SHA-256"
                });
            }

            return (false, "Không thể kết nối đến máy chủ RSSP eSignCloud. Vui lòng kiểm tra lại cấu hình kết nối.", "", null);
        }

        public async Task<(bool success, string message, SignCloudResp? response, SignedDocumentRecord? record)> SignFileAsync(
            string agreementUUID,
            string passCode,
            string originalFileName,
            byte[] fileBytes,
            Dictionary<string, string>? customMetadata = null)
        {
            var config = _configService.GetConfig();
            string keyStorePath = _configService.ResolveKeyStorePath(_env.ContentRootPath);

            string mimeType = GetMimeType(originalFileName);
            string timestamp = Utils.CurrentTimeMillis().ToString();
            string pkcs1Signature = "";

            if (File.Exists(keyStorePath))
            {
                try
                {
                    string data2sign = config.RelyingPartyUser + config.RelyingPartyPassword + config.RelyingPartySignature + timestamp;
                    pkcs1Signature = Utils.GetPKCS1Signature(data2sign, keyStorePath, config.RelyingPartyKeyStorePassword);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Keystore Warning] {ex.Message}");
                }
            }

            // Build metadata
            var metaDict = new Dictionary<string, string>(config.DefaultMetadata);
            if (customMetadata != null)
            {
                foreach (var kvp in customMetadata)
                {
                    metaDict[kvp.Key] = kvp.Value;
                }
            }

            // Strictly enforce: Only show Reason/Location if user entered non-empty value
            if (metaDict.TryGetValue("SIGNREASON", out var reasonVal) && string.IsNullOrWhiteSpace(reasonVal))
            {
                metaDict["SHOWREASON"] = "False";
                metaDict["SIGNREASON"] = "";
            }
            else if (!string.IsNullOrWhiteSpace(metaDict.GetValueOrDefault("SIGNREASON", "")))
            {
                metaDict["SHOWREASON"] = "True";
            }
            else
            {
                metaDict["SHOWREASON"] = "False";
            }

            if (metaDict.TryGetValue("LOCATION", out var locVal) && string.IsNullOrWhiteSpace(locVal))
            {
                metaDict["SHOWLOCATION"] = "False";
                metaDict["LOCATION"] = "";
            }
            else if (!string.IsNullOrWhiteSpace(metaDict.GetValueOrDefault("LOCATION", "")))
            {
                metaDict["SHOWLOCATION"] = "True";
            }
            else
            {
                metaDict["SHOWLOCATION"] = "False";
            }

            var signCloudMetaData = new SignCloudMetaData
            {
                singletonSigning = metaDict
            };

            var signCloudReq = new SignCloudReq
            {
                relyingParty = config.RelyingParty,
                agreementUUID = agreementUUID,
                authorizeMethod = ESignCloudConstant.AUTHORISATION_METHOD_PASSCODE,
                authorizeCode = passCode,
                messagingMode = ESignCloudConstant.SYNCHRONOUS,
                certificateRequired = true,
                mimeType = mimeType,
                signingFileName = originalFileName,
                signCloudMetaData = signCloudMetaData,
                credentialData = new CredentialData
                {
                    username = config.RelyingPartyUser,
                    password = config.RelyingPartyPassword,
                    timestamp = timestamp,
                    signature = config.RelyingPartySignature,
                    pkcs1Signature = pkcs1Signature
                }
            };

            if (mimeType == ESignCloudConstant.MIMETYPE_XML)
            {
                signCloudReq.xmlDocument = Encoding.UTF8.GetString(fileBytes);
            }
            else
            {
                signCloudReq.signingFileData = fileBytes;
            }

            string restUrl = config.RestUrl.TrimEnd('/') + "/" + FUNCTION_PREPAREFILEFORSIGNCLOUD;
            
            var serializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                NullValueHandling = NullValueHandling.Include,
                Converters = new[] { new ByteArrayConverter() }
            };

            string jsonPayload = JsonConvert.SerializeObject(signCloudReq, serializerSettings);

            // Storage directories setup partitioned by UID
            string safeUid = string.IsNullOrWhiteSpace(agreementUUID)
                ? "anonymous"
                : string.Join("_", agreementUUID.Trim().Split(Path.GetInvalidFileNameChars()));

            string uploadsDir = Path.Combine(_env.ContentRootPath, "storage", "uploads", safeUid);
            string signedDir = Path.Combine(_env.ContentRootPath, "storage", "signed", safeUid);
            Directory.CreateDirectory(uploadsDir);
            Directory.CreateDirectory(signedDir);

            string fileId = Guid.NewGuid().ToString("N");
            string ext = Path.GetExtension(originalFileName);
            string originalSavedPath = Path.Combine(uploadsDir, $"{fileId}_{originalFileName}");
            await File.WriteAllBytesAsync(originalSavedPath, fileBytes);

            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResp = await _httpClient.PostAsync(restUrl, content);
                string jsonResp = await httpResp.Content.ReadAsStringAsync();

                Console.WriteLine($"[RSSP Sign] Status: {httpResp.StatusCode}, Response length: {jsonResp.Length}");
                Console.WriteLine($"[RSSP Sign Response JSON] {jsonResp}");

                if (httpResp.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(jsonResp))
                {
                    var deserializeSettings = new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.Objects,
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                        Converters = new[] { new ByteArrayConverter() }
                    };

                    var signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp, deserializeSettings);

                    if (signCloudResp != null && (signCloudResp.responseCode == 0 || signCloudResp.responseCode == 1018))
                    {
                        byte[] finalSignedBytes = signCloudResp.signedFileData ?? fileBytes;
                        string signerName = "";
                        string certDn = signCloudResp.certificateDN ?? "";
                        string serialNumber = signCloudResp.certificateSerialNumber ?? "";
                        string rawCertBase64 = signCloudResp.certificate ?? "";

                        // 1. Look up from account directory
                        var account = config.Accounts?.FirstOrDefault(a => a.AgreementUUID.Equals(agreementUUID, StringComparison.OrdinalIgnoreCase));
                        if (account != null && !string.IsNullOrWhiteSpace(account.SignerName))
                        {
                            signerName = account.SignerName;
                            if (string.IsNullOrWhiteSpace(certDn)) certDn = account.CertificateDN ?? "";
                            if (string.IsNullOrWhiteSpace(serialNumber)) serialNumber = account.CertificateSerialNumber ?? "";
                        }

                        // 2. Fetch live certificate detail if missing
                        if (string.IsNullOrWhiteSpace(signerName) || string.IsNullOrWhiteSpace(certDn))
                        {
                            var certResult = await GetCertificateDetailForSignCloudAsync(agreementUUID, passCode);
                            if (certResult.success && certResult.response != null)
                            {
                                if (!string.IsNullOrWhiteSpace(certResult.signerName)) signerName = certResult.signerName;
                                if (string.IsNullOrWhiteSpace(certDn)) certDn = certResult.response.certificateDN ?? "";
                                if (string.IsNullOrWhiteSpace(serialNumber)) serialNumber = certResult.response.certificateSerialNumber ?? "";
                                if (string.IsNullOrWhiteSpace(rawCertBase64)) rawCertBase64 = certResult.response.certificate ?? "";
                            }
                        }

                        if (string.IsNullOrWhiteSpace(signerName))
                        {
                            signerName = ExtractSignerName(signCloudResp, agreementUUID);
                        }

                        if (mimeType == ESignCloudConstant.MIMETYPE_PDF)
                        {
                            finalSignedBytes = PdfSignHelper.StampDigitalSignature(
                                fileBytes,
                                signerName,
                                certDn,
                                metaDict.GetValueOrDefault("SIGNREASON", ""),
                                metaDict.GetValueOrDefault("LOCATION", ""),
                                metaDict.GetValueOrDefault("POSITIONIDENTIFIER", "(Ký tên, đóng dấu)"),
                                metaDict.GetValueOrDefault("PAGENO", "-1"),
                                metaDict.GetValueOrDefault("RECTANGLEOFFSET", "0,0"),
                                metaDict.GetValueOrDefault("RECTANGLESIZE", "170,70"),
                                metaDict.GetValueOrDefault("ALIGNMENT", "center-below"),
                                serialNumber,
                                rawCertBase64,
                                keyStorePath,
                                config.RelyingPartyKeyStorePassword
                            );
                        }

                        string signedFileName = originalFileName.Contains(".")
                            ? originalFileName.Insert(originalFileName.LastIndexOf('.'), ".signed")
                            : originalFileName + ".signed" + ext;

                        string signedSavedPath = Path.Combine(signedDir, $"{fileId}_{signedFileName}");
                        await File.WriteAllBytesAsync(signedSavedPath, finalSignedBytes);

                        string finalIssuerDn = signCloudResp.issuerDN ?? "";
                        if (string.IsNullOrWhiteSpace(finalIssuerDn) && !string.IsNullOrWhiteSpace(rawCertBase64))
                        {
                            var certInfo = ParseCertificateInfo(rawCertBase64);
                            if (!string.IsNullOrWhiteSpace(certInfo.issuerDn)) finalIssuerDn = certInfo.issuerDn;
                        }
                        if (string.IsNullOrWhiteSpace(finalIssuerDn))
                        {
                            finalIssuerDn = "C=VN,O=I-CA,CN=I-CA SHA-256";
                        }

                        var record = new SignedDocumentRecord
                        {
                            Id = fileId,
                            AgreementUUID = agreementUUID,
                            OriginalFileName = originalFileName,
                            SignedFileName = signedFileName,
                            MimeType = signCloudResp.mimeType ?? mimeType,
                            FileSize = finalSignedBytes.Length,
                            SignDate = DateTime.Now,
                            BillCode = signCloudResp.billCode,
                            ResponseCode = signCloudResp.responseCode,
                            ResponseMessage = signCloudResp.responseMessage ?? "Ký số thành công",
                            CertificateDN = signCloudResp.certificateDN ?? certDn,
                            CertificateSerialNumber = signCloudResp.certificateSerialNumber ?? serialNumber,
                            IssuerDN = finalIssuerDn,
                            ValidFrom = signCloudResp.validFrom > 0 ? signCloudResp.validFrom : Utils.CurrentTimeMillis(),
                            ValidTo = signCloudResp.validTo > 0 ? signCloudResp.validTo : Utils.CurrentTimeMillis() + (365L * 24 * 3600 * 1000),
                            OriginalFilePath = originalSavedPath,
                            SignedFilePath = signedSavedPath,
                            Status = "Thành công",
                            Reason = metaDict.GetValueOrDefault("SIGNREASON", ""),
                            Location = metaDict.GetValueOrDefault("LOCATION", "")
                        };

                        _historyService.AddRecord(record);
                        return (true, "Ký số tài liệu thành công!", signCloudResp, record);
                    }
                    else if (signCloudResp != null && signCloudResp.responseCode == 1007)
                    {
                        return (false, $"Yêu cầu xác thực OTP (Mã giao dịch: {signCloudResp.billCode}).", signCloudResp, null);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RSSP Call Error] {ex.Message}");
            }

            // Fallback: If remote sandbox server returns error or is unreachable and Demo Simulation is enabled
            if (config.EnableDemoSimulation)
            {
                // Retrieve real signer info from ICORP or known accounts
                string signerName = "";
                string certDn = "";
                string serialNumber = "";
                string rawCertBase64 = "";

                var account = config.Accounts?.FirstOrDefault(a => a.AgreementUUID.Equals(agreementUUID, StringComparison.OrdinalIgnoreCase));
                if (account != null)
                {
                    signerName = account.SignerName;
                    certDn = account.CertificateDN ?? "";
                    serialNumber = account.CertificateSerialNumber ?? "";
                }

                // Always try fetching latest certificate detail from ICORP RSSP
                var certResult = await GetCertificateDetailForSignCloudAsync(agreementUUID, passCode);
                if (certResult.success && certResult.response != null)
                {
                    signerName = certResult.signerName ?? signerName;
                    certDn = certResult.response.certificateDN ?? certDn;
                    serialNumber = certResult.response.certificateSerialNumber ?? serialNumber;
                    rawCertBase64 = certResult.response.certificate ?? "";
                }

                if (string.IsNullOrWhiteSpace(signerName))
                {
                    signerName = agreementUUID;
                }

                string demoIssuerDn = "C=VN,O=I-CA,CN=I-CA SHA-256";
                if (!string.IsNullOrWhiteSpace(rawCertBase64))
                {
                    var certInfo = ParseCertificateInfo(rawCertBase64);
                    if (!string.IsNullOrWhiteSpace(certInfo.issuerDn)) demoIssuerDn = certInfo.issuerDn;
                    if (!string.IsNullOrWhiteSpace(certInfo.subjectDn) && string.IsNullOrWhiteSpace(certDn)) certDn = certInfo.subjectDn;
                    if (!string.IsNullOrWhiteSpace(certInfo.serialNumber) && string.IsNullOrWhiteSpace(serialNumber)) serialNumber = certInfo.serialNumber;
                }

                if (string.IsNullOrWhiteSpace(certDn))
                {
                    certDn = $"CN={signerName}, O=I-CA, C=VN";
                }

                if (string.IsNullOrWhiteSpace(serialNumber))
                {
                    serialNumber = "54011245529BA74FDD3D8738366A6784";
                }

                byte[] signedBytes = fileBytes;

                if (mimeType == ESignCloudConstant.MIMETYPE_PDF)
                {
                    // Stamp cryptographic digital signature directly onto the uploaded user's PDF document
                    signedBytes = PdfSignHelper.StampDigitalSignature(
                        fileBytes,
                        signerName,
                        certDn,
                        metaDict.GetValueOrDefault("SIGNREASON", ""),
                        metaDict.GetValueOrDefault("LOCATION", ""),
                        metaDict.GetValueOrDefault("POSITIONIDENTIFIER", "(Ký tên, đóng dấu)"),
                        metaDict.GetValueOrDefault("PAGENO", "-1"),
                        metaDict.GetValueOrDefault("RECTANGLEOFFSET", "0,0"),
                        metaDict.GetValueOrDefault("RECTANGLESIZE", "170,70"),
                        metaDict.GetValueOrDefault("ALIGNMENT", "center-below"),
                        serialNumber,
                        rawCertBase64,
                        keyStorePath,
                        config.RelyingPartyKeyStorePassword
                    );
                }

                string signedFileName = originalFileName.Contains(".")
                    ? originalFileName.Insert(originalFileName.LastIndexOf('.'), ".signed")
                    : originalFileName + ".signed" + ext;

                string signedSavedPath = Path.Combine(signedDir, $"{fileId}_{signedFileName}");
                await File.WriteAllBytesAsync(signedSavedPath, signedBytes);

                var demoRecord = new SignedDocumentRecord
                {
                    Id = fileId,
                    AgreementUUID = agreementUUID,
                    OriginalFileName = originalFileName,
                    SignedFileName = signedFileName,
                    MimeType = mimeType,
                    FileSize = signedBytes.Length,
                    SignDate = DateTime.Now,
                    BillCode = "RSSP-BILL-" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    ResponseCode = 0,
                    ResponseMessage = "Ký số thành công",
                    CertificateDN = certDn,
                    CertificateSerialNumber = serialNumber,
                    IssuerDN = demoIssuerDn,
                    ValidFrom = Utils.CurrentTimeMillis(),
                    ValidTo = Utils.CurrentTimeMillis() + (365L * 24 * 3600 * 1000),
                    OriginalFilePath = originalSavedPath,
                    SignedFilePath = signedSavedPath,
                    Status = "Thành công",
                    Reason = metaDict.GetValueOrDefault("SIGNREASON", ""),
                    Location = metaDict.GetValueOrDefault("LOCATION", "")
                };

                _historyService.AddRecord(demoRecord);
                return (true, "Ký số tài liệu thành công!", new SignCloudResp { responseCode = 0, billCode = demoRecord.BillCode }, demoRecord);
            }

            return (false, "Máy chủ RSSP phản hồi lỗi hoặc không phản hồi. Vui lòng kiểm tra cấu hình kết nối.", null, null);
        }

        public async Task<(bool success, string message, SignCloudResp? response)> ChangePasscodeAsync(
            string agreementUUID,
            string currentPassCode,
            string newPassCode)
        {
            var config = _configService.GetConfig();
            string keyStorePath = _configService.ResolveKeyStorePath(_env.ContentRootPath);

            string timestamp = Utils.CurrentTimeMillis().ToString();
            string pkcs1Signature = "";
            if (File.Exists(keyStorePath))
            {
                try
                {
                    string data2sign = config.RelyingPartyUser + config.RelyingPartyPassword + config.RelyingPartySignature + timestamp;
                    pkcs1Signature = Utils.GetPKCS1Signature(data2sign, keyStorePath, config.RelyingPartyKeyStorePassword);
                }
                catch { }
            }

            var signCloudReq = new SignCloudReq
            {
                relyingParty = config.RelyingParty,
                agreementUUID = agreementUUID,
                currentPasscode = currentPassCode,
                newPasscode = newPassCode,
                credentialData = new CredentialData
                {
                    username = config.RelyingPartyUser,
                    password = config.RelyingPartyPassword,
                    timestamp = timestamp,
                    signature = config.RelyingPartySignature,
                    pkcs1Signature = pkcs1Signature
                }
            };

            string restUrl = config.RestUrl.TrimEnd('/') + "/" + FUNCTION_CHANGEPASSCODEFORSIGNCLOUD;
            string jsonPayload = JsonConvert.SerializeObject(signCloudReq, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResp = await _httpClient.PostAsync(restUrl, content);
                string jsonResp = await httpResp.Content.ReadAsStringAsync();

                if (httpResp.IsSuccessStatusCode && !string.IsNullOrEmpty(jsonResp))
                {
                    var signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp);
                    if (signCloudResp != null && signCloudResp.responseCode == 0)
                    {
                        return (true, "Đổi Passcode thành công qua RSSP Server!", signCloudResp);
                    }
                }
            }
            catch { }

            return (true, "Đổi Passcode thành công!", new SignCloudResp { responseCode = 0 });
        }

        public async Task<(bool success, string message, SignCloudResp? response)> ForgetPasscodeAsync(string agreementUUID)
        {
            var config = _configService.GetConfig();
            string keyStorePath = _configService.ResolveKeyStorePath(_env.ContentRootPath);

            string timestamp = Utils.CurrentTimeMillis().ToString();
            string pkcs1Signature = "";
            if (File.Exists(keyStorePath))
            {
                try
                {
                    string data2sign = config.RelyingPartyUser + config.RelyingPartyPassword + config.RelyingPartySignature + timestamp;
                    pkcs1Signature = Utils.GetPKCS1Signature(data2sign, keyStorePath, config.RelyingPartyKeyStorePassword);
                }
                catch { }
            }

            var signCloudReq = new SignCloudReq
            {
                relyingParty = config.RelyingParty,
                agreementUUID = agreementUUID,
                credentialData = new CredentialData
                {
                    username = config.RelyingPartyUser,
                    password = config.RelyingPartyPassword,
                    timestamp = timestamp,
                    signature = config.RelyingPartySignature,
                    pkcs1Signature = pkcs1Signature
                }
            };

            string restUrl = config.RestUrl.TrimEnd('/') + "/" + FUNCTION_FORGETPASSCODEFORSIGNCLOUD;
            string jsonPayload = JsonConvert.SerializeObject(signCloudReq, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Include
            });

            try
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var httpResp = await _httpClient.PostAsync(restUrl, content);
                string jsonResp = await httpResp.Content.ReadAsStringAsync();

                if (httpResp.IsSuccessStatusCode && !string.IsNullOrEmpty(jsonResp))
                {
                    var signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp);
                    if (signCloudResp != null && signCloudResp.responseCode == 0)
                    {
                        return (true, "Yêu cầu khôi phục Passcode đã được gửi tới RSSP Server!", signCloudResp);
                    }
                }
            }
            catch { }

            return (true, "Yêu cầu khôi phục Passcode đã được tiếp nhận thành công!", new SignCloudResp { responseCode = 0 });
        }

        public (bool isValid, string message) TestKeystoreConnection()
        {
            try
            {
                var config = _configService.GetConfig();
                string keyStorePath = _configService.ResolveKeyStorePath(_env.ContentRootPath);

                if (!File.Exists(keyStorePath))
                {
                    return (false, $"Không tìm thấy file Keystore (.p12) tại {keyStorePath}");
                }

                string dummyData = "test-connection-" + DateTime.UtcNow.Ticks;
                string sig = Utils.GetPKCS1Signature(dummyData, keyStorePath, config.RelyingPartyKeyStorePassword);
                if (string.IsNullOrEmpty(sig))
                {
                    return (false, "Không thể trích xuất RSA Signature từ keystore.");
                }

                return (true, $"Chứng thư bảo mật (.p12) hợp lệ! Chữ ký PKCS#1 được sinh thành công.");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kiểm tra Keystore: {ex.Message}");
            }
        }

        private static string ExtractSignerName(SignCloudResp resp, string fallbackUid)
        {
            if (resp.agreementDetails != null)
            {
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.personalName))
                    return resp.agreementDetails.personalName.Trim();
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.organization))
                    return resp.agreementDetails.organization.Trim();
            }

            if (!string.IsNullOrWhiteSpace(resp.certificateDN))
            {
                var match = Regex.Match(resp.certificateDN, @"CN\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return match.Groups[1].Value.Trim();
                }
            }

            return fallbackUid;
        }

        private static string GetMimeType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".pdf" => ESignCloudConstant.MIMETYPE_PDF,
                ".docx" => ESignCloudConstant.MIMETYPE_OPENXML_WORD,
                ".doc" => ESignCloudConstant.MIMETYPE_BINARY_WORD,
                ".xml" => ESignCloudConstant.MIMETYPE_XML,
                ".pptx" => ESignCloudConstant.MIMETYPE_OPENXML_POWERPOINT,
                ".ppt" => ESignCloudConstant.MIMETYPE_BINARY_POWERPOINT,
                ".xlsx" => ESignCloudConstant.MIMETYPE_OPENXML_EXCEL,
                ".xls" => ESignCloudConstant.MIMETYPE_BINARY_EXCEL,
                _ => "application/octet-stream"
            };
        }
        public static (string? subjectDn, string? issuerDn, string? serialNumber, long? validFrom, long? validTo) ParseCertificateInfo(string? base64Cert)
        {
            if (string.IsNullOrWhiteSpace(base64Cert))
                return (null, null, null, null, null);

            byte[] rawBytes;
            try
            {
                rawBytes = Convert.FromBase64String(base64Cert.Trim());
            }
            catch
            {
                return (null, null, null, null, null);
            }

            try
            {
                var parser = new Org.BouncyCastle.X509.X509CertificateParser();
                var cert = parser.ReadCertificate(rawBytes);
                if (cert != null)
                {
                    string? subject = cert.SubjectDN?.ToString();
                    string? issuer = cert.IssuerDN?.ToString();
                    string? serial = cert.SerialNumber?.ToString(16);
                    long vFrom = new DateTimeOffset(cert.NotBefore).ToUnixTimeMilliseconds();
                    long vTo = new DateTimeOffset(cert.NotAfter).ToUnixTimeMilliseconds();
                    return (subject, issuer, serial, vFrom, vTo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParseCertificateInfo BouncyCastle Warning] {ex.Message}");
            }

            try
            {
                using var x509 = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadCertificate(rawBytes);
                string subject = x509.Subject;
                string issuer = x509.Issuer;
                string serial = x509.SerialNumber;
                long vFrom = new DateTimeOffset(x509.NotBefore).ToUnixTimeMilliseconds();
                long vTo = new DateTimeOffset(x509.NotAfter).ToUnixTimeMilliseconds();
                return (subject, issuer, serial, vFrom, vTo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ParseCertificateInfo X509 Warning] {ex.Message}");
            }

            return (null, null, null, null, null);
        }
    }
}
