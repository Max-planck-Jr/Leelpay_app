using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EUM.Http
{
    class Response
    {
        public int status { get; set; }
        public int transactionId { get; set; }
        public string message { get; set; }
    }
}
