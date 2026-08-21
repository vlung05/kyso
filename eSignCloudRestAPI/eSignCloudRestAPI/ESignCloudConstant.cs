using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eSignCloudRestAPI
{
    class ESignCloudConstant
    {
        public static int AUTHORISATION_METHOD_SMS = 1;
        public static int AUTHORISATION_METHOD_EMAIL = 2;
        public static int AUTHORISATION_METHOD_MOBILE = 3;
        public static int AUTHORISATION_METHOD_PASSCODE = 4;
        public static int AUTHORISATION_METHOD_UAF = 5;

        public static int ASYNCHRONOUS_CLIENTSERVER = 1;
        public static int ASYNCHRONOUS_SERVERSERVER = 2;
        public static int SYNCHRONOUS = 3;

        public static String MIMETYPE_PDF = "application/pdf";
        public static String MIMETYPE_XML = "application/xml";
        public static String MIMETYPE_XHTML_XML = "application/xhtml+xml";

        public static String MIMETYPE_BINARY_WORD = "application/msword";
        public static String MIMETYPE_OPENXML_WORD = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        public static String MIMETYPE_BINARY_POWERPOINT = "application/vnd.ms-powerpoint";
        public static String MIMETYPE_OPENXML_POWERPOINT = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        public static String MIMETYPE_BINARY_EXCEL = "application/vnd.ms-excel";
        public static String MIMETYPE_OPENXML_EXCEL = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        public static String MIMETYPE_MSVISIO = "application/vnd.visio";

        public static String MIMETYPE_SHA1 = "application/sha1-binary";
        public static String MIMETYPE_SHA256 = "application/sha256-binary";
        public static String MIMETYPE_SHA384 = "application/sha384-binary";
        public static String MIMETYPE_SHA512 = "application/sha512-binary";
    }
}
