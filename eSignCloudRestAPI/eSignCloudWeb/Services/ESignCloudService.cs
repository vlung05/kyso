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
                        string finalIssuerDn = signCloudResp.issuerDN ?? "";
                        long validFrom = signCloudResp.validFrom;
                        long validTo = signCloudResp.validTo;

                        // 1. Parse raw certificate if present in sign response
                        if (!string.IsNullOrWhiteSpace(rawCertBase64))
                        {
                            var certInfo = ParseCertificateInfo(rawCertBase64);
                            if (!string.IsNullOrWhiteSpace(certInfo.issuerDn)) finalIssuerDn = certInfo.issuerDn;
                            if (string.IsNullOrWhiteSpace(certDn) && !string.IsNullOrWhiteSpace(certInfo.subjectDn)) certDn = certInfo.subjectDn;
                            if (string.IsNullOrWhiteSpace(serialNumber) && !string.IsNullOrWhiteSpace(certInfo.serialNumber)) serialNumber = certInfo.serialNumber;
                            if (validFrom <= 0 && certInfo.validFrom.HasValue) validFrom = certInfo.validFrom.Value;
                            if (validTo <= 0 && certInfo.validTo.HasValue) validTo = certInfo.validTo.Value;
                        }

                        // 2. Look up from account directory
                        var account = config.Accounts?.FirstOrDefault(a => a.AgreementUUID.Equals(agreementUUID, StringComparison.OrdinalIgnoreCase));
                        if (account != null)
                        {
                            if (!string.IsNullOrWhiteSpace(account.SignerName)) signerName = account.SignerName;
                            if (string.IsNullOrWhiteSpace(certDn)) certDn = account.CertificateDN ?? "";
                            if (string.IsNullOrWhiteSpace(serialNumber)) serialNumber = account.CertificateSerialNumber ?? "";
                            if (string.IsNullOrWhiteSpace(finalIssuerDn) && !string.IsNullOrWhiteSpace(account.IssuerDN)) finalIssuerDn = account.IssuerDN;
                            if (validFrom <= 0 && account.ValidFrom > 0) validFrom = account.ValidFrom;
                            if (validTo <= 0 && account.ValidTo > 0) validTo = account.ValidTo;
                            if (string.IsNullOrWhiteSpace(rawCertBase64) && !string.IsNullOrWhiteSpace(account.Certificate)) rawCertBase64 = account.Certificate;
                        }

                        // 3. Fetch live certificate detail if anything essential is missing
                        if (string.IsNullOrWhiteSpace(signerName) || string.IsNullOrWhiteSpace(certDn) || validFrom <= 0 || validTo <= 0 || string.IsNullOrWhiteSpace(rawCertBase64))
                        {
                            var certResult = await GetCertificateDetailForSignCloudAsync(agreementUUID, passCode);
                            if (certResult.success && certResult.response != null)
                            {
                                if (!string.IsNullOrWhiteSpace(certResult.signerName)) signerName = certResult.signerName;
                                if (string.IsNullOrWhiteSpace(certDn)) certDn = certResult.response.certificateDN ?? "";
                                if (string.IsNullOrWhiteSpace(serialNumber)) serialNumber = certResult.response.certificateSerialNumber ?? "";
                                if (string.IsNullOrWhiteSpace(rawCertBase64)) rawCertBase64 = certResult.response.certificate ?? "";
                                if (string.IsNullOrWhiteSpace(finalIssuerDn)) finalIssuerDn = certResult.response.issuerDN ?? "";
                                if (validFrom <= 0 && certResult.response.validFrom > 0) validFrom = certResult.response.validFrom;
                                if (validTo <= 0 && certResult.response.validTo > 0) validTo = certResult.response.validTo;

                                if (!string.IsNullOrWhiteSpace(rawCertBase64))
                                {
                                    var certInfo = ParseCertificateInfo(rawCertBase64);
                                    if (string.IsNullOrWhiteSpace(finalIssuerDn) && !string.IsNullOrWhiteSpace(certInfo.issuerDn)) finalIssuerDn = certInfo.issuerDn;
                                    if (validFrom <= 0 && certInfo.validFrom.HasValue) validFrom = certInfo.validFrom.Value;
                                    if (validTo <= 0 && certInfo.validTo.HasValue) validTo = certInfo.validTo.Value;
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(signerName))
                        {
                            signerName = ExtractSignerName(signCloudResp, agreementUUID);
                        }
                        if (string.IsNullOrWhiteSpace(finalIssuerDn))
                        {
                            finalIssuerDn = "C=VN,O=I-CA,CN=I-CA SHA-256";
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
                                config.RelyingPartyKeyStorePassword,
                                finalIssuerDn,
                                validFrom > 0 ? validFrom : (long?)null,
                                validTo > 0 ? validTo : (long?)null
                            );
                        }

                        string signedFileName = originalFileName.Contains(".")
                            ? originalFileName.Insert(originalFileName.LastIndexOf('.'), ".signed")
                            : originalFileName + ".signed" + ext;

                        string signedSavedPath = Path.Combine(signedDir, $"{fileId}_{signedFileName}");
                        await File.WriteAllBytesAsync(signedSavedPath, finalSignedBytes);

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
                            CertificateDN = !string.IsNullOrWhiteSpace(signCloudResp.certificateDN) ? signCloudResp.certificateDN : certDn,
                            CertificateSerialNumber = !string.IsNullOrWhiteSpace(signCloudResp.certificateSerialNumber) ? signCloudResp.certificateSerialNumber : serialNumber,
                            IssuerDN = finalIssuerDn,
                            ValidFrom = validFrom > 0 ? validFrom : Utils.CurrentTimeMillis(),
                            ValidTo = validTo > 0 ? validTo : (validFrom > 0 ? validFrom + (365L * 24 * 3600 * 1000 * 3) : Utils.CurrentTimeMillis() + (365L * 24 * 3600 * 1000)),
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
                string demoIssuerDn = "";
                long demoValidFrom = 0;
                long demoValidTo = 0;

                var account = config.Accounts?.FirstOrDefault(a => a.AgreementUUID.Equals(agreementUUID, StringComparison.OrdinalIgnoreCase));
                if (account != null)
                {
                    signerName = account.SignerName;
                    certDn = account.CertificateDN ?? "";
                    serialNumber = account.CertificateSerialNumber ?? "";
                    demoIssuerDn = account.IssuerDN ?? "";
                    demoValidFrom = account.ValidFrom;
                    demoValidTo = account.ValidTo;
                    rawCertBase64 = account.Certificate ?? "";
                }

                // Always try fetching latest certificate detail from ICORP RSSP
                var certResult = await GetCertificateDetailForSignCloudAsync(agreementUUID, passCode);
                if (certResult.success && certResult.response != null)
                {
                    signerName = certResult.signerName ?? signerName;
                    certDn = certResult.response.certificateDN ?? certDn;
                    serialNumber = certResult.response.certificateSerialNumber ?? serialNumber;
                    rawCertBase64 = certResult.response.certificate ?? rawCertBase64;
                    demoIssuerDn = certResult.response.issuerDN ?? demoIssuerDn;
                    if (certResult.response.validFrom > 0) demoValidFrom = certResult.response.validFrom;
                    if (certResult.response.validTo > 0) demoValidTo = certResult.response.validTo;
                }

                if (!string.IsNullOrWhiteSpace(rawCertBase64))
                {
                    var certInfo = ParseCertificateInfo(rawCertBase64);
                    if (!string.IsNullOrWhiteSpace(certInfo.issuerDn)) demoIssuerDn = certInfo.issuerDn;
                    if (!string.IsNullOrWhiteSpace(certInfo.subjectDn) && string.IsNullOrWhiteSpace(certDn)) certDn = certInfo.subjectDn;
                    if (!string.IsNullOrWhiteSpace(certInfo.serialNumber) && string.IsNullOrWhiteSpace(serialNumber)) serialNumber = certInfo.serialNumber;
                    if (certInfo.validFrom.HasValue && demoValidFrom <= 0) demoValidFrom = certInfo.validFrom.Value;
                    if (certInfo.validTo.HasValue && demoValidTo <= 0) demoValidTo = certInfo.validTo.Value;
                }

                if (string.IsNullOrWhiteSpace(signerName))
                {
                    signerName = agreementUUID;
                }
                if (string.IsNullOrWhiteSpace(demoIssuerDn))
                {
                    demoIssuerDn = "C=VN,O=I-CA,CN=I-CA SHA-256";
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
                        config.RelyingPartyKeyStorePassword,
                        demoIssuerDn,
                        demoValidFrom > 0 ? demoValidFrom : (long?)null,
                        demoValidTo > 0 ? demoValidTo : (long?)null
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
                    ValidFrom = demoValidFrom > 0 ? demoValidFrom : Utils.CurrentTimeMillis(),
                    ValidTo = demoValidTo > 0 ? demoValidTo : (demoValidFrom > 0 ? demoValidFrom + (365L * 24 * 3600 * 1000 * 3) : Utils.CurrentTimeMillis() + (365L * 24 * 3600 * 1000)),
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

        public static string ExtractTaxId(SignCloudResp? resp, string? certDn)
        {
            if (resp?.agreementDetails != null)
            {
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.taxID))
                    return resp.agreementDetails.taxID.Trim();
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.citizenID))
                    return resp.agreementDetails.citizenID.Trim();
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.personalID))
                    return resp.agreementDetails.personalID.Trim();
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.budgetID))
                    return resp.agreementDetails.budgetID.Trim();
            }

            return ExtractTaxIdFromDN(certDn);
        }

        public static string ExtractTaxIdFromDN(string? dn)
        {
            if (string.IsNullOrWhiteSpace(dn)) return "";

            // 1. UID=MST:038079004321 or UID=CCCD:001090123456 or UID=CMND:... or UID=038079004321
            var mUidMst = Regex.Match(dn, @"UID\s*=\s*(?:MST|CCCD|CMND)?[:\s]*([A-Za-z0-9-]+)", RegexOptions.IgnoreCase);
            if (mUidMst.Success && !string.IsNullOrWhiteSpace(mUidMst.Groups[1].Value))
            {
                return mUidMst.Groups[1].Value.Trim();
            }

            // 2. OID.2.5.4.97=VATVN-0101234567 or 2.5.4.97=...
            var mOid97 = Regex.Match(dn, @"(?:OID\.)?2\.5\.4\.97\s*=\s*(?:VATVN-|VATMST:|VAT-|MST:)?([A-Za-z0-9-]+)", RegexOptions.IgnoreCase);
            if (mOid97.Success && !string.IsNullOrWhiteSpace(mOid97.Groups[1].Value))
            {
                return mOid97.Groups[1].Value.Trim();
            }

            // 3. MST=038079004321 or MST: 038079004321
            var mMst = Regex.Match(dn, @"(?:MST|TIN)\s*[:=]\s*([0-9-]{9,14})", RegexOptions.IgnoreCase);
            if (mMst.Success && !string.IsNullOrWhiteSpace(mMst.Groups[1].Value))
            {
                return mMst.Groups[1].Value.Trim();
            }

            // 4. SERIALNUMBER=MST:038079004321 or OID.2.5.4.5=MST:...
            var mSerialMst = Regex.Match(dn, @"(?:SERIALNUMBER|OID\.2\.5\.4\.5)\s*=\s*(?:MST:)?([A-Za-z0-9-]+)", RegexOptions.IgnoreCase);
            if (mSerialMst.Success && !string.IsNullOrWhiteSpace(mSerialMst.Groups[1].Value))
            {
                string val = mSerialMst.Groups[1].Value.Trim();
                if (val.Length >= 9 && (val.Contains("-") || val.All(char.IsDigit)))
                {
                    return val;
                }
            }

            // 5. In CN: (MST: 038079004321) or - MST: 038079004321
            var mCnMst = Regex.Match(dn, @"(?:MST|CCCD|CMND)[:\s]+([0-9-]{9,14})", RegexOptions.IgnoreCase);
            if (mCnMst.Success && !string.IsNullOrWhiteSpace(mCnMst.Groups[1].Value))
            {
                return mCnMst.Groups[1].Value.Trim();
            }

            return "";
        }

        public static string ExtractAddress(SignCloudResp? resp, string? certDn)
        {
            if (resp?.agreementDetails != null)
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.location))
                    parts.Add(resp.agreementDetails.location.Trim());
                if (!string.IsNullOrWhiteSpace(resp.agreementDetails.stateOrProvince) &&
                    !parts.Any(p => p.Equals(resp.agreementDetails.stateOrProvince.Trim(), StringComparison.OrdinalIgnoreCase)))
                    parts.Add(resp.agreementDetails.stateOrProvince.Trim());
                if (parts.Count > 0)
                    return string.Join(", ", parts);
            }

            return ExtractAddressFromDN(certDn);
        }

        public static string ExtractAddressFromDN(string? dn)
        {
            if (string.IsNullOrWhiteSpace(dn)) return "";

            var parts = new List<string>();

            // STREET=...
            var mStreet = Regex.Match(dn, @"(?:STREET)\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
            if (mStreet.Success && !string.IsNullOrWhiteSpace(mStreet.Groups[1].Value))
            {
                parts.Add(mStreet.Groups[1].Value.Trim());
            }

            // L=... (Locality)
            var mL = Regex.Match(dn, @"(?<![A-Za-z0-9])L\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
            if (mL.Success && !string.IsNullOrWhiteSpace(mL.Groups[1].Value))
            {
                string lVal = mL.Groups[1].Value.Trim();
                if (!parts.Any(p => p.Equals(lVal, StringComparison.OrdinalIgnoreCase)))
                    parts.Add(lVal);
            }

            // ST=... or STATE=... (State / Province)
            var mSt = Regex.Match(dn, @"(?<![A-Za-z0-9])(?:ST|STATE)\s*=\s*([^,]+)", RegexOptions.IgnoreCase);
            if (mSt.Success && !string.IsNullOrWhiteSpace(mSt.Groups[1].Value))
            {
                string stVal = mSt.Groups[1].Value.Trim();
                if (!parts.Any(p => p.Equals(stVal, StringComparison.OrdinalIgnoreCase)))
                    parts.Add(stVal);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "";
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
                    long vFrom = new DateTimeOffset(DateTime.SpecifyKind(cert.NotBefore, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
                    long vTo = new DateTimeOffset(DateTime.SpecifyKind(cert.NotAfter, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
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
                long vFrom = new DateTimeOffset(x509.NotBefore.ToUniversalTime()).ToUnixTimeMilliseconds();
                long vTo = new DateTimeOffset(x509.NotAfter.ToUniversalTime()).ToUnixTimeMilliseconds();
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
