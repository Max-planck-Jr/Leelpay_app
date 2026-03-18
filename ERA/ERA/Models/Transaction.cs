using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERA.Models
{
    class Transaction
    {
        public int transactionId { get; set; }
        public string access_token { get; set; }
        public string sender_name { set; get; }
        public string sender_phone { get; set; }
        public string receiver_name { get; set; }
        public string receiver_phone { get; set; }
        public int amount { get; set; }
        public int frais { get; set; }
        public int Iamount { get; set; }
        public string net { get { return (amount - frais).ToString(); } }
        public string bordo { get; set; }
    }
}
