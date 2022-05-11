using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    public async Task<string> Token(JwtPayload data)
    {
        var username = data.username;
        var password = data.password;
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.54 Safari/537.36 Edg/101.0.1210.39");
        await client.GetAsync("https://sso.cqu.edu.cn/login?service=https://my.cqu.edu.cn/authserver/authentication/cas");
        var serviceResponse = await client.GetAsync("https://sso.cqu.edu.cn/clientredirect?client_name=adapter&service=https://my.cqu.edu.cn/authserver/authentication/cas");
        var url = serviceResponse.Headers.Location;
        var response = await client.GetStringAsync(url);
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
        var ticketResponse = await client.PostAsync(url, ticketContent);
        if (ticketResponse.StatusCode != System.Net.HttpStatusCode.Redirect) return null;
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
        await client.GetAsync("http://authserver.cqu.edu.cn/authserver/logout?service=https%3A%2F%2Fsso.cqu.edu.cn%2Flogin%3Fservice%3Dhttps%3A%2F%2Fmy.cqu.edu.cn%2Fauthserver%2Fauthentication%2Fcas"); 
        return Regex.Match(rawToken, @"[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+").Value;
    }

    [HttpGet("Valid")]
    public async Task<bool> Valid(string jwt)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        var response = await client.GetAsync("https://my.cqu.edu.cn/api/resourceapi/session/list");
        return response.IsSuccessStatusCode;
    }

    [HttpGet("Logout")]
    public async Task Logout()
    {
        var client = _factory.CreateClient();
        await client.GetAsync("http://authserver.cqu.edu.cn/authserver/logout?service=https%3A%2F%2Fsso.cqu.edu.cn%2Flogin%3Fservice%3Dhttps%3A%2F%2Fmy.cqu.edu.cn%2Fauthserver%2Fauthentication%2Fcas");
    }
}

public record JwtPayload(string username,string password);

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
