using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace EUM.Http
{
    class Auth
    {
        public static Response login()
        {
            Response resultat = null;
            string salt = getSalt();
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("bor",Global.Global.ID),
                new KeyValuePair<string,string>("serv",Global.Global.SERVICE),
                new KeyValuePair<string,string>("sign",getSign(salt)),
                new KeyValuePair<string,string>("salt", salt),
            };
            try
            {
                HttpContent q = new FormUrlEncodedContent(queries);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_AUTH, q).Result;
                    HttpContent content = response.Content;
                    string reponse = content.ReadAsStringAsync().Result;
                    resultat = JsonConvert.DeserializeObject<Response>(reponse);
                }
            }
            catch (Exception excep)
            {
            }


            return resultat;
        }

        public static Response logout(Models.Transaction transaction)
        {
            Response resultat = null;
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("access_token",transaction.access_token),
                new KeyValuePair<string,string>("transaction_id",transaction.transactionId.ToString()),

            };
            try
            {
                HttpContent q = new FormUrlEncodedContent(queries);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_LOGOUT, q).Result;
                    HttpContent content = response.Content;
                    string reponse = content.ReadAsStringAsync().Result;
                    resultat = JsonConvert.DeserializeObject<Response>(reponse);
                }
            }
            catch (Exception excep)
            {
            }


            return resultat;
        }
        private static string getSalt()
        {
            return (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();
        }

        private static string getSign(string salt)
        {
            return Securities.Cryptography.getHmacSha1(Global.Global.TOKEN + salt, Global.Global.KEY);
        }
    }
}
