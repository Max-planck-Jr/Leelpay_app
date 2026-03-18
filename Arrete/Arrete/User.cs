using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arrete
{
    class User
    {
        public string login;
        public string password;
        public string key;

        public User(string login, string password, string key)
        {
            this.login = login;
            this.password = password;
            this.key = key;
        }

       
    }
}
