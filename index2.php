<?php
// ATIVA EXIBIÇÃO DE ERROS
error_reporting(E_ALL);
ini_set('display_errors', 1);

// URL alvo
$url = 'https://noticias.uol.com.br/';

echo "Iniciando request HTTPS para: $url\n\n";

$cookiesFile = __DIR__ . '/cookies_uol.txt';

// Inicializa cURL
$ch = curl_init($url);

// Configurações principais
curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_FOLLOWLOCATION => true,
    CURLOPT_MAXREDIRS      => 5,
    CURLOPT_TIMEOUT        => 30,
    CURLOPT_CONNECTTIMEOUT => 15,

    // SSL verificado (melhor prática)
    CURLOPT_SSL_VERIFYPEER => true,
    CURLOPT_SSL_VERIFYHOST => 2,

    // Forçar somente HTTPS (também em redirecionamentos)
    CURLOPT_PROTOCOLS      => CURLPROTO_HTTPS,
    CURLOPT_REDIR_PROTOCOLS=> CURLPROTO_HTTPS,

    // HTTP/2 e decodificação automática (gzip/deflate/br)
    CURLOPT_HTTP_VERSION   => CURL_HTTP_VERSION_2TLS,
    CURLOPT_ENCODING       => '',


    CURLOPT_USERAGENT      => 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36',

    CURLOPT_COOKIEJAR      => $cookiesFile,
    CURLOPT_COOKIEFILE     => $cookiesFile,

    // Cabeçalhos de navegador
    CURLOPT_HTTPHEADER     => [
        'Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8',
        'Accept-Language: pt-BR,pt;q=0.9,en-US;q=0.8,en;q=0.7',
        'Connection: keep-alive',
        'Upgrade-Insecure-Requests: 1',
        'Referer: https://www.uol.com.br/',

        'Sec-Fetch-Site: none',
        'Sec-Fetch-Mode: navigate',
        'Sec-Fetch-User: ?1',
        'Sec-Fetch-Dest: document',
    ],
]);

// Executa o request
$html = curl_exec($ch);

// Infos da requisição
$httpCode     = curl_getinfo($ch, CURLINFO_HTTP_CODE);
$effectiveUrl = curl_getinfo($ch, CURLINFO_EFFECTIVE_URL);

// Tratamento de erro/saída
if ($html === false) {
    echo "ERRO cURL: " . curl_error($ch) . "\n";
    echo "Código do erro: " . curl_errno($ch) . "\n";
} else {
    echo "URL final acessada: $effectiveUrl\n";
    echo "Status Code recebido: $httpCode\n\n";

    if ($httpCode == 200) {
        echo "✅ Sucesso! HTML recebido (" . strlen($html) . " bytes)\n\n";
        echo "--- INÍCIO DO HTML ---\n";
        echo $html;
        echo "\n--- FIM DO HTML ---\n";
    } else {
        echo "❌ Bloqueado/erro (HTTP $httpCode)\n\n";
        echo "Mensagem do servidor:\n";
        echo "--- INÍCIO DA RESPOSTA ---\n";
        echo $html;
        echo "\n--- FIM DA RESPOSTA ---\n";
    }
}

// Fecha conexão
curl_close($ch);
?>