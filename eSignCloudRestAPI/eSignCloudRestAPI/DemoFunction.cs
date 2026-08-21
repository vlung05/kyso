using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace eSignCloudRestAPI
{
    class DemoFunction
    {
        public static string FUNCTION_PREPAREFILEFORSIGNCLOUD = "prepareFileForSignCloud";
        public static string FUNCTION_CHANGEPASSCODEFORSIGNCLOUD = "changePasscodeForSignCloud";
        public static string FUNCTION_FORGETPASSCODEFORSIGNCLOUD = "forgetPasscodeForSignCloud";

        public static string REST_URL = "https://prd-rssp.mobile-id.vn/eSignCloud/restapi/";
        public static string relyingParty = "RSSP";
        public static string relyingPartyUser = "rsspdemo";
        public static string relyingPartyPassword = "12345678";
        public static string relyingPartySignature = "f3eL/n2q5rLn3SdzGfvl1V4MzgPqM68M4TDVqF2fRHarKFQBVQnJU36DPtufu3ofyGVrsq9OgYh3Nujrx7/CUCiKd8I1Qms1y946jEo6wi55ietUQ6vW6/riMwG0blknbb7Wj5tP4SDe1upNydwetgwvaNEKEfv6kubvNqJVkYCo+bFr2rcWV/u1s+i3L1wv4hRIpLZx0Je5IGurGgf2XkGWVhD6x8/AXyy/qmrZ3IzHnFaiWOuy2Dv+NzVLSR0NPU+Zr3btTYMa/ZUa1YYJjrs6c1XLiiwLMJURac/C5j6i5VSRfTQDSHUkIOfTDtN6oRVLZ5ewQ0aQc6tW/FuM2w==";
        public static string relyingPartyKeyStore = "file\\rssp.p12";
        public static string relyingPartyKeyStorePassword = "12345678";
        public static string CERTIFICATEPROFILE = "PERS.1D";
        public static string FILE_DIRECTORY = "file\\";

        public static void printUsage()
        {
            Console.WriteLine("Welcome to eSignCloud Service");
            Console.WriteLine("There are functions we support");
            Console.WriteLine("1. prepareFileForSignCloud");
            Console.WriteLine("2. sign XML");
            Console.WriteLine("3. changePasscodeForSignCloud");
            Console.WriteLine("4. forgetPasscodeForSignCloud");
            Console.WriteLine("5. Exit");
        }

        public static string prepareFileForSignCloud(
            string agreementUUID,
            SignCloudMetaData signCloudMetaData,
            string passCode,
            string mimeType,
            string fileName,
            byte[] fileData)
        {
            string timestamp = Utils.CurrentTimeMillis().ToString();
            string data2sign = DemoFunction.relyingPartyUser + DemoFunction.relyingPartyPassword + DemoFunction.relyingPartySignature + timestamp;
            string pkcs1Signature = Utils.getPKCS1Signature(data2sign, DemoFunction.relyingPartyKeyStore, DemoFunction.relyingPartyKeyStorePassword);

            SignCloudReq signCloudReq = new SignCloudReq();
            signCloudReq.relyingParty = DemoFunction.relyingParty;
            signCloudReq.agreementUUID = agreementUUID;
            signCloudReq.authorizeMethod = ESignCloudConstant.AUTHORISATION_METHOD_PASSCODE;
            signCloudReq.authorizeCode = passCode;
            signCloudReq.messagingMode = ESignCloudConstant.SYNCHRONOUS;
            signCloudReq.certificateRequired = true;
            if (mimeType == ESignCloudConstant.MIMETYPE_PDF)
            {
                signCloudReq.signingFileData = fileData;
            }
            else if (mimeType == ESignCloudConstant.MIMETYPE_XML)
            {
                char[] charXml = Encoding.UTF8.GetString(fileData).ToCharArray();
                string stringXML = new string(charXml);
                signCloudReq.xmlDocument = stringXML;
            }
            signCloudReq.mimeType = mimeType;
            signCloudReq.signingFileName = fileName;
            signCloudReq.signCloudMetaData = signCloudMetaData;

            CredentialData credentialData = new CredentialData();
            credentialData.username = DemoFunction.relyingPartyUser;
            credentialData.password = DemoFunction.relyingPartyPassword;
            credentialData.timestamp = timestamp;
            credentialData.signature = DemoFunction.relyingPartySignature;
            credentialData.pkcs1Signature = pkcs1Signature;
            signCloudReq.credentialData = credentialData;

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = Int32.MaxValue;
            string jsonReq = javaScriptSerializer.Serialize(signCloudReq);
            string jsonResp = DataTypes.sendPost(REST_URL + FUNCTION_PREPAREFILEFORSIGNCLOUD, jsonReq);

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Converters = new[] { new ByteArrayConverter() }
            };
            SignCloudResp signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp, jsonSerializerSettings);
            if (signCloudResp.responseCode == 0 || signCloudResp.responseCode == 1018)
            {
                if (signCloudResp.signedFileData != null)
                {
                    if (signCloudResp.mimeType == ESignCloudConstant.MIMETYPE_XML)
                    {
                        string file = Program.FILE_DIRECTORY + Program.FILE_XML_SIGNED;
                        Console.WriteLine("Saved in " + file);
                        Console.WriteLine("MimeType: " + signCloudResp.mimeType);
                        File.WriteAllBytes(file, signCloudResp.signedFileData);
                    }
                    else if (signCloudResp.mimeType == ESignCloudConstant.MIMETYPE_PDF)
                    {
                        string file = Program.FILE_DIRECTORY + Program.FILE_PDF_SIGNED;
                        Console.WriteLine("Saved in " + file);
                        Console.WriteLine("MimeType: " + signCloudResp.mimeType);
                        File.WriteAllBytes(file, signCloudResp.signedFileData);
                    }
                }
                else
                {
                    Console.WriteLine("Oops! no data received");
                }
            }
            else if (signCloudResp.responseCode == 1007)
            {
                Console.WriteLine("ResponseCode: " + signCloudResp.responseCode);
                Console.WriteLine("ResponseMessage: " + signCloudResp.responseMessage);
                Console.WriteLine("BillCode: " + signCloudResp.billCode);
                Console.WriteLine("OTP: " + signCloudResp.authorizeCredential);
            }
            else
            {
                throw new Exception("Error while calling prepareFileForSignCloud");
            }

            return jsonResp;
        }


        public static void changePasscodeForSignCloud(string agreementUUID, string currentPassCode, string newPassCode)
        {
            string timestamp = Utils.CurrentTimeMillis().ToString();
            string data2sign = relyingPartyUser + relyingPartyPassword + relyingPartySignature + timestamp;
            string pkcs1Signature = Utils.getPKCS1Signature(data2sign, relyingPartyKeyStore, relyingPartyKeyStorePassword);

            SignCloudReq signCloudReq = new SignCloudReq();
            signCloudReq.relyingParty = relyingParty;
            signCloudReq.agreementUUID = agreementUUID;
            signCloudReq.currentPasscode = currentPassCode;
            signCloudReq.newPasscode = newPassCode;

            CredentialData credentialData = new CredentialData();
            credentialData.username = relyingPartyUser;
            credentialData.password = relyingPartyPassword;
            credentialData.timestamp = timestamp;
            credentialData.signature = relyingPartySignature;
            credentialData.pkcs1Signature = pkcs1Signature;
            signCloudReq.credentialData = credentialData;

            string jsonReq = new JavaScriptSerializer().Serialize(signCloudReq);
            string jsonResp = DataTypes.sendPost(REST_URL + FUNCTION_CHANGEPASSCODEFORSIGNCLOUD, jsonReq);

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Converters = new[] { new ByteArrayConverter() }
            };
            SignCloudResp signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp, jsonSerializerSettings);
            if (signCloudResp.responseCode == 0)
            {
                Console.WriteLine("Response Code: " + signCloudResp.responseCode);
                Console.WriteLine("Response Message: " + signCloudResp.responseMessage);
            }
            else
            {
                throw new Exception("Error while calling changePasscodeForSignCloud");
            }
        }

        public static string forgetPasscodeForSignCloud(string agreementUUID)
        {
            string timestamp = Utils.CurrentTimeMillis().ToString();
            string data2sign = DemoFunction.relyingPartyUser + DemoFunction.relyingPartyPassword + DemoFunction.relyingPartySignature + timestamp;
            string pkcs1Signature = Utils.getPKCS1Signature(data2sign, DemoFunction.relyingPartyKeyStore, DemoFunction.relyingPartyKeyStorePassword);

            SignCloudReq signCloudReq = new SignCloudReq();
            signCloudReq.relyingParty = DemoFunction.relyingParty;
            signCloudReq.agreementUUID = agreementUUID;

            CredentialData credentialData = new CredentialData();
            credentialData.username = DemoFunction.relyingPartyUser;
            credentialData.password = DemoFunction.relyingPartyPassword;
            credentialData.timestamp = timestamp;
            credentialData.signature = DemoFunction.relyingPartySignature;
            credentialData.pkcs1Signature = pkcs1Signature;
            signCloudReq.credentialData = credentialData;

            JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
            javaScriptSerializer.MaxJsonLength = Int32.MaxValue;
            string jsonReq = javaScriptSerializer.Serialize(signCloudReq);
            string jsonResp = DataTypes.sendPost(REST_URL + FUNCTION_FORGETPASSCODEFORSIGNCLOUD, jsonReq);

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                Converters = new[] { new ByteArrayConverter() }
            };
            SignCloudResp signCloudResp = JsonConvert.DeserializeObject<SignCloudResp>(jsonResp, jsonSerializerSettings);
            if (signCloudResp.responseCode == 0)
            {
                Console.WriteLine("Response Code: " + signCloudResp.responseCode);
                Console.WriteLine("Response Message: " + signCloudResp.responseMessage);
            }
            else
            {
                throw new Exception("Error while calling changePasscodeForSignCloud");
            }
            return jsonResp;
        }

    }
}
