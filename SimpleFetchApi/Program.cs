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

app.MapGet("/", () => Results.Redirect("/fetch?url=noticias.uol.com.br"));

app.MapGet("/fetch", async (string url) =>
{
    try
    {
        var req = new Request();
        var html = await req.GetHtmlAsync(Normalize(url));
        return Results.Text(html, "text/plain; charset=utf-8");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro em /fetch: {ex.Message}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
})
.WithSummary("Faz GET no URL e retorna o HTML como texto")
.Produces<string>(contentType: "text/plain");

app.MapGet("/fetch-html", async (string? url) =>
{
    try
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Results.BadRequest("Parâmetro 'url' é obrigatório");
        }

        var req = new Request();
        var html = await req.GetHtmlAsync(Normalize(url));
        return Results.Content(html, "text/html; charset=utf-8");
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Erro HTTP em /fetch-html: {ex.Message}");
        return Results.Problem(detail: $"Erro ao fazer request: {ex.Message}", statusCode: 500);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro geral em /fetch-html: {ex.Message}\n{ex.StackTrace}");
        return Results.Problem(detail: ex.Message, statusCode: 500);
    }
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

        client.Timeout = TimeSpan.FromSeconds(30);
        // Headers mais completos para simular um browser real
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        client.DefaultRequestHeaders.Add("DNT", "1");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");

        // Referer simulando acesso do Google (às vezes ajuda)
        client.DefaultRequestHeaders.Add("Referer", "https://www.google.com/");

        return client;
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        using var client = CreateClient();

        await Task.Delay(Random.Shared.Next(500, 1500));

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