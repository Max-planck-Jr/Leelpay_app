using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MoMo.Http
{
    class Request
    {
        public static Response checkOp(Models.Transaction transaction)
        {
            Response resultat = null;
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("access_token",transaction.access_token),
                new KeyValuePair<string,string>("transaction_id",transaction.transactionId.ToString()),
                new KeyValuePair<string,string>("phone","237"+transaction.phone),

            };
            try
            {
                HttpContent q = new FormUrlEncodedContent(queries);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_CHECK_ACCOUNT, q).Result;
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

        public static Response enterInfo(Models.Transaction transaction)
        {
            Response resultat = null;
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("access_token",transaction.access_token),
                new KeyValuePair<string,string>("transaction_id",transaction.transactionId.ToString()),
                new KeyValuePair<string,string>("name",transaction.sender),
                new KeyValuePair<string,string>("amount",transaction.amount),

            };
            try
            {
                HttpContent q = new FormUrlEncodedContent(queries);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_ENTER_INFO, q).Result;
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

        public static Response validInsert(Models.Transaction transaction)
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
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_AMOUNT_INSERT, q).Result;
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

        public static Response terminate(Models.Transaction transaction)
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
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_TERMINATE, q).Result;
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
    }
}
