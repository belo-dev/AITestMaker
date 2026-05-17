using AI_TestMaker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public static class AIQuestionGenerator
{
    // 🔥 Activa/desactiva logs globalmente
    private const bool DEBUG_LOG = true;

    public static async Task<List<Pregunta>> GenerarPreguntasIA(
            string tema, string dificultad, string? cantidadPersonalizada, string agenteSeleccionado)
    {
        var agente = AgentManager.ObtenerAgente(agenteSeleccionado);

        if (agente == null)
            throw new Exception($"No se encontró el agente: {agenteSeleccionado}");

        int cantidad = dificultad switch
        {
            "Fácil" => 20,
            "Medio" => 50,
            "Difícil" => 100,
            "Personalizado" =>
                int.TryParse(cantidadPersonalizada, out int n)
                    ? Math.Clamp(n, 1, 200)
                    : 20,
            _ => 20
        };


        int maxPorTanda = 5;
        var preguntasAcumuladas = new List<Pregunta>();
        var enunciadosPrevios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (dificultad == "Personalizado")
            dificultad = "Medio";
        int restantes = cantidad;

        while (restantes > 0)
        {
            int cantidadTanda = Math.Min(restantes, maxPorTanda);

            string prompt = CrearPromptConExclusiones(
                tema, dificultad, cantidadTanda, preguntasAcumuladas
            );

            List<Pregunta> nuevasPreguntas = await ObtenerPreguntasConFallback(agente.Nombre, prompt);

            foreach (var pregunta in nuevasPreguntas)
            {
                pregunta.Enunciado = SanitizarTexto(pregunta.Enunciado);

                if (!enunciadosPrevios.Contains(pregunta.Enunciado))
                {
                    preguntasAcumuladas.Add(pregunta);
                    enunciadosPrevios.Add(pregunta.Enunciado);
                }
            }

            restantes = cantidad - preguntasAcumuladas.Count;
        }

        var listaFinal = preguntasAcumuladas.Take(cantidad).ToList();

        for (int i = 0; i < listaFinal.Count; i++)
            listaFinal[i].Numero = i + 1;

        return listaFinal;
    }

    // ============================================================
    // 🔥 Prompt con exclusiones restaurado
    // ============================================================
    private static string CrearPromptConExclusiones(string tema, string dificultad, int cantidad, List<Pregunta> previas)
    {
        string exclusiones = "";

        if (previas.Count > 0)
        {
            exclusiones = "NO repitas ni crees preguntas similares a las siguientes ya generadas:\n";
            foreach (var p in previas)
                exclusiones += $"- {p.Enunciado}\n";
        }

        return $@"
Genera EXACTAMENTE {cantidad} preguntas en Español tipo test sobre el tema ""{tema}"".
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

    // ============================================================
    // 🔥 Fallback con logging
    // ============================================================
    private static async Task<List<Pregunta>> ObtenerPreguntasConFallback(string agente, string prompt)
    {
        int intentos = 0;
        const int MAX_INTENTOS = 20;

        while (intentos < MAX_INTENTOS)
        {
            string respuesta = await AgentManager.EjecutarConFallback(agente, prompt);

            Log($"Intento #{intentos + 1}");
            LogJson("JSON ORIGINAL RECIBIDO", respuesta);

            try
            {
                var preguntas = ParsearPreguntas(respuesta);

                if (ValidarEstructuraPreguntas(preguntas))
                {
                    Log("✔ JSON ACEPTADO");
                    LogJson("JSON ACEPTADO", respuesta);
                    return preguntas;
                }
                else
                {
                    Log("❌ JSON DESCARTADO POR ESTRUCTURA");
                    LogJson("JSON DESCARTADO (estructura inválida)", respuesta);
                }
            }
            catch (Exception ex)
            {
                Log($"❌ Error parseando JSON: {ex.Message}");
                LogJson("JSON DESCARTADO (error parseo)", respuesta);
            }

            intentos++;
            await Task.Delay(200);
        }

        throw new Exception("No se pudo obtener un JSON válido tras múltiples intentos.");
    }

    // ============================================================
    // 🔥 Parseo blindado con logs
    // ============================================================
    private static List<Pregunta> ParsearPreguntas(string raw)
    {
        LogJson("RAW COMPLETO", raw);

        string json = ExtraerJSONRobusto(raw);
        LogJson("JSON EXTRAÍDO", json);

        if (!EsJsonValido(json))
        {
            Log("⚠ JSON inválido, intentando reparación…");

            string reparado = RepararJsonIA(json);
            LogJson("JSON REPARADO", reparado);

            if (!EsJsonValido(reparado))
                throw new Exception("JSON inválido incluso tras reparación.");

            json = reparado;
        }

        LogJson("JSON FINAL A DESERIALIZAR", json);

        var root = JsonConvert.DeserializeObject<RootPreguntas>(json);

        if (root?.preguntas == null)
            throw new Exception("La IA no devolvió preguntas en el formato esperado.");

        var lista = new List<Pregunta>();

        foreach (var p in root.preguntas)
        {
            var opciones = p.opciones
                .Select(o => new Opcion(SanitizarTexto(o.texto), o.correcta))
                .ToList();

            lista.Add(new Pregunta(SanitizarTexto(p.enunciado), opciones));
        }

        return lista;
    }

    // ============================================================
    // 🔥 Extractor JSON robusto basado en stack
    // ============================================================
    private static string ExtraerJSONRobusto(string raw)
    {
        bool dentroString = false;
        int nivel = 0;
        int inicio = -1;

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];

            if (c == '"' && (i == 0 || raw[i - 1] != '\\'))
                dentroString = !dentroString;

            if (!dentroString)
            {
                if (c == '{')
                {
                    if (nivel == 0)
                        inicio = i;

                    nivel++;
                }
                else if (c == '}')
                {
                    nivel--;

                    if (nivel == 0 && inicio != -1)
                        return raw.Substring(inicio, i - inicio + 1);
                }
            }
        }

        throw new Exception("No se pudo extraer un JSON válido.");
    }

    // ============================================================
    // 🔥 Validación sintáctica
    // ============================================================
    private static bool EsJsonValido(string json)
    {
        try
        {
            JToken.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // 🔧 Reparación ligera
    // ============================================================
    private static string RepararJsonIA(string json)
    {
        json = json.Replace("\r", "")
                   .Replace("\n", "")
                   .Replace("\t", "")
                   .Replace("\u00A0", " ")
                   .Replace("\u200B", "");

        json = Regex.Replace(json, @"(?<={|,)\s*(\w+)\s*:", "\"$1\":");

        json = Regex.Replace(json, @",\s*([}\]])", "$1");

        return json;
    }

    // ============================================================
    // 🔥 Validación estructural completa
    // ============================================================
    private static bool ValidarEstructuraPreguntas(List<Pregunta> preguntas)
    {
        if (preguntas == null || preguntas.Count == 0)
            return false;

        foreach (var p in preguntas)
        {
            if (string.IsNullOrWhiteSpace(p.Enunciado))
                return false;

            if (p.Opciones == null || p.Opciones.Count != 4)
                return false;

            if (p.Opciones.Count(o => o.EsCorrecta) != 1)
                return false;

            if (p.Opciones.Any(o => string.IsNullOrWhiteSpace(o.Texto)))
                return false;
        }

        return true;
    }

    // ============================================================
    // 🔧 Sanitización de texto
    // ============================================================
    private static string SanitizarTexto(string s)
    {
        if (s == null) return "";

        s = s.Trim();
        s = Regex.Replace(s, @"\s+", " ");
        s = s.Replace("\u200B", "");
        s = s.Replace("\u00A0", " ");
        s = s.Replace("\"", "'");

        return s;
    }

    // ============================================================
    // 🔥 LOGGER
    // ============================================================
    private static void Log(string mensaje)
    {
        if (!DEBUG_LOG) return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[AI-LOG] {mensaje}");
        Console.ResetColor();
    }

    private static void LogJson(string titulo, string contenido)
    {
        if (!DEBUG_LOG) return;

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n===== {titulo} =====");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(contenido);
        Console.ResetColor();

        Console.WriteLine("============================\n");
    }

    // ============================================================
    // 🔧 Clases auxiliares
    // ============================================================
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
}
