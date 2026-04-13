using AI_TestMaker;
using System;
using System.Collections.Generic;

public static class AgentManager
{
    public static List<IAgent> Agentes { get; } = new List<IAgent>();

    public static void RegistrarAgentes(string deepseekKey, string groqKey, string geminiKey, string openrouterKey)
    {
        Agentes.Clear();

        if (!string.IsNullOrWhiteSpace(deepseekKey))
            Agentes.Add(new DeepSeekAgent(deepseekKey));

        if (!string.IsNullOrWhiteSpace(groqKey))
            Agentes.Add(new GroqAgent(groqKey));

        if (!string.IsNullOrWhiteSpace(geminiKey))
            Agentes.Add(new GeminiAgent(geminiKey));

        if (!string.IsNullOrWhiteSpace(openrouterKey))
            Agentes.Add(new DeepSeekAgentOpenRouter(openrouterKey));

        Agentes.Add(new LMStudioAgent());
    }

    public static IAgent ObtenerAgente(string nombre)
    {
        return Agentes.Find(a => a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<string> EjecutarConFallback(string nombreAgente, string prompt)
    {
        IAgent agente = ObtenerAgente(nombreAgente);

        if (agente == null)
            throw new Exception($"No existe el agente '{nombreAgente}'.");

        try
        {
            // Intento principal
            return await agente.ChatCompletion(prompt);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error con {nombreAgente}: {ex.Message}");
            Console.WriteLine("→ Intentando fallback con LMStudio...");

            // Buscar LMStudio
            var lm = ObtenerAgente("LMStudio");

            if (lm == null)
                throw new Exception("No se pudo usar el fallback porque LMStudio no está registrado.");

            return await lm.ChatCompletion(prompt);
        }
    }
    public static async Task<string> EjecutarSinFallback(string nombreAgente, string prompt)
    {
        IAgent agente = ObtenerAgente(nombreAgente);

        if (agente == null)
            throw new Exception($"No existe el agente '{nombreAgente}'.");

        // Aquí NO hay fallback, si falla, falla.
        return await agente.ChatCompletion(prompt);
    }
}
