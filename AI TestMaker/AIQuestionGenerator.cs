using AI_TestMaker;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        string prompt = CrearPrompt(tema, dificultad, cantidad);

        string respuesta = await agente.ChatCompletion(prompt);

        return ParsearPreguntas(respuesta);
    }

    public static string CrearPrompt(string tema, string dificultad, int cantidad)
    {
        return $@"
Genera EXACTAMENTE {cantidad} preguntas tipo test sobre el tema ""{tema}"".
Nivel de dificultad: {dificultad}.

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
- Dentro de los valores de texto NO se permiten comillas dobles. Si necesitas comillas, usa comillas simples.
- NO incluyas caracteres especiales sin escapar.

DEVUELVE SOLO EL JSON.
";
    }


    private static List<Pregunta> ParsearPreguntas(string json)
    {
        json = RepararJsonIA(json);
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

        // 1. Eliminar caracteres invisibles o raros
        json = json.Replace("\r", "")
                   .Replace("\n", "")
                   .Replace("\t", "")
                   .Replace("\u00A0", " ")
                   .Replace("\u200B", "");

        // 2. Asegurar que las claves están entre comillas
        json = Regex.Replace(json, @"(?<={|,)\s*(\w+)\s*:", "\"$1\":");

        // 3. Reemplazar comillas internas no escapadas dentro de strings
        json = Regex.Replace(json, "\"([^\"\\\\]*(?:\\\\.[^\"\\\\]*)*)\"", match =>
        {
            string contenido = match.Value;

            // Saltar claves del JSON
            if (Regex.IsMatch(contenido, "^\"[a-zA-Z0-9_]+\":$"))
                return contenido;

            // Reemplazar comillas internas por comillas simples
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
