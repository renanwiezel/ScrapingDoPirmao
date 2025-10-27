using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ScrapingDoPirmao
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var request = new Request();
            var url = "https://noticias.uol.com.br";

            try
            {
                string html = await request.GetHtmlAsync(url);
                Console.WriteLine(html);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Erro ao fazer a requisição: {ex.Message}");
            }
        }
    }
}
