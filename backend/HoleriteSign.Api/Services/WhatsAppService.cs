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

    // ── Instance Management ────────────────────────────────

    /// <summary>
    /// Create a new WhatsApp instance in Evolution API.
    /// If instance already exists, deletes it and creates fresh to get a new QR code.
    /// Returns (response, errorMessage) tuple.
    /// </summary>
    public async Task<(EvolutionCreateInstanceResponse? Result, string? Error)> CreateInstanceAsync()
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
                // Small delay to let Evolution clean up
                await Task.Delay(1000);
                (createResult, createBody, createStatus) = await DoCreateInstance();
            }

            if (createStatus < 200 || createStatus >= 300)
            {
                _logger.LogWarning("Evolution create failed after retry: {Status} {Body}", createStatus, createBody);
                return (null, $"Evolution API retornou {createStatus}: {createBody}");
            }

            _logger.LogInformation("Evolution create success: {Body}", createBody);
            return (createResult, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot reach Evolution API at {BaseUrl}", BaseUrl);
            return (null, $"Não foi possível conectar ao Evolution API em {BaseUrl}: {ex.Message}");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Evolution API request timed out");
            return (null, "Timeout ao conectar com Evolution API. O serviço pode estar iniciando.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create Evolution API instance");
            return (null, $"Erro inesperado: {ex.Message}");
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

            _logger.LogInformation("WhatsApp message sent to {Phone}", normalizedPhone);
            return JsonSerializer.Deserialize<EvolutionSendMessageResponse>(body, JsonOpts);
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
    public string? Hash { get; set; }

    [JsonPropertyName("qrcode")]
    public EvolutionQrCode? Qrcode { get; set; }
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
    public string? MessageTimestamp { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
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
