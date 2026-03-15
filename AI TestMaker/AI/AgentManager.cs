using AI_TestMaker;
using System;
using System.Collections.Generic;

public static class AgentManager
{
    public static List<IAgent> Agentes { get; } = new List<IAgent>();

    public static void RegistrarAgentes(string deepseekKey, string groqKey)
    {
        Agentes.Clear();

        if (!string.IsNullOrWhiteSpace(deepseekKey))
            Agentes.Add(new DeepSeekAgent(deepseekKey));

        if (!string.IsNullOrWhiteSpace(groqKey))
            Agentes.Add(new GroqAgent(groqKey));
    }

    public static IAgent ObtenerAgente(string nombre)
    {
        return Agentes.Find(a => a.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase));
    }

    public static IAgent ObtenerPrimeroDisponible()
    {
        return Agentes.Count > 0 ? Agentes[0] : null;
    }
}
