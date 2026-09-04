using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace eSignCloudWeb.Models
{
    public static class ESignCloudConstant
    {
        public const int AUTHORISATION_METHOD_SMS = 1;
        public const int AUTHORISATION_METHOD_EMAIL = 2;
        public const int AUTHORISATION_METHOD_MOBILE = 3;
        public const int AUTHORISATION_METHOD_PASSCODE = 4;
        public const int AUTHORISATION_METHOD_UAF = 5;

        public const int ASYNCHRONOUS_CLIENTSERVER = 1;
        public const int ASYNCHRONOUS_SERVERSERVER = 2;
        public const int SYNCHRONOUS = 3;

        public const string MIMETYPE_PDF = "application/pdf";
        public const string MIMETYPE_XML = "application/xml";
        public const string MIMETYPE_XHTML_XML = "application/xhtml+xml";

        public const string MIMETYPE_BINARY_WORD = "application/msword";
        public const string MIMETYPE_OPENXML_WORD = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public const string MIMETYPE_BINARY_POWERPOINT = "application/vnd.ms-powerpoint";
        public const string MIMETYPE_OPENXML_POWERPOINT = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        public const string MIMETYPE_BINARY_EXCEL = "application/vnd.ms-excel";
        public const string MIMETYPE_OPENXML_EXCEL = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public const string MIMETYPE_MSVISIO = "application/vnd.visio";

        public const string MIMETYPE_SHA1 = "application/sha1-binary";
        public const string MIMETYPE_SHA256 = "application/sha256-binary";
        public const string MIMETYPE_SHA384 = "application/sha384-binary";
        public const string MIMETYPE_SHA512 = "application/sha512-binary";
    }

    public class SignCloudReq
    {
        [JsonProperty("relyingParty", NullValueHandling = NullValueHandling.Ignore)]
        public string? relyingParty { get; set; }

        [JsonProperty("relyingPartyBillCode", NullValueHandling = NullValueHandling.Ignore)]
        public string? relyingPartyBillCode { get; set; }

        [JsonProperty("agreementUUID", NullValueHandling = NullValueHandling.Ignore)]
        public string? agreementUUID { get; set; }

        [JsonProperty("sharedAgreementUUID", NullValueHandling = NullValueHandling.Ignore)]
        public string? sharedAgreementUUID { get; set; }

        [JsonProperty("sharedRelyingParty", NullValueHandling = NullValueHandling.Ignore)]
        public string? sharedRelyingParty { get; set; }

        [JsonProperty("mobileNo", NullValueHandling = NullValueHandling.Ignore)]
        public string? mobileNo { get; set; }

        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string? email { get; set; }

        [JsonProperty("certificateProfile", NullValueHandling = NullValueHandling.Ignore)]
        public string? certificateProfile { get; set; }

        [JsonProperty("signingFileUUID", NullValueHandling = NullValueHandling.Ignore)]
        public string? signingFileUUID { get; set; }

        [JsonProperty("signingFileData", NullValueHandling = NullValueHandling.Ignore)]
        public byte[]? signingFileData { get; set; }

        [JsonProperty("signingFileName", NullValueHandling = NullValueHandling.Ignore)]
        public string? signingFileName { get; set; }

        [JsonProperty("mimeType", NullValueHandling = NullValueHandling.Ignore)]
        public string? mimeType { get; set; }

        [JsonProperty("notificationTemplate", NullValueHandling = NullValueHandling.Ignore)]
        public string? notificationTemplate { get; set; }

        [JsonProperty("notificationSubject", NullValueHandling = NullValueHandling.Ignore)]
        public string? notificationSubject { get; set; }

        [JsonProperty("timestampEnabled")]
        public bool timestampEnabled { get; set; }

        [JsonProperty("ltvEnabled")]
        public bool ltvEnabled { get; set; }

        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore)]
        public string? language { get; set; }

        [JsonProperty("authorizeCode", NullValueHandling = NullValueHandling.Ignore)]
        public string? authorizeCode { get; set; }

        [JsonProperty("postbackEnabled")]
        public bool postbackEnabled { get; set; }

        [JsonProperty("noPadding")]
        public bool noPadding { get; set; }

        [JsonProperty("authorizeMethod")]
        public int authorizeMethod { get; set; }

        [JsonProperty("uploadingFileData", NullValueHandling = NullValueHandling.Ignore)]
        public byte[]? uploadingFileData { get; set; }

        [JsonProperty("downloadingFileUUID", NullValueHandling = NullValueHandling.Ignore)]
        public string? downloadingFileUUID { get; set; }

        [JsonProperty("currentPasscode", NullValueHandling = NullValueHandling.Ignore)]
        public string? currentPasscode { get; set; }

        [JsonProperty("newPasscode", NullValueHandling = NullValueHandling.Ignore)]
        public string? newPasscode { get; set; }

        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        public string? hash { get; set; }

        [JsonProperty("hashAlgorithm", NullValueHandling = NullValueHandling.Ignore)]
        public string? hashAlgorithm { get; set; }

        [JsonProperty("encryption", NullValueHandling = NullValueHandling.Ignore)]
        public string? encryption { get; set; }

        [JsonProperty("billCode", NullValueHandling = NullValueHandling.Ignore)]
        public string? billCode { get; set; }

        [JsonProperty("messagingMode")]
        public int messagingMode { get; set; }

        [JsonProperty("sharedMode")]
        public int sharedMode { get; set; }

        [JsonProperty("xslTemplateUUID", NullValueHandling = NullValueHandling.Ignore)]
        public string? xslTemplateUUID { get; set; }

        [JsonProperty("xslTemplate", NullValueHandling = NullValueHandling.Ignore)]
        public string? xslTemplate { get; set; }

        [JsonProperty("xmlDocument", NullValueHandling = NullValueHandling.Ignore)]
        public string? xmlDocument { get; set; }

        [JsonProperty("p2pEnabled")]
        public bool p2pEnabled { get; set; }

        [JsonProperty("csrRequired")]
        public bool csrRequired { get; set; }

        [JsonProperty("certificateRequired")]
        public bool certificateRequired { get; set; }

        [JsonProperty("keepOldKeysEnabled")]
        public bool keepOldKeysEnabled { get; set; }

        [JsonProperty("revokeOldCertificateEnabled")]
        public bool revokeOldCertificateEnabled { get; set; }

        [JsonProperty("certificate", NullValueHandling = NullValueHandling.Ignore)]
        public string? certificate { get; set; }

        [JsonProperty("multipleSigningFileData", NullValueHandling = NullValueHandling.Ignore)]
        public List<MultipleSigningFileData>? multipleSigningFileData { get; set; }

        [JsonProperty("signCloudMetaData", NullValueHandling = NullValueHandling.Ignore)]
        public SignCloudMetaData? signCloudMetaData { get; set; }

        [JsonProperty("agreementDetails", NullValueHandling = NullValueHandling.Ignore)]
        public AgreementDetails? agreementDetails { get; set; }

        [JsonProperty("credentialData", NullValueHandling = NullValueHandling.Ignore)]
        public CredentialData? credentialData { get; set; }
    }

    public class CredentialData
    {
        [JsonProperty("username")]
        public string username { get; set; } = "";

        [JsonProperty("password")]
        public string password { get; set; } = "";

        [JsonProperty("signature")]
        public string signature { get; set; } = "";

        [JsonProperty("pkcs1Signature")]
        public string pkcs1Signature { get; set; } = "";

        [JsonProperty("timestamp")]
        public string timestamp { get; set; } = "";
    }

    public class AgreementDetails
    {
        public string? personalName { get; set; }
        public string? organization { get; set; }
        public string? organizationUnit { get; set; }
        public string? title { get; set; }
        public string? email { get; set; }
        public string? telephoneNumber { get; set; }
        public string? location { get; set; }
        public string? stateOrProvince { get; set; }
        public string? country { get; set; }
        public string? personalID { get; set; }
        public string? passportID { get; set; }
        public string? citizenID { get; set; }
        public string? taxID { get; set; }
        public string? budgetID { get; set; }

        public byte[]? applicationForm { get; set; }
        public byte[]? requestForm { get; set; }
        public byte[]? authorizeLetter { get; set; }
        public byte[]? photoIDCard { get; set; }
        public byte[]? photoFrontSideIDCard { get; set; }
        public byte[]? photoBackSideIDCard { get; set; }
        public byte[]? photoActivityDeclaration { get; set; }
        public byte[]? photoAuthorizeDelegate { get; set; }
    }

    public class SignCloudMetaData
    {
        [JsonProperty("singletonSigning", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? singletonSigning { get; set; }

        [JsonProperty("counterSigning", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string>? counterSigning { get; set; }
    }

    public class MultipleSigningFileData
    {
        public string? hash { get; set; }
        public byte[]? signingFileData { get; set; }
        public string? signingFileName { get; set; }
        public string? mimeType { get; set; }
        public string? xslTemplate { get; set; }
        public string? xmlDocument { get; set; }
        public SignCloudMetaData? signCloudMetaData { get; set; }
    }

    public class SignCloudResp
    {
        [JsonProperty("responseCode")]
        public int responseCode { get; set; }

        [JsonProperty("responseMessage")]
        public string? responseMessage { get; set; }

        [JsonProperty("billCode")]
        public string? billCode { get; set; }

        [JsonProperty("email")]
        public string? email { get; set; }

        [JsonProperty("mobileNo")]
        public string? mobileNo { get; set; }

        [JsonProperty("timestamp")]
        public long timestamp { get; set; }

        [JsonProperty("logInstance")]
        public int logInstance { get; set; }

        [JsonProperty("notificationMessage")]
        public string? notificationMessage { get; set; }

        [JsonProperty("remainingCounter")]
        public int remainingCounter { get; set; }

        [JsonProperty("signedFileData")]
        public byte[]? signedFileData { get; set; }

        [JsonProperty("signedFileName")]
        public string? signedFileName { get; set; }

        [JsonProperty("authorizeCredential")]
        public string? authorizeCredential { get; set; }

        [JsonProperty("signedFileUUID")]
        public string? signedFileUUID { get; set; }

        [JsonProperty("mimeType")]
        public string? mimeType { get; set; }

        [JsonProperty("certificateDN")]
        public string? certificateDN { get; set; }

        [JsonProperty("certificateSerialNumber")]
        public string? certificateSerialNumber { get; set; }

        [JsonProperty("certificateThumbprint")]
        public string? certificateThumbprint { get; set; }

        [JsonProperty("validFrom")]
        public long validFrom { get; set; }

        [JsonProperty("validTo")]
        public long validTo { get; set; }

        [JsonProperty("issuerDN")]
        public string? issuerDN { get; set; }

        [JsonProperty("uploadedFileUUID")]
        public string? uploadedFileUUID { get; set; }

        [JsonProperty("downloadedFileUUID")]
        public string? downloadedFileUUID { get; set; }

        [JsonProperty("downloadedFileData")]
        public byte[]? downloadedFileData { get; set; }

        [JsonProperty("signatureValue")]
        public string? signatureValue { get; set; }

        [JsonProperty("authorizeMethod")]
        public int authorizeMethod { get; set; }

        [JsonProperty("notificationSubject")]
        public string? notificationSubject { get; set; }

        [JsonProperty("dmsMetaData")]
        public string? dmsMetaData { get; set; }

        [JsonProperty("csr")]
        public string? csr { get; set; }

        [JsonProperty("certificate")]
        public string? certificate { get; set; }

        [JsonProperty("certificateStateID")]
        public int certificateStateID { get; set; }

        [JsonProperty("agreementDetails", NullValueHandling = NullValueHandling.Ignore)]
        public AgreementDetails? agreementDetails { get; set; }

        [JsonProperty("multipleSignedFileData")]
        public List<MultipleSignedFileData>? multipleSignedFileData { get; set; }
    }

    public class MultipleSignedFileData
    {
        public byte[]? signedFileData { get; set; }
        public string? mimeType { get; set; }
        public string? signedFileName { get; set; }
        public string? signedFileUUID { get; set; }
        public string? dmsMetaData { get; set; }
        public string? signatureValue { get; set; }
    }

    // Application Configuration Model (Editable from Web Page)
    public class ESignCloudConfig
    {
        // RSSP REST Connection Settings
        public string RestUrl { get; set; } = "https://prd-rssp.mobile-id.vn/eSignCloud/restapi/";
        public string RelyingParty { get; set; } = "RSSP";
        public string RelyingPartyUser { get; set; } = "rsspdemo";
        public string RelyingPartyPassword { get; set; } = "12345678";
        public string RelyingPartySignature { get; set; } = "f3eL/n2q5rLn3SdzGfvl1V4MzgPqM68M4TDVqF2fRHarKFQBVQnJU36DPtufu3ofyGVrsq9OgYh3Nujrx7/CUCiKd8I1Qms1y946jEo6wi55ietUQ6vW6/riMwG0blknbb7Wj5tP4SDe1upNydwetgwvaNEKEfv6kubvNqJVkYCo+bFr2rcWV/u1s+i3L1wv4hRIpLZx0Je5IGurGgf2XkGWVhD6x8/AXyy/qmrZ3IzHnFaiWOuy2Dv+NzVLSR0NPU+Zr3btTYMa/ZUa1YYJjrs6c1XLiiwLMJURac/C5j6i5VSRfTQDSHUkIOfTDtN6oRVLZ5ewQ0aQc6tW/FuM2w==";
        public string RelyingPartyKeyStore { get; set; } = "file/rssp.p12";
        public string RelyingPartyKeyStorePassword { get; set; } = "12345678";
        public string CertificateProfile { get; set; } = "PERS.1D";

        // Default User Info
        public string DefaultAgreementUUID { get; set; } = "";
        public string DefaultPassCode { get; set; } = "";

        // Storage Directory
        public string FileDirectory { get; set; } = "storage/";

        // Admin Security & Access Control
        public string AdminUsername { get; set; } = "admin";
        public string AdminPassword { get; set; } = "admin123";
        public List<string> AdminUids { get; set; } = new List<string>();

        // Demo fallback simulation mode (useful when remote sandbox server is down)
        public bool EnableDemoSimulation { get; set; } = true;

        // Default Metadata for PDF / Document Signing
        public Dictionary<string, string> DefaultMetadata { get; set; } = new Dictionary<string, string>
        {
            { "PAGENO", "1" },
            { "POSITIONIDENTIFIER", "CHỮ KÝ ĐIỆN TỬ" },
            { "RECTANGLEOFFSET", "-30,-100" },
            { "RECTANGLESIZE", "170,70" },
            { "VISIBLESIGNATURE", "True" },
            { "VISUALSTATUS", "False" },
            { "SHOWSIGNERINFO", "True" },
            { "SIGNERINFOPREFIX", "Ký bởi:" },
            { "SHOWDATETIME", "True" },
            { "DATETIMEPREFIX", "Ký ngày:" },
            { "SHOWREASON", "False" },
            { "SIGNREASONPREFIX", "Lý do:" },
            { "SIGNREASON", "" },
            { "SHOWLOCATION", "False" },
            { "LOCATION", "" },
            { "LOCATIONPREFIX", "Nơi ký:" },
            { "TEXTCOLOR", "black" },
            { "IMAGEANDTEXT", "False" },
            { "TEXTDIRECTION", "LEFTTORIGHT" }
        };

        // Advertisement Slides
        public List<SlideItem> Slides { get; set; } = new List<SlideItem>();

        // Danh sách tài khoản UID / Người ký nội bộ do Quản trị viên quản lý
        public List<SignerAccountRecord> Accounts { get; set; } = new List<SignerAccountRecord>();
    }

    public class SignerAccountRecord
    {
        public string AgreementUUID { get; set; } = "";
        public string SignerName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string DefaultPasscode { get; set; } = "";
        public string Status { get; set; } = "Hoạt động";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string? CertificateDN { get; set; }
        public string? CertificateSerialNumber { get; set; }
        public string? IssuerDN { get; set; }
        public long ValidFrom { get; set; }
        public long ValidTo { get; set; }
        public string? Certificate { get; set; }
        public int SignedCount { get; set; } = 0;
    }

    public class SlideItem
    {
        public int Id { get; set; }
        public string Tag { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Gradient { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public string ImageFit { get; set; } = "contain";
        public string LinkUrl { get; set; } = "";
        public string Icon { get; set; } = "";
        public string ButtonText { get; set; } = "";
        public string ButtonLink { get; set; } = "";
    }

    // Document History Record
    public class SignedDocumentRecord
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string AgreementUUID { get; set; } = "";
        public string OriginalFileName { get; set; } = "";
        public string SignedFileName { get; set; } = "";
        public string MimeType { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime SignDate { get; set; } = DateTime.Now;
        public string? BillCode { get; set; }
        public int ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public string? CertificateDN { get; set; }
        public string? CertificateSerialNumber { get; set; }
        public string? IssuerDN { get; set; }
        public long ValidFrom { get; set; }
        public long ValidTo { get; set; }
        public string OriginalFilePath { get; set; } = "";
        public string SignedFilePath { get; set; } = "";
        public string Status { get; set; } = "Thành công";
        public string Reason { get; set; } = "";
        public string Location { get; set; } = "";
    }

    // API Request Models
    public class LoginRequest
    {
        public string Uid { get; set; } = "";
        public string Passcode { get; set; } = "";
    }

    public class ChangePasscodeRequest
    {
        public string Uid { get; set; } = "";
        public string CurrentPasscode { get; set; } = "";
        public string NewPasscode { get; set; } = "";
    }

    public class ForgetPasscodeRequest
    {
        public string Uid { get; set; } = "";
    }
}
