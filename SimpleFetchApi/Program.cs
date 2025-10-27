using System;
using System.Net;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/fetch", async (string url) =>
{
    var req = new Request();
    var html = await req.GetHtmlAsync(Normalize(url));
    // text/plain pra Swagger não tentar renderizar HTML
    return Results.Text(html, "text/plain; charset=utf-8");
})
.WithSummary("Faz GET no URL e retorna o HTML como texto")
.Produces<string>(contentType: "text/plain");

app.MapGet("/fetch-html", async (string url) =>
{
    var req = new Request();
    var html = await req.GetHtmlAsync(Normalize(url));
    // text/html pra abrir como página (fora do Swagger fica lindo)
    return Results.Content(html, "text/html; charset=utf-8");
})
.WithSummary("Faz GET no URL e retorna como text/html (renderizável)");

app.Run();

static string Normalize(string url)
{
    url = (url ?? "").Trim();
    if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
        !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        url = "https://" + url;
    return url;
}

public class Request
{
    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip |
                                     DecompressionMethods.Deflate |
                                     DecompressionMethods.Brotli
        };

        var client = new HttpClient(handler);

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        client.DefaultRequestHeaders.Connection.ParseAdd("keep-alive");
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("pt-BR,pt;q=0.9");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml") { Quality = 0.9 });
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*") { Quality = 0.8 });

        return client;
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        using var client = CreateClient();
        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            var preview = body.Length > 500 ? body[..500] : body;
            throw new HttpRequestException(
                $"Falha HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}. Trecho do corpo: {preview}"
            );
        }

        return await resp.Content.ReadAsStringAsync();
    }

}