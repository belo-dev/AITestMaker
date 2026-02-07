using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_TestMaker
{
    public class Pregunta
    {
        public static int contador = 1;
        public int Numero { get; set; }
        public string Enunciado { get; set; }
        public List<Opcion> Opciones { get; set; }
        public int? RespuestaUsuario { get; set; }

        public Pregunta(string enunciado, List<Opcion> opciones)
        {
            Numero = contador++;
            Enunciado = enunciado;
            Opciones = opciones;
            RespuestaUsuario = null;
        }

        public bool EsCorrecta()
        {
            if (RespuestaUsuario == null)
                return false;

            return Opciones[(int)RespuestaUsuario].EsCorrecta;
        }

        public string EnunciadoConNumero => $"{Numero}. {Enunciado}";
    }
}
