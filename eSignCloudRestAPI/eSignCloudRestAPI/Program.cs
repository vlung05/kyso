using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSignCloudRestAPI
{
    class Program
    {
        public static string FILE_DIRECTORY = "file\\";
        public static string FILE_BACKGROUND = "signature_img.png";
        public static string FILE_PDF = "sample.pdf";
        public static string FILE_PDF_SIGNED = "sample.signed.pdf";
        public static string FILE_XML = "sample_xml_B.xml";
        public static string FILE_XML_SIGNED = "sample_xml_B.signed.xml";

        static void Main(string[] args)
        {
            string agreementUUID = "562449E1-090C-4573-B978-76A740B9F50A";
            string passCode = "12345678";
            try
            {
                DemoFunction.printUsage();
                int resultCode = 0;
                do
                {
                    Console.Write("Enter the function: ");
                    int functionNumber = Convert.ToInt16(Console.ReadLine());
                    switch (functionNumber)
                    {
                        case 1:
                            SignCloudMetaData signCloudMetaData = new SignCloudMetaData();
                            Dictionary<string, string> singletonSigning = new Dictionary<string, string>();
                            singletonSigning["PAGENO"] = "1";
                            singletonSigning["POSITIONIDENTIFIER"] = "CHỮ KÝ ĐIỆN TỬ";
                            singletonSigning["RECTANGLEOFFSET"] = "-30,-100";
                            singletonSigning["RECTANGLESIZE"] = "170,70";
                            singletonSigning["VISIBLESIGNATURE"] = "True";
                            singletonSigning["VISUALSTATUS"] = "False";
                            singletonSigning["SHOWSIGNERINFO"] = "True";
                            singletonSigning["SIGNERINFOPREFIX"] = "Ký bởi:";
                            singletonSigning["SHOWDATETIME"] = "True";
                            singletonSigning["DATETIMEPREFIX"] = "Ký ngày:";
                            singletonSigning["SHOWREASON"] = "True";
                            singletonSigning["SIGNREASONPREFIX"] = "Lý do:";
                            singletonSigning["SIGNREASON"] = "Tôi đồng ý";
                            singletonSigning["SHOWLOCATION"] = "True";
                            singletonSigning["LOCATION"] = "Hồ Chí Minh";
                            singletonSigning["LOCATIONPREFIX"] = "Nơi ký:";
                            singletonSigning["TEXTCOLOR"] = "black";
                            singletonSigning["IMAGEANDTEXT"] = "False";
                            singletonSigning["TEXTDIRECTION"] = "LEFTTORIGHT";
                            signCloudMetaData.singletonSigning = singletonSigning;

                            byte[] fileData_pdf = File.ReadAllBytes(Program.FILE_DIRECTORY + FILE_PDF);
                            string fileName = "sample.pdf";
                            string mimeTYPE = ESignCloudConstant.MIMETYPE_PDF;

                            DemoFunction.prepareFileForSignCloud(agreementUUID, signCloudMetaData, passCode, mimeTYPE, fileName, fileData_pdf);
                            break;

                        case 2:
                            SignCloudMetaData signCloudMetaData_case2 = new SignCloudMetaData();

                            Dictionary<string, string> singletonSigning_c2 = new Dictionary<string, string>();
                            singletonSigning_c2["NODETOBESIGNED"] = "id-836a41b0-2c34-422f-ab5b-646963e41ac9";
                            singletonSigning_c2["SIGNATUREFORMAT"] = "TAX-211120";
                            singletonSigning_c2["SIGNATURELOCATION"] = "DKyThue";
                            singletonSigning_c2["DATETIMEFORMAT"] = "yyyy-MM-dd HH:mm:ss";
                            signCloudMetaData_case2.singletonSigning = singletonSigning_c2;

                            int AuthorizeMethod = ESignCloudConstant.AUTHORISATION_METHOD_PASSCODE;
                            int MessagingMode = ESignCloudConstant.SYNCHRONOUS;
                            string MimeTYPE = ESignCloudConstant.MIMETYPE_XML;
                            byte[] FileData = File.ReadAllBytes(Program.FILE_DIRECTORY + FILE_XML);
                            string FileName = "sample_xml_B.xml";

                            DemoFunction.prepareFileForSignCloud(agreementUUID, signCloudMetaData_case2, passCode, MimeTYPE, FileName, FileData);
                            break;

                        case 3:
                            Console.Write("Current PassCode: ");
                            string currentPassCode = Console.ReadLine();
                            Console.Write("New PassCode: ");
                            string newPassCode = Console.ReadLine();
                            DemoFunction.changePasscodeForSignCloud(agreementUUID, currentPassCode, newPassCode);
                            break;

                        case 4:
                            DemoFunction.forgetPasscodeForSignCloud(agreementUUID);
                            break;

                        default:
                            resultCode = -1;
                            break;
                    }
                } while (resultCode == 0);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                throw e;
            }
        }
    }
}
