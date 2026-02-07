using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI_TestMaker
{
    public static class MockQuestionGenerator
    {
        public static List<Pregunta> GenerarPreguntas(string dificultad)
        {

            //Cantidad de preguntas por dificultad
            int cantidad = dificultad switch
            {
                "Fácil" => 20,
                "Medio" => 50,
                "Difícil" => 100,
                _ => 20
            };

            var preguntas = new List<Pregunta>();

            // Preguntas de ejemplo
            for (int i = 1; i <= cantidad; i++)
            {
                preguntas.Add(new Pregunta(
                    $"Pregunta de ejemplo {i} ({dificultad})",
                    new List<Opcion>
                    {
                    new Opcion("Opción A", esCorrecta: i % 4 == 0),
                    new Opcion("Opción B", esCorrecta: i % 4 == 1),
                    new Opcion("Opción C", esCorrecta: i % 4 == 2),
                    new Opcion("Opción D", esCorrecta: i % 4 == 3)
                    }
                ));
            }

            return preguntas;
        }
    }
}
