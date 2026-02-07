using System;
using System.Collections.Generic;
using AI_TestMaker.Properties;
using System.Linq;
using System.util;

public static class TopicHistoryManager
{
    private const int MaxHistorial = 50;

    // Estructura interna: tema|contador
    public static List<(string Tema, int Frecuencia)> ObtenerHistorial()
    {
        string raw = Settings.Default.HistorialTemas;

        if (string.IsNullOrWhiteSpace(raw))
            return new List<(string, int)>();

        return raw.Split('|')
                  .Select(item =>
                  {
                      var parts = item.Split(';');
                      return (Tema: parts[0], Frecuencia: int.Parse(parts[1]));
                  })
                  .OrderByDescending(x => x.Frecuencia)
                  .ToList();
    }

    public static void AgregarTema(string tema)
    {
        if (string.IsNullOrWhiteSpace(tema))
            return;

        var historial = ObtenerHistorial();

        var existente = historial.FirstOrDefault(x => x.Tema == tema);

        if (existente.Tema != null)
        {
            historial.Remove(existente);
            historial.Add((tema, existente.Frecuencia + 1));
        }
        else
        {
            historial.Add((tema, 1));
        }

        historial = historial
            .OrderByDescending(x => x.Frecuencia)
            .Take(MaxHistorial)
            .ToList();

        Guardar(historial);
    }

    public static void BorrarHistorial()
    {
        Settings.Default.HistorialTemas = "";
        Settings.Default.Save();
    }

    private static void Guardar(List<(string Tema, int Frecuencia)> historial)
    {
        string raw = string.Join("|", historial.Select(x => $"{x.Tema};{x.Frecuencia}"));
        Settings.Default.HistorialTemas = raw;
        Settings.Default.Save();
    }
}
