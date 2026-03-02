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
    /// If instance already exists, connects to it instead.
    /// Returns (response, errorMessage) tuple.
    /// </summary>
    public async Task<(EvolutionCreateInstanceResponse? Result, string? Error)> CreateInstanceAsync()
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

        _logger.LogInformation("Creating Evolution instance at {BaseUrl}/instance/create", BaseUrl);

        try
        {
            var response = await _http.PostAsync($"{BaseUrl}/instance/create", content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Evolution create response: {Status} {Body}", response.StatusCode, body);

            // Instance already exists — just connect to it and get QR code
            if ((int)response.StatusCode == 403 && body.Contains("already in use"))
            {
                _logger.LogInformation("Instance '{Instance}' already exists, connecting...", InstanceName);
                var qr = await GetQrCodeAsync();
                if (qr != null)
                {
                    // Build a response that mirrors a fresh create
                    return (new EvolutionCreateInstanceResponse
                    {
                        Instance = new EvolutionInstanceInfo
                        {
                            InstanceName = InstanceName,
                            Status = "created",
                        },
                        Qrcode = qr.Base64 != null ? new EvolutionQrCode
                        {
                            Base64 = qr.Base64,
                            PairingCode = qr.PairingCode,
                            Code = qr.Code,
                        } : null,
                    }, null);
                }

                // If QR also failed, check status
                var status = await GetConnectionStatusAsync();
                return (new EvolutionCreateInstanceResponse
                {
                    Instance = new EvolutionInstanceInfo
                    {
                        InstanceName = InstanceName,
                        Status = status?.State ?? "exists",
                    },
                }, null);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution API create instance failed: {Status} {Body}", response.StatusCode, body);
                return (null, $"Evolution API retornou {(int)response.StatusCode}: {body}");
            }

            var result = JsonSerializer.Deserialize<EvolutionCreateInstanceResponse>(body, JsonOpts);
            return (result, null);
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

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Evolution API QR code failed: {Status} {Body}", response.StatusCode, body);
                return null;
            }

            return JsonSerializer.Deserialize<EvolutionQrCodeResponse>(body, JsonOpts);
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
