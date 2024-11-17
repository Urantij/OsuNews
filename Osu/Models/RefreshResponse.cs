using System.Text.Json.Serialization;

namespace OsuNews.Osu.Models;

public class RefreshResponse
{
    [JsonPropertyName("token_type")] public string TokenType { get; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; }
    [JsonPropertyName("access_token")] public string AccessToken { get; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; }

    public RefreshResponse(string tokenType, int expiresIn, string accessToken, string refreshToken)
    {
        TokenType = tokenType;
        ExpiresIn = expiresIn;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
}