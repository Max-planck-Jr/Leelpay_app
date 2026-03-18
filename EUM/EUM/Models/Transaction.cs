using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EUM.Models
{
    class Transaction
    {
        public int transactionId { get; set; }
        public string access_token { get; set; }
        public string phone { get; set; }
        public string receiver { get; set; }
        public string agence { get; set; }
        public string sender { get; set; }
        public string amount { get; set; }
        public int Iamount { get; set; }
        public int frais { get; set; }
        public string net {  get { return (int.Parse(amount) - frais).ToString(); } }

    }
}
