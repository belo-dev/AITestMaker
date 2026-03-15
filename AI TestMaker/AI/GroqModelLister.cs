using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class GroqModelLister
{
    public static async Task<List<string>> ObtenerModelos(string apiKey)
    {
        var http = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
            "https://api.groq.com/openai/v1/models");

        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await http.SendAsync(request);
        string json = await response.Content.ReadAsStringAsync();

        var data = JObject.Parse(json);

        var lista = new List<string>();

        foreach (var m in data["data"])
        {
            lista.Add(m["id"].ToString());
        }

        return lista;
    }
}
