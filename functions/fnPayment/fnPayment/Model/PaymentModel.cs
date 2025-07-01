using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace fnPayment.Model
{
    internal class PaymentModel
    {

        public string nome { get; set; }
        public string email { get; set; }
        public string modelo { get; set; }
        public string ano { get; set; }
        public string tempoAluguel { get; set; }
        public DateTime data {  get; set; }
        public string id { get { return Guid.NewGuid().ToString(); } }
        public string IdPayment { get { return Guid.NewGuid().ToString(); } }

        public string Status { get; set; }
        public DateTime? DataAprovacao { get; set; }

    }
}
