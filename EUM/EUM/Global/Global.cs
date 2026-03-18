using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EUM.Global
{
    class Global
    {
        public static string ID = "6";
        public static string SERVICE = "21";
        public static string TOKEN = "RXGngryjcZL0BuLhY5jklwWtKAbzieIw";
        public static string KEY = "qiMaSxxySjoAZ0qn3HMkGv09iSSuVDxy";
        public static int max = 0;

        public static string URL = "http://137.74.160.144/api/v1/";
        public static string URL_AUTH = URL + "auth";
        public static string URL_LOGOUT = URL + "cancel";

        private static string BASE_URL = URL + "eum/";
        public static string URL_CHECK_ACCOUNT = BASE_URL + "check_account";

        public static string URL_ENTER_INFO = BASE_URL + "enter_info";

        public static string URL_AMOUNT_INSERT = BASE_URL + "valid_amount";

        public static string URL_TERMINATE = BASE_URL + "terminate";

        public static int SERVICE_NOT_ACTIVATE = 8;
        public static int OK = 0;

        public static int TIME_TO_WARMING = 240000;

        public static int TIME_TO_STOP = 60000;

        public static Int32 montant = 0;

        public static string ComPort = "COM5";

        public static byte SSPAddress = 0;
    }
}
