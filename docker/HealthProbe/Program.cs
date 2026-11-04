using System.Text.Json;
try {
    using var http=new HttpClient(new HttpClientHandler{UseProxy=false,AllowAutoRedirect=false}){Timeout=TimeSpan.FromSeconds(2)};
    using var response=await http.GetAsync("http://127.0.0.1:5077/health");
    response.EnsureSuccessStatusCode();
    using var body=JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    return body.RootElement.GetProperty("status").GetString()=="ok" && body.RootElement.GetProperty("mode").GetString()=="local-mock"?0:1;
} catch { return 1; }
