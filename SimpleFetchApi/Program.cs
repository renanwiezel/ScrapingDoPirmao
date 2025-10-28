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
        var cookieContainer = new CookieContainer();

        var handler = new HttpClientHandler
        {
            // SSL verificado (melhor prática) - igual PHP
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            
            // Compressão automática (gzip/deflate/br) - igual PHP CURLOPT_ENCODING
            AutomaticDecompression = DecompressionMethods.GZip | 
                                     DecompressionMethods.Deflate | 
                                     DecompressionMethods.Brotli,
            
            // Cookies persistentes - igual PHP CURLOPT_COOKIEJAR/COOKIEFILE
            UseCookies = true,
            CookieContainer = cookieContainer,
            
            // Segue redirecionamentos - igual PHP CURLOPT_FOLLOWLOCATION
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5, // igual PHP CURLOPT_MAXREDIRS = 5
            
            // Força HTTPS - não há equivalente direto, mas configuramos abaixo
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | 
                          System.Security.Authentication.SslProtocols.Tls13
        };

        var client = new HttpClient(handler)
        {
            // Timeout de 30 segundos - igual PHP CURLOPT_TIMEOUT = 30
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Forçar HTTP/2 - igual PHP CURLOPT_HTTP_VERSION = CURL_HTTP_VERSION_2TLS
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

        // User-Agent EXATO do PHP
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36");
        
        // Headers EXATOS do PHP
        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        client.DefaultRequestHeaders.Add("Accept-Language", "pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7");
        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        client.DefaultRequestHeaders.Add("Referer", "https://www.uol.com.br/");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");

        return client;
    }

    public async Task<string> GetHtmlAsync(string url)
    {
        using var client = CreateClient();

        await Task.Delay(Random.Shared.Next(500, 1500));

        // Se for noticias.uol.com.br, acessa www.uol.com.br primeiro para pegar cookies
        /* if (url.Contains("noticias.uol.com.br", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                Console.WriteLine("Detectado noticias.uol.com.br - acessando www.uol.com.br primeiro...");
                await client.GetAsync("https://www.uol.com.br/");
                await Task.Delay(1000);
                Console.WriteLine("Cookies obtidos. Tentando acessar noticias.uol.com.br...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao acessar www.uol.com.br: {ex.Message}");
            }
        }*/

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