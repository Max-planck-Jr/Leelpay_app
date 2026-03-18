using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airtime.Models
{
    class Transaction
    {
        public string access_token { get; set; }
        public int transactionId { get; set; }
        public string phone { get; set; }
        public int amount { get; set; }
        public int Iamount { get; set; }
        public int op { get; set; }
    }
}
