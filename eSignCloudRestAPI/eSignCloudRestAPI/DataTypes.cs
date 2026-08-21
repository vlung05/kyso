using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace eSignCloudRestAPI
{
    class DataTypes
    {
        public static string sendPost(string url, string payload)
        {
            try
            {
                string result;
                string endpointUrl = url;

                ServicePointManager.CheckCertificateRevocationList = false;
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.DefaultConnectionLimit = 9999;

                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(endpointUrl);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(payload);
                }

                HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    result = streamReader.ReadToEnd();
                }
                return result;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }

    // Classes defination
    class SignCloudReq
    {
        public string relyingParty { get; set; }
        public string relyingPartyBillCode { get; set; }
        public string agreementUUID { get; set; }
        public string sharedAgreementUUID { get; set; }
        public string sharedRelyingParty { get; set; }
        public string mobileNo { get; set; }
        public string email { get; set; }
        public string certificateProfile { get; set; }
        public string signingFileUUID { get; set; }
        public byte[] signingFileData { get; set; }
        public string signingFileName { get; set; }
        public string mimeType { get; set; }
        public string notificationTemplate { get; set; }
        public string notificationSubject { get; set; }
        public bool timestampEnabled { get; set; }
        public bool ltvEnabled { get; set; }
        public string language { get; set; }
        public string authorizeCode { get; set; }
        public bool postbackEnabled { get; set; }
        public bool noPadding { get; set; }
        public int authorizeMethod { get; set; }
        public byte[] uploadingFileData { get; set; }
        public string downloadingFileUUID { get; set; }
        public string currentPasscode { get; set; }
        public string newPasscode { get; set; }
        public string hash { get; set; }
        public string hashAlgorithm { get; set; }
        public string encryption { get; set; }
        public string billCode { get; set; }
        public int messagingMode { get; set; }
        public int sharedMode { get; set; }
        public string xslTemplateUUID { get; set; }
        public string xslTemplate { get; set; }
        public string xmlDocument { get; set; }
        public bool p2pEnabled { get; set; }
        public bool csrRequired { get; set; }
        public bool certificateRequired { get; set; }
        public bool keepOldKeysEnabled { get; set; }
        public bool revokeOldCertificateEnabled { get; set; }
        public string certificate { get; set; }


        public List<MultipleSigningFileData> multipleSigningFileData { get; set; }
        public SignCloudMetaData signCloudMetaData { get; set; }
        public AgreementDetails agreementDetails { get; set; }
        public CredentialData credentialData { get; set; }
    }

    class CredentialData
    {
        public string username { get; set; }
        public string password { get; set; }
        public string signature { get; set; }
        public string pkcs1Signature { get; set; }
        public string timestamp { get; set; }
    }

    class AgreementDetails
    {
        public string personalName { get; set; }
        public string organization { get; set; }
        public string organizationUnit { get; set; }
        public string title { get; set; }
        public string email { get; set; }
        public string telephoneNumber { get; set; }
        public string location { get; set; }
        public string stateOrProvince { get; set; }
        public string country { get; set; }
        public string personalID { get; set; }
        public string passportID { get; set; }
        public string citizenID { get; set; }
        public string taxID { get; set; }
        public string budgetID { get; set; }

        public byte[] applicationForm { get; set; }
        public byte[] requestForm { get; set; }
        public byte[] authorizeLetter { get; set; }
        public byte[] photoIDCard { get; set; }
        public byte[] photoFrontSideIDCard { get; set; }
        public byte[] photoBackSideIDCard { get; set; }
        public byte[] photoActivityDeclaration { get; set; }
        public byte[] photoAuthorizeDelegate { get; set; }
    }

    class SignCloudMetaData
    {
        public Dictionary<string, string> singletonSigning { get; set; }
        public Dictionary<string, string> counterSigning { get; set; }
    }

    class MultipleSigningFileData
    {
        public string hash { get; set; }
        public byte[] signingFileData { get; set; }
        public string signingFileName { get; set; }
        public string mimeType { get; set; }
        public string xslTemplate { get; set; }
        public string xmlDocument { get; set; }
        public SignCloudMetaData signCloudMetaData { get; set; }
    }

    class SignCloudResp
    {
        public int responseCode { get; set; }
        public string responseMessage { get; set; }
        public string billCode { get; set; }
        public long timestamp { get; set; }
        public int logInstance { get; set; }
        public string notificationMessage { get; set; }
        public int remainingCounter { get; set; }
        public byte[] signedFileData { get; set; }
        public string signedFileName { get; set; }
        public string authorizeCredential { get; set; }
        public string signedFileUUID { get; set; }
        public string mimeType { get; set; }
        public string certificateDN { get; set; }
        public string certificateSerialNumber { get; set; }
        public string certificateThumbprint { get; set; }
        public long validFrom { get; set; }
        public long validTo { get; set; }
        public string issuerDN { get; set; }
        public string uploadedFileUUID { get; set; }
        public string downloadedFileUUID { get; set; }
        public byte[] downloadedFileData { get; set; }
        public string signatureValue { get; set; }
        public int authorizeMethod { get; set; }
        public string notificationSubject { get; set; }
        public string dmsMetaData { get; set; }
        public string csr { get; set; }
        public string certificate { get; set; }
        public int certificateStateID { get; set; }
        public List<MultipleSignedFileData> multipleSignedFileData { get; set; }
    }

    class MultipleSignedFileData
    {
        public byte[] signedFileData { get; set; }
        public string mimeType { get; set; }
        public string signedFileName { get; set; }
        public string signedFileUUID { get; set; }
        public string dmsMetaData { get; set; }
        public string signatureValue { get; set; }
    }
}
