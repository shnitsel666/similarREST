using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarRest.Models
{
    public class ProtectedHeader
    {
        public string Alg { get; set; }
        public string Kid { get; set; }
        public string Signdate { get; set; }
        public string Cty { get; set; }
    }
}
