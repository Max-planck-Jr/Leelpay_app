using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERA.Helpers
{
    class Helper
    {
        public static bool isPhone(string phone)
        {
            if ((phone.Trim().StartsWith("69")
                || phone.Trim().StartsWith("67")
                || phone.Trim().StartsWith("66")
                || phone.Trim().StartsWith("65")
                || phone.Trim().StartsWith("2")
                || phone.Trim().StartsWith("68")
                ) && phone.Trim().Length == 9)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool isValidAmount(string montant)
        {
            if (!string.IsNullOrEmpty(montant) || !string.IsNullOrWhiteSpace(montant) || montant.Trim().Length != 0)
            {

                int amount = int.Parse(montant);
                return amount % 500 == 0;
            }
            else
            {
                return false;
            }

        }
    }
}
