using System;
using System.Collections.Generic;
using System.Linq;

namespace AI_TestMaker
{
    public class Test
    {
        public string Dificultad { get; set; }
        public string Tema { get; set; }   // ← NUEVO
        public DateTime Fecha { get; set; }
        public List<Pregunta> Preguntas { get; set; }

        public DateTime Inicio { get; set; }
        public DateTime? Fin { get; set; }
        public TimeSpan TiempoMaximo { get; set; }

        public TimeSpan TiempoEmpleado =>
            Fin.HasValue ? Fin.Value - Inicio : TimeSpan.Zero;

        public Test(string dificultad, List<Pregunta> preguntas, TimeSpan tiempoMaximo, string tema)
        {
            Dificultad = dificultad;
            Preguntas = preguntas;
            Fecha = DateTime.Now;

            TiempoMaximo = tiempoMaximo;
            Inicio = DateTime.Now;

            Tema = tema;
        }

        public int CalcularAciertos()
        {
            return Preguntas.Count(p => p.EsCorrecta());
        }

        public int CalcularFallos()
        {
            return Preguntas.Count - CalcularAciertos();
        }

        public double CalcularNota()
        {
            if (Preguntas.Count == 0)
                return 0;

            return (double)CalcularAciertos() / Preguntas.Count * 10.0;
        }
    }
}
