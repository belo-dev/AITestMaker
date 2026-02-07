using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_TestMaker
{
    public class Opcion
    {
        public string Texto { get; set; }
        public bool EsCorrecta { get; set; }

        public Opcion(string texto, bool esCorrecta)
        {
            Texto = texto;
            EsCorrecta = esCorrecta;
        }
    }
}
