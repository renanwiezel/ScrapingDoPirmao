<?php
// ATIVA EXIBIÇÃO DE ERROS
error_reporting(E_ALL);
ini_set('display_errors', 1);

// Configurações
$url = 'https://noticias.uol.com.br/';

// Arquivo temporário para cookies
$cookiesFile = sys_get_temp_dir() . '/uol_cookies.txt';

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
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

// Verifica se teve sucesso
if ($httpCode != 200) {
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode(['erro' => "HTTP $httpCode", 'mensagem' => 'Não foi possível acessar o site'], JSON_UNESCAPED_UNICODE);
    exit;
}

// Carrega o HTML no DOMDocument
$dom = new DOMDocument();
@$dom->loadHTML($html);
$xpath = new DOMXPath($dom);

// Busca notícias
$query = '//div[contains(@class, "thumb")]//a[.//h3[contains(@class, "thumb-title")]]';
$links = $xpath->query($query);

$noticias = [];

foreach ($links as $link) {
    $href = $link->getAttribute('href');
    
    // Filtra apenas URLs de notícias válidas
    if (empty($href) || 
        strpos($href, 'javascript:') !== false ||
        strpos($href, '#') === 0 ||
        !preg_match('/\/(esporte|cotidiano|politica|economia|internacional|noticias)\/.*\.htm$/', $href)) {
        continue;
    }
    
    $h3Elements = $xpath->query('.//h3[contains(@class, "thumb-title")]', $link);
    
    if ($h3Elements->length > 0) {
        $titulo = trim($h3Elements->item(0)->textContent);
        
        // Evita títulos muito curtos
        if (strlen($titulo) < 10) {
            continue;
        }
        
        // Garante URL absoluta
        if (strpos($href, 'http') !== 0) {
            $href = 'https://noticias.uol.com.br' . $href;
        }
        
        $noticias[] = [
            'titulo' => $titulo,
            'link' => $href
        ];
    }
}

// Remove duplicatas baseado no link
$noticias = array_values(array_unique($noticias, SORT_REGULAR));

// Limita a 20 notícias
$noticias = array_slice($noticias, 0, 20);

// Retorna JSON limpo
header('Content-Type: application/json; charset=utf-8');
echo json_encode([
    'total' => count($noticias),
    'fonte' => 'UOL Notícias',
    'url' => $url,
    'data_coleta' => date('Y-m-d H:i:s'),
    'noticias' => $noticias
], JSON_UNESCAPED_UNICODE | JSON_PRETTY_PRINT);
?>