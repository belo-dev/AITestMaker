using AI_TestMaker;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class GeminiAgent : IAgent
{
    private readonly HttpClient _http = new HttpClient();
    private readonly string _apiKey;
    private readonly string _model;

    public string Nombre => "Gemini";

    public GeminiAgent(string apiKey)
    {
        _apiKey = apiKey;

        //Peta por que es de pago
        _model = "gemini-2.5-flash";

        // Timeout amplio como en tu LMStudioAgent
        _http.Timeout = TimeSpan.FromMinutes(20);
    }

    public async Task<string> ChatCompletion(string prompt)
    {
        string endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var payload = new
        {
            contents = new[]
            {
                new {
                    role = "user",
                    parts = new[] {
                        new { text = prompt }
                    }
                }
            }
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        HttpResponseMessage response;

        try
        {
            response = await _http.SendAsync(request);
        }
        catch (TaskCanceledException)
        {
            throw new Exception("Gemini tardó demasiado en responder (timeout).");
        }

        string respuesta = await response.Content.ReadAsStringAsync();

        var data = JObject.Parse(respuesta);

        if (data["error"] != null)
            throw new Exception($"Gemini Error: {data["error"]["message"]}");

        // Gemini devuelve el texto en:
        // candidates[0].content.parts[0].text
        var texto = data["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();

        if (string.IsNullOrWhiteSpace(texto))
            throw new Exception("Gemini no devolvió ninguna respuesta válida.");

        return texto;
    }
}