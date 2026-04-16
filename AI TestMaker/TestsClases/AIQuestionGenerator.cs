using AI_TestMaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class AIQuestionGenerator
{
    public static async Task<List<Pregunta>> GenerarPreguntasIA(
            string tema, string dificultad, string agenteSeleccionado)
    {
        var agente = AgentManager.ObtenerAgente(agenteSeleccionado);

        if (agente == null)
            throw new Exception($"No se encontró el agente: {agenteSeleccionado}");

        int cantidad = dificultad switch
        {
            "Fácil" => 20,
            "Medio" => 50,
            "Difícil" => 100,
            _ => 20
        };

        int maxPorTanda = 5;
        var preguntasAcumuladas = new List<Pregunta>();
        var enunciadosPrevios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        int restantes = cantidad;

        while (restantes > 0)
        {
            int cantidadTanda = Math.Min(restantes, maxPorTanda);

            string prompt = CrearPromptConExclusiones(
                tema, dificultad, cantidadTanda, preguntasAcumuladas
            );

            var respuesta = await AgentManager.EjecutarConFallback(agente.Nombre, prompt);

            var nuevasPreguntas = ParsearPreguntas(respuesta);

            foreach (var pregunta in nuevasPreguntas)
            {
                // Limpieza de numeración automática
                pregunta.Enunciado = Regex.Replace(
                    pregunta.Enunciado,
                    @"^\s*\d+[\.\)\-]\s*",
                    ""
                );

                if (!enunciadosPrevios.Contains(pregunta.Enunciado))
                {
                    preguntasAcumuladas.Add(pregunta);
                    enunciadosPrevios.Add(pregunta.Enunciado);
                }
            }

            restantes = cantidad - preguntasAcumuladas.Count;

            if (nuevasPreguntas.Count == 0)
                throw new Exception("No se pudieron generar suficientes preguntas únicas.");
        }

        // 🔥 Numeración final correcta
        var listaFinal = preguntasAcumuladas.Take(cantidad).ToList();

        for (int i = 0; i < listaFinal.Count; i++)
            listaFinal[i].Numero = i + 1;

        return listaFinal;
    }


    private static string CrearPromptConExclusiones(string tema, string dificultad, int cantidad, List<Pregunta> preguntasPrevias)
    {
        string exclusiones = "";
        if (preguntasPrevias.Count > 0)
        {
            exclusiones = "NO repitas ni crees preguntas similares a las siguientes ya generadas:\n";
            foreach (var p in preguntasPrevias)
                exclusiones += $"- {p.Enunciado}\n";
        }

        return $@"
Genera EXACTAMENTE {cantidad} preguntas tipo test sobre el tema ""{tema}"".
Nivel de dificultad: {dificultad}.

{exclusiones}

INSTRUCCIONES CRÍTICAS:
- Devuelve ÚNICAMENTE JSON válido.
- NO añadas texto antes ni después del JSON.
- NO añadas explicaciones, comentarios ni notas.
- NO uses comillas inteligentes.
- NO cambies los nombres de los campos.
- NO añadas campos adicionales.
- NO incluyas saltos de línea fuera del JSON.
- NO incluyas caracteres extra después del cierre del JSON.

FORMATO JSON OBLIGATORIO:

{{
  ""preguntas"": [
    {{
      ""enunciado"": ""texto de la pregunta"",
      ""opciones"": [
        {{ ""texto"": ""opción A"", ""correcta"": true }},
        {{ ""texto"": ""opción B"", ""correcta"": false }},
        {{ ""texto"": ""opción C"", ""correcta"": false }},
        {{ ""texto"": ""opción D"", ""correcta"": false }}
      ]
    }}
  ]
}}

REQUISITOS:
- Cada pregunta debe tener EXACTAMENTE 4 opciones.
- SOLO una opción debe tener ""correcta"": true.
- El JSON debe ser válido y parseable sin errores.
- No incluyas numeración en el enunciado.
- No incluyas saltos de línea dentro de los textos.
- Dentro de los valores de texto NO se permiten comillas dobles. Usa comillas simples si es necesario.
- NO incluyas caracteres especiales sin escapar.

DEVUELVE SOLO EL JSON.
";
    }

    // 🔥 Blindaje total: extrae solo el JSON válido
    private static string ExtraerSoloJSON(string raw)
    {
        int first = raw.IndexOf('{');
        int last = raw.LastIndexOf('}');

        if (first == -1 || last == -1 || last <= first)
            throw new Exception("La IA devolvió un formato no válido.");

        string contenido = raw.Substring(first, last - first + 1);

        // Si hay otro JSON después, eliminarlo
        int next = raw.IndexOf('{', last + 1);
        if (next != -1)
            return contenido;

        return contenido;
    }

    private static List<Pregunta> ParsearPreguntas(string json)
    {
        json = ExtraerSoloJSON(json);
        json = RepararJsonIA(json);

        // Validación mínima
        if (!json.Contains("\"preguntas\""))
            throw new Exception("La IA no devolvió el campo 'preguntas'.");

        var root = JsonConvert.DeserializeObject<RootPreguntas>(json);

        if (root?.preguntas == null)
            throw new Exception("La IA no devolvió preguntas en el formato esperado.");

        var lista = new List<Pregunta>();

        foreach (var p in root.preguntas)
        {
            var opciones = new List<Opcion>();

            foreach (var o in p.opciones)
                opciones.Add(new Opcion(o.texto, o.correcta));

            lista.Add(new Pregunta(p.enunciado, opciones));
        }

        return lista;
    }

    private static string RepararJsonIA(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        // 0. Eliminar comentarios y símbolos peligrosos
        json = Regex.Replace(json, @"//.*?(?=\n|$)", ""); // comentarios //
        json = Regex.Replace(json, @"/\*.*?\*/", "", RegexOptions.Singleline); // comentarios /* */
        json = Regex.Replace(json, @"--?>", ""); // -->
        json = Regex.Replace(json, @"=>", "");  // =>
        json = Regex.Replace(json, @"->", "");  // ->
        json = Regex.Replace(json, @"(?<![""'])>(?![""'])", ""); // > sueltos

        // 1. Eliminar caracteres invisibles
        json = json.Replace("\r", "")
                   .Replace("\n", "")
                   .Replace("\t", "")
                   .Replace("\u00A0", " ")
                   .Replace("\u200B", "");

        // 2. Asegurar claves entre comillas
        json = Regex.Replace(json, @"(?<={|,)\s*(\w+)\s*:", "\"$1\":");

        // 3. Reemplazar comillas internas por comillas simples
        json = Regex.Replace(json, "\"([^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\"", match =>
        {
            string contenido = match.Value;

            if (Regex.IsMatch(contenido, "^\"[a-zA-Z0-9_]+\":$"))
                return contenido;

            string limpio = contenido.Substring(1, contenido.Length - 2)
                                     .Replace("\"", "'");

            return "\"" + limpio + "\"";
        });

        // 4. Arreglar objetos truncados
        int openBraces = json.Count(c => c == '{');
        int closeBraces = json.Count(c => c == '}');
        while (closeBraces < openBraces)
        {
            json += "}";
            closeBraces++;
        }

        // 5. Arreglar arrays truncados
        int openBrackets = json.Count(c => c == '[');
        int closeBrackets = json.Count(c => c == ']');
        while (closeBrackets < openBrackets)
        {
            json += "]";
            closeBrackets++;
        }

        // 6. Eliminar comas colgantes
        json = Regex.Replace(json, @",\s*([}\]])", "$1");

        // 7. Reemplazar null por string vacío
        json = json.Replace(": null", ": \"\"");

        return json;
    }
}

public class RootPreguntas
{
    public List<PreguntaIA> preguntas { get; set; }
}

public class PreguntaIA
{
    public string enunciado { get; set; }
    public List<OpcionIA> opciones { get; set; }
}

public class OpcionIA
{
    public string texto { get; set; }
    public bool correcta { get; set; }
}