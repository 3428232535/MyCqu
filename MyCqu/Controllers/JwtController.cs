using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class JwtController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<JwtController> _logger;

    public JwtController(IHttpClientFactory factory, ILogger<JwtController> logger)
    {
        _factory = factory;
        _logger = logger;
    }


    [HttpPost("Token")]
    public async Task<string> Token(JwtPostItem data)
    {
        var username = data.username;
        var password = data.password;
        var client = _factory.CreateClient();
        var response = await client.GetStringAsync("http://authserver.cqu.edu.cn/authserver/login?service=http://my.cqu.edu.cn/authserver/authentication/cas");
        var key = Regex.Match(response, "(?<=Salt = \")\\w+").Value;
        var lt = Regex.Match(response, "LT-\\S+-cas").Value;
        var execution = Regex.Match(response, "(?<=name=\"execution\" value=\")\\w+").Value;
        var ticketContent = new FormUrlEncodedContent(new Dictionary<string, string>() {
                { "username", username },
                { "password", AuthAes.EncryptAes(password, key) },
                { "lt", lt },
                { "dllt", "userNamePasswordLogin" },
                { "_eventId", "submit" },
                { "execution", execution },
                { "rmShown", "1" }
            });
        var ticketResponse = await client.PostAsync("http://authserver.cqu.edu.cn/authserver/login?service=http://my.cqu.edu.cn/authserver/authentication/cas", ticketContent);
        if (ticketResponse.StatusCode != System.Net.HttpStatusCode.Redirect) return null;
        await client.GetAsync(ticketResponse.RequestMessage.RequestUri);
        var codeResponse = await client.GetAsync("https://my.cqu.edu.cn/authserver/oauth/authorize?client_id=enroll-prod&response_type=code&scope=all&state=&redirect_uri=https://my.cqu.edu.cn/enroll/token-index");
        var code = Regex.Match(codeResponse.RequestMessage.RequestUri.ToString(), "(?<=code=)\\w+").Value;
        var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "client_id", "enroll-prod" },
                { "client_secret", "app-a-1234" },
                { "code", code },
                { "redirect_uri", "https://my.cqu.edu.cn/enroll/token-index" },
                { "grant_type", "authorization_code" }
            });
        var tokenResponse = await client.PostAsync("https://my.cqu.edu.cn/authserver/oauth/token", tokenContent);
        var rawToken = await tokenResponse.Content.ReadAsStringAsync();
        return Regex.Match(rawToken, @"[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+").Value;
    }
}

public record JwtPostItem(string username,string password);

internal static class AuthAes
{
    private static readonly string aes_chars = "ABCDEFGHJKMNPQRSTWXYZabcdefhijkmnprstwxyz2345678";
    private static readonly int aes_chars_len = aes_chars.Length;
    private static readonly Random random = new();
    private static readonly Aes aes = Aes.Create();
    public static string EncryptAes(string data, string aesKey) =>
        string.IsNullOrEmpty(aesKey) ? data : AES(RandomString(64) + data, aesKey, RandomString(16));
    public static string RandomString(int len) =>
        Enumerable.Repeat("", len).
            Aggregate("", (str, _) =>
                str += aes_chars[(int)(random.NextDouble() * aes_chars_len)]);

    public static string AES(string data, string key, string iv)
    {
        aes.KeySize = 128;
        aes.Key = Encoding.UTF8.GetBytes(key);
        var encBts = aes.EncryptCbc(Encoding.UTF8.GetBytes(data), Encoding.UTF8.GetBytes(iv));
        return Convert.ToBase64String(encBts);
    }
}
