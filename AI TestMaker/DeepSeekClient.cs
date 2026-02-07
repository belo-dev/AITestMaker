using AI_TestMaker;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

public class DeepSeekAgent : IAgent
{
    private readonly HttpClient _http = new HttpClient();
    private readonly string _apiKey;

    public string Nombre => "DeepSeek";

    public DeepSeekAgent(string apiKey)
    {
        _apiKey = apiKey?.Trim() ?? throw new ArgumentNullException(nameof(apiKey));
    }

    public async Task<string> ChatCompletion(string prompt)
    {
        var payload = new
        {
            model = "deepseek-chat",
            messages = new[]
            {
                new { role = "user", content = prompt }
            }
        };

        string json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

        var request = new HttpRequestMessage(HttpMethod.Post,
            "https://api.deepseek.com/v1/chat/completions");

        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        string respuesta = await response.Content.ReadAsStringAsync();

        var data = JObject.Parse(respuesta);

        if (data["error"] != null)
            throw new Exception($"DeepSeek Error: {data["error"]["message"]}");

        if (data["choices"] == null || !data["choices"].HasValues)
            throw new Exception("DeepSeek no devolvió ninguna respuesta válida.");

        return data["choices"][0]["message"]["content"].ToString();
    }
}
