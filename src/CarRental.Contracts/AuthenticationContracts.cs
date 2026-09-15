namespace CarRental.Contracts;

public sealed record TokenRequest(string ClientId, string ClientSecret);

public sealed record TokenResponse(string AccessToken, string TokenType, int ExpiresIn);
