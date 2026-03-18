using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Arrete.Http
{
    class Request
    {
        public static Response auth(User user)
        {
            Response resultat = null;
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("login",user.login),
                new KeyValuePair<string,string>("password",user.password),
                new KeyValuePair<string,string>("key",user.key),
                  new KeyValuePair<string,string>("borne",Global.Global.BORNE),

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

        public static Response confirm(User user)
        {
            Response resultat = null;
            IEnumerable<KeyValuePair<string, string>> queries = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("login",user.login),
                new KeyValuePair<string,string>("password",user.password),
                new KeyValuePair<string,string>("key",user.key),
                  new KeyValuePair<string,string>("borne",Global.Global.BORNE),

            };
            try
            {
                HttpContent q = new FormUrlEncodedContent(queries);
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = client.PostAsync(Global.Global.URL_CONFIRM, q).Result;
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
