using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MoMo.Helpers
{
    class Helper
    {
        public static bool isPhone(string phone)
        {
           if(phone.Trim().Length != 9)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        public static bool isValidAmnt(string amount)
        {
            if(String.IsNullOrEmpty(amount) || String.IsNullOrWhiteSpace(amount))
            {
                return false;
            }
            int montant = int.Parse(amount);

            if(montant % 500 == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
