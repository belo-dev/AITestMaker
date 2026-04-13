using AI_TestMaker;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class LMStudioAgent : IAgent
{
    private readonly HttpClient _http = new HttpClient();
    private readonly string _endpoint;
    private readonly string _model;

    public string Nombre => "LMStudio";

    public LMStudioAgent(string endpoint = "https://ia.belo-dev.site/v1/chat/completions",
                         string model = "openai/gpt-oss-20b")
    {
        // Timeout ampliado
        _http.Timeout = TimeSpan.FromMinutes(20);
        _endpoint = endpoint;
        _model = model;
    }

    public async Task<string> ChatCompletion(string prompt)
    {
        var payload = new
        {
            model = _model,
            messages = new[]
            {
            new { role = "user", content = prompt }
        }
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

        var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
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
            throw new Exception("LMStudio tardó demasiado en responder (timeout).");
        }

        string respuesta = await response.Content.ReadAsStringAsync();

        var data = JObject.Parse(respuesta);

        if (data["error"] != null)
            throw new Exception($"LMStudio Error: {data["error"]["message"]}");

        if (data["choices"] == null || !data["choices"].HasValues)
            throw new Exception("LMStudio no devolvió ninguna respuesta válida.");

        return data["choices"][0]["message"]["content"].ToString();
    }
}