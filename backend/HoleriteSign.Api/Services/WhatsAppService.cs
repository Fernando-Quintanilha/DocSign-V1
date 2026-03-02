using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HoleriteSign.Api.Services;

/// <summary>
/// Service to interact with the Evolution API for WhatsApp messaging.
/// </summary>
public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _logger;

    private string BaseUrl => _config["WhatsApp:BaseUrl"] ?? "http://evolution:8080";
    private string ApiKey => _config["WhatsApp:ApiKey"] ?? "";
    private string InstanceName => _config["WhatsApp:InstanceName"] ?? "holeritesign";

    public WhatsAppService(HttpClient http, IConfiguration config, ILogger<WhatsAppService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    private void SetHeaders()
    {
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("apikey", ApiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ── Diagnostics ──────────────────────────────────────────

    /// <summary>
    /// Run comprehensive diagnostic checks on Evolution API.
    /// </summary>
    public async Task<object> RunDiagnosticAsync()
    {
        SetHeaders();
        var results = new Dictionary<string, object?>();
        results["timestamp"] = DateTime.UtcNow.ToString("o");
        results["baseUrl"] = BaseUrl;
        results["instanceName"] = InstanceName;

        // 1. Check if Evolution API is reachable
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/");
            results["evoReachable"] = true;
            results["evoStatus"] = (int)response.StatusCode;
            var body = await response.Content.ReadAsStringAsync();
            results["evoRootResponse"] = body.Length > 500 ? body[..500] : body;
        }
        catch (Exception ex)
        {
            results["evoReachable"] = false;
            results["evoError"] = ex.Message;
        }

        // 2. Fetch all instances
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/fetchInstances");
            var body = await response.Content.ReadAsStringAsync();
            results["fetchInstancesStatus"] = (int)response.StatusCode;
            results["fetchInstancesBody"] = body.Length > 2000 ? body[..2000] : body;
        }
        catch (Exception ex)
        {
            results["fetchInstancesError"] = ex.Message;
        }

        // 3. Get connection state
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/connectionState/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();
            results["connectionStateStatus"] = (int)response.StatusCode;
            results["connectionStateBody"] = body;
        }
        catch (Exception ex)
        {
            results["connectionStateError"] = ex.Message;
        }

        // 4. Try connect endpoint (this is what generates QR)
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();
            results["connectStatus"] = (int)response.StatusCode;
            results["connectBodyLength"] = body.Length;
            // Check if base64 is present
            results["connectHasBase64"] = body.Contains("base64") && !body.Contains("\"base64\":null");
            results["connectBody"] = body.Length > 3000 ? body[..3000] : body;
        }
        catch (Exception ex)
        {
            results["connectError"] = ex.Message;
        }

        // 5. Check internet access from API container
        try
        {
            using var testHttp = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await testHttp.GetAsync("https://web.whatsapp.com");
            results["apiContainerInternetAccess"] = true;
            results["whatsappWebStatus"] = (int)response.StatusCode;
        }
        catch (Exception ex)
        {
            results["apiContainerInternetAccess"] = false;
            results["apiContainerInternetError"] = ex.Message;
        }

        // 6. Check if Evolution can reach WhatsApp web (via Evolution's internal check)
        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();
            results["connectAttempt1"] = body.Length > 1000 ? body[..1000] : body;
            
            // If first attempt returns no QR, wait and retry
            if (!body.Contains("base64") || body.Contains("\"base64\":null"))
            {
                await Task.Delay(3000);
                response = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}");
                body = await response.Content.ReadAsStringAsync();
                results["connectAttempt2"] = body.Length > 1000 ? body[..1000] : body;
            }
        }
        catch (Exception ex)
        {
            results["connectRetryError"] = ex.Message;
        }

        // 7. Full lifecycle test: delete → create → connect (with timing)
        try
        {
            // Delete existing instance
            var delResp = await _http.DeleteAsync($"{BaseUrl}/instance/delete/{InstanceName}");
            var delBody = await delResp.Content.ReadAsStringAsync();
            results["lifecycleDeleteStatus"] = (int)delResp.StatusCode;
            results["lifecycleDeleteBody"] = delBody;

            // Wait for cleanup
            await Task.Delay(3000);

            // Create fresh
            var createPayload = new
            {
                instanceName = InstanceName,
                qrcode = true,
                integration = "WHATSAPP-BAILEYS",
            };
            var createJson = System.Text.Json.JsonSerializer.Serialize(createPayload);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResp = await _http.PostAsync($"{BaseUrl}/instance/create", createContent);
            var createBody = await createResp.Content.ReadAsStringAsync();
            results["lifecycleCreateStatus"] = (int)createResp.StatusCode;
            results["lifecycleCreateHasQR"] = createBody.Contains("base64") && !createBody.Contains("\"base64\":null") && !createBody.Contains("\"base64\":\"\"");
            results["lifecycleCreateBody"] = createBody.Length > 1000 ? createBody[..1000] : createBody;

            // Try connect immediately
            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(3000);
                var connResp = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}");
                var connBody = await connResp.Content.ReadAsStringAsync();
                var hasQr = connBody.Contains("base64") && !connBody.Contains("\"base64\":null") && !connBody.Contains("\"base64\":\"\"");
                results[$"lifecycleConnect_{i+1}_status"] = (int)connResp.StatusCode;
                results[$"lifecycleConnect_{i+1}_hasQR"] = hasQr;
                results[$"lifecycleConnect_{i+1}_bodyLen"] = connBody.Length;
                results[$"lifecycleConnect_{i+1}_body"] = connBody.Length > 500 ? connBody[..500] : connBody;
                
                if (hasQr)
                {
                    results["lifecycleQRFound"] = true;
                    results["lifecycleQRFoundAtAttempt"] = i + 1;
                    break;
                }
            }

            // 8. Check if Evolution container can reach WhatsApp's WebSocket server
            // Test DNS resolution and TCP connectivity to WhatsApp's signaling servers
            try
            {
                // Ask Evolution for its internal health/debug info
                var healthResp = await _http.GetAsync($"{BaseUrl}/");
                var healthBody = await healthResp.Content.ReadAsStringAsync();
                results["evoHealth"] = healthBody;
            }
            catch (Exception ex)
            {
                results["evoHealthError"] = ex.Message;
            }

            // 9. Check connection state after all attempts  
            try
            {
                var stateResp = await _http.GetAsync($"{BaseUrl}/instance/connectionState/{InstanceName}");
                var stateBody = await stateResp.Content.ReadAsStringAsync();
                results["finalConnectionState"] = stateBody;
            }
            catch (Exception ex)
            {
                results["finalStateError"] = ex.Message;
            }

            // 10. Try fetching QR via POST to connect endpoint (some versions need POST)
            try
            {
                var postConnResp = await _http.PostAsync($"{BaseUrl}/instance/connect/{InstanceName}", null);
                var postConnBody = await postConnResp.Content.ReadAsStringAsync();
                results["connectViaPostStatus"] = (int)postConnResp.StatusCode;
                results["connectViaPostBody"] = postConnBody.Length > 1000 ? postConnBody[..1000] : postConnBody;
                results["connectViaPostHasQR"] = postConnBody.Contains("base64") && !postConnBody.Contains("\"base64\":null");
            }
            catch (Exception ex)
            {
                results["connectViaPostError"] = ex.Message;
            }

            // 11. Try /instance/connect without instance name and with query
            try
            {
                var r2 = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}?number=");
                var b2 = await r2.Content.ReadAsStringAsync();
                results["connectWithQueryStatus"] = (int)r2.StatusCode;
                results["connectWithQueryBody"] = b2.Length > 1000 ? b2[..1000] : b2;
            }
            catch (Exception ex)
            {
                results["connectWithQueryError"] = ex.Message;
            }
        }
        catch (Exception ex)
        {
            results["lifecycleError"] = ex.Message;
        }

        return results;
    }

    // ── Instance Management ────────────────────────────────

    /// <summary>
    /// Create a new WhatsApp instance in Evolution API.
    /// If instance already exists, deletes it and creates fresh to get a new QR code.
    /// Returns (response, errorMessage, rawBody) tuple.
    /// </summary>
    public async Task<(EvolutionCreateInstanceResponse? Result, string? Error, string? RawBody)> CreateInstanceAsync()
    {
        SetHeaders();

        _logger.LogInformation("Creating Evolution instance '{Instance}' at {BaseUrl}", InstanceName, BaseUrl);

        try
        {
            // First try to create
            var (createResult, createBody, createStatus) = await DoCreateInstance();

            // Instance already exists → delete it and create fresh
            if (createStatus == 403 && createBody.Contains("already in use"))
            {
                _logger.LogInformation("Instance '{Instance}' already exists, deleting and recreating...", InstanceName);
                await DeleteInstanceAsync();
                await Task.Delay(2000);
                (createResult, createBody, createStatus) = await DoCreateInstance();
            }

            if (createStatus < 200 || createStatus >= 300)
            {
                _logger.LogWarning("Evolution create failed after retry: {Status} {Body}", createStatus, createBody);
                return (null, $"Evolution API retornou {createStatus}: {createBody}", createBody);
            }

            _logger.LogInformation("Evolution create success: {Body}", createBody);

            // Parse QR from create response (flexible parsing)
            var result = ParseCreateResponse(createBody);

            // If no QR in create response, try connect endpoint with retries
            if (result.Qrcode?.Base64 == null)
            {
                _logger.LogInformation("No QR in create response, trying restart + connect approach...");
                
                // Try restart the instance to force Baileys to initialize
                try
                {
                    var restartResp = await _http.PutAsync($"{BaseUrl}/instance/restart/{InstanceName}", null);
                    var restartBody = await restartResp.Content.ReadAsStringAsync();
                    _logger.LogInformation("Restart response: {Status} {Body}", restartResp.StatusCode, restartBody);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Restart failed, continuing with connect attempts...");
                }

                // Retry up to 10 times with delays (total ~30s)
                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    var delay = attempt <= 2 ? 3000 : 2000;
                    _logger.LogInformation("Waiting {Delay}ms before connect attempt {Attempt}/10...", delay, attempt);
                    await Task.Delay(delay);
                    
                    var qr = await GetQrCodeAsync();
                    if (qr?.Base64 != null)
                    {
                        result.Qrcode = new EvolutionQrCode
                        {
                            Base64 = qr.Base64,
                            PairingCode = qr.PairingCode,
                            Code = qr.Code,
                        };
                        _logger.LogInformation("Got QR from connect endpoint on attempt {Attempt}: base64 length={Len}", attempt, qr.Base64?.Length);
                        break;
                    }
                    
                    _logger.LogInformation("Attempt {Attempt}/10: No QR yet (base64 is null)", attempt);
                }
                
                if (result.Qrcode?.Base64 == null)
                {
                    _logger.LogWarning("All 10 connect attempts returned no QR code. Baileys may not be initializing properly.");
                }
            }
            else
            {
                _logger.LogInformation("Got QR from create response: base64 length={Len}", result.Qrcode.Base64?.Length);
            }

            return (result, null, createBody);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot reach Evolution API at {BaseUrl}", BaseUrl);
            return (null, $"Não foi possível conectar ao Evolution API em {BaseUrl}: {ex.Message}", null);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Evolution API request timed out");
            return (null, "Timeout ao conectar com Evolution API. O serviço pode estar iniciando.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Evolution API instance");
            return (null, $"Erro inesperado: {ex.Message}", null);
        }
    }

    private async Task<(EvolutionCreateInstanceResponse? Result, string Body, int Status)> DoCreateInstance()
    {
        SetHeaders();

        var payload = new
        {
            instanceName = InstanceName,
            qrcode = true,
            integration = "WHATSAPP-BAILEYS",
            reject_call = false,
            groups_ignore = true,
            always_online = false,
            read_messages = false,
            read_status = false,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{BaseUrl}/instance/create", content);
        var body = await response.Content.ReadAsStringAsync();
        var status = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            var result = JsonSerializer.Deserialize<EvolutionCreateInstanceResponse>(body, JsonOpts);
            return (result, body, status);
        }

        return (null, body, status);
    }

    /// <summary>
    /// Flexibly parse the create-instance response from Evolution API.
    /// Handles different response shapes across Evolution API versions.
    /// </summary>
    private EvolutionCreateInstanceResponse ParseCreateResponse(string body)
    {
        // Try standard deserialization first
        var result = JsonSerializer.Deserialize<EvolutionCreateInstanceResponse>(body, JsonOpts);
        if (result == null)
            result = new EvolutionCreateInstanceResponse();

        result.RawBody = body;

        // If standard parsing got the QR, we're done
        if (result.Qrcode?.Base64 != null)
            return result;

        // Try flexible parsing with JsonDocument
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Look for base64 at root level
            string? base64 = null;
            string? pairingCode = null;
            string? code = null;

            if (root.TryGetProperty("base64", out var b64Prop))
                base64 = b64Prop.GetString();
            if (root.TryGetProperty("pairingCode", out var pcProp))
                pairingCode = pcProp.GetString();
            if (root.TryGetProperty("code", out var codeProp) && codeProp.ValueKind == JsonValueKind.String)
                code = codeProp.GetString();

            // Check nested qrcode object
            if (base64 == null && root.TryGetProperty("qrcode", out var qrObj))
            {
                if (qrObj.ValueKind == JsonValueKind.Object)
                {
                    if (qrObj.TryGetProperty("base64", out var qrB64))
                        base64 = qrB64.GetString();
                    if (qrObj.TryGetProperty("pairingCode", out var qrPc))
                        pairingCode = qrPc.GetString();
                    if (qrObj.TryGetProperty("code", out var qrCode))
                        code = qrCode.GetString();
                }
                else if (qrObj.ValueKind == JsonValueKind.String)
                {
                    base64 = qrObj.GetString();
                }
            }

            if (base64 != null)
            {
                result.Qrcode = new EvolutionQrCode
                {
                    Base64 = base64,
                    PairingCode = pairingCode,
                    Code = code,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to flexibly parse create response");
        }

        return result;
    }

    /// <summary>
    /// Delete a WhatsApp instance from Evolution API.
    /// </summary>
    public async Task<bool> DeleteInstanceAsync()
    {
        SetHeaders();

        try
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/instance/delete/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Evolution delete instance: {Status} {Body}", response.StatusCode, body);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Evolution API instance");
            return false;
        }
    }

    /// <summary>
    /// Get the QR code for connecting WhatsApp.
    /// </summary>
    public async Task<EvolutionQrCodeResponse?> GetQrCodeAsync()
    {
        SetHeaders();

        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/connect/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Evolution QR response: {Status} Body={Body}", response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution API QR code failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            // Try standard deserialization
            var result = JsonSerializer.Deserialize<EvolutionQrCodeResponse>(body, JsonOpts);
            if (result?.Base64 != null) return result;

            // Evolution v2 wraps QR inside root - try parsing raw JSON to find base64
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            string? base64 = null;
            string? pairingCode = null;
            string? code = null;

            if (root.TryGetProperty("base64", out var b64Prop))
                base64 = b64Prop.GetString();
            if (root.TryGetProperty("pairingCode", out var pcProp))
                pairingCode = pcProp.GetString();
            if (root.TryGetProperty("code", out var codeProp))
                code = codeProp.GetString();

            // Some versions nest under "qrcode"
            if (base64 == null && root.TryGetProperty("qrcode", out var qrObj))
            {
                if (qrObj.ValueKind == JsonValueKind.Object)
                {
                    if (qrObj.TryGetProperty("base64", out var qrB64))
                        base64 = qrB64.GetString();
                    if (qrObj.TryGetProperty("pairingCode", out var qrPc))
                        pairingCode = qrPc.GetString();
                    if (qrObj.TryGetProperty("code", out var qrCode))
                        code = qrCode.GetString();
                }
                else if (qrObj.ValueKind == JsonValueKind.String)
                {
                    base64 = qrObj.GetString();
                }
            }

            if (base64 != null)
            {
                return new EvolutionQrCodeResponse
                {
                    Base64 = base64,
                    PairingCode = pairingCode,
                    Code = code,
                };
            }

            _logger.LogWarning("QR response parsed but no base64 found. Body: {Body}", body);
            return result; // Return whatever we got
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get QR code from Evolution API");
            return null;
        }
    }

    /// <summary>
    /// Get connection status of the WhatsApp instance.
    /// </summary>
    public async Task<EvolutionConnectionState?> GetConnectionStatusAsync()
    {
        SetHeaders();

        try
        {
            var response = await _http.GetAsync($"{BaseUrl}/instance/connectionState/{InstanceName}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution API connection status failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<EvolutionConnectionState>(body, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get connection status from Evolution API");
            return null;
        }
    }

    /// <summary>
    /// Logout / disconnect the WhatsApp instance.
    /// </summary>
    public async Task<bool> LogoutInstanceAsync()
    {
        SetHeaders();

        try
        {
            var response = await _http.DeleteAsync($"{BaseUrl}/instance/logout/{InstanceName}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to logout Evolution API instance");
            return false;
        }
    }

    // ── Messaging ──────────────────────────────────────────

    /// <summary>
    /// Send a text message via WhatsApp.
    /// </summary>
    public async Task<EvolutionSendMessageResponse?> SendTextMessageAsync(string phone, string message)
    {
        SetHeaders();

        // Normalize phone: remove +, spaces, dashes
        var normalizedPhone = NormalizePhone(phone);

        var payload = new
        {
            number = normalizedPhone,
            text = message,
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _http.PostAsync($"{BaseUrl}/message/sendText/{InstanceName}", content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution API send message failed: {Status} {Body}", response.StatusCode, body);
                throw new InvalidOperationException($"Falha ao enviar WhatsApp: {response.StatusCode}");
            }

            _logger.LogInformation("WhatsApp message sent to {Phone}. Response: {Body}", normalizedPhone, body.Length > 500 ? body[..500] : body);

            try
            {
                return JsonSerializer.Deserialize<EvolutionSendMessageResponse>(body, JsonOpts);
            }
            catch (JsonException jsonEx)
            {
                // Message was sent successfully (HTTP 2xx) but response format differs
                _logger.LogWarning(jsonEx, "WhatsApp message sent but response deserialization failed for {Phone}", normalizedPhone);
                return new EvolutionSendMessageResponse
                {
                    Key = new EvolutionMessageKey { Id = "sent-parse-error" },
                    Status = "SENT"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {Phone}", normalizedPhone);
            throw new InvalidOperationException("Serviço WhatsApp indisponível. Tente novamente mais tarde.");
        }
    }

    // ── Helpers ─────────────────────────────────────────────

    private static string NormalizePhone(string phone)
    {
        // Remove +, spaces, dashes, parentheses
        return phone.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

// ── Evolution API DTOs ──────────────────────────────────────

public class EvolutionCreateInstanceResponse
{
    [JsonPropertyName("instance")]
    public EvolutionInstanceInfo? Instance { get; set; }

    [JsonPropertyName("hash")]
    public JsonElement? Hash { get; set; }

    [JsonPropertyName("qrcode")]
    public EvolutionQrCode? Qrcode { get; set; }

    /// <summary>Raw JSON body from Evolution API for debugging.</summary>
    [JsonIgnore]
    public string? RawBody { get; set; }
}

public class EvolutionInstanceInfo
{
    [JsonPropertyName("instanceName")]
    public string? InstanceName { get; set; }

    [JsonPropertyName("instanceId")]
    public string? InstanceId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public class EvolutionQrCodeResponse
{
    [JsonPropertyName("pairingCode")]
    public string? PairingCode { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("base64")]
    public string? Base64 { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class EvolutionQrCode
{
    [JsonPropertyName("pairingCode")]
    public string? PairingCode { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("base64")]
    public string? Base64 { get; set; }
}

public class EvolutionConnectionState
{
    [JsonPropertyName("instance")]
    public EvolutionInstanceInfo? Instance { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }
}

public class EvolutionSendMessageResponse
{
    [JsonPropertyName("key")]
    public EvolutionMessageKey? Key { get; set; }

    [JsonPropertyName("message")]
    public object? Message { get; set; }

    [JsonPropertyName("messageTimestamp")]
    public JsonElement? MessageTimestamp { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("messageType")]
    public string? MessageType { get; set; }

    [JsonPropertyName("source")]
    public string? Source { get; set; }
}

public class EvolutionMessageKey
{
    [JsonPropertyName("remoteJid")]
    public string? RemoteJid { get; set; }

    [JsonPropertyName("fromMe")]
    public bool FromMe { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

// ── Webhook DTOs ────────────────────────────────────────────

public class EvolutionWebhookPayload
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}
