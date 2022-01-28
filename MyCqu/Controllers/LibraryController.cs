using Microsoft.AspNetCore.Mvc;
using MyCqu.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class LibraryController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<LibraryController> _logger;

    public LibraryController(IHttpClientFactory factory,ILogger<LibraryController> logger)
    {
        this._factory = factory;
        this._logger = logger;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(LibraryPayload data)
    {
        var client = _factory.CreateClient();
        var response = await client.GetStringAsync("http://lib.cqu.edu.cn/login?returnUrl=http%3a%2f%2flib.cqu.edu.cn%2fstart%2floginredirect%3furl%3dhttp%253a%252f%252flib.cqu.edu.cn%252f");
        var service = Regex.Match(response, "service=[\\w\\.%-]+").Value;
        var lt = Regex.Match(response, "LT-[\\w-]+").Value;
        var execution = Regex.Match(response, "(?<=name=\"execution\" value=\")\\w+").Value;
        Dictionary<string, string> paras = new()
        {
            { "username", data.Username },
            { "password", data.Password },
            { "lt", lt },
            { "_eventId", "submit" },
            { "execution", execution },
            { "way", "&" + DateTime.Now.ToString("yyyyMMddhhmm") }
        };
        var ticketContent = new FormUrlEncodedContent(paras);
        var ticketResponse = await client.PostAsync("http://sso2.lib.cqu.edu.cn:8002/cas/login?" + service, ticketContent);
        return Ok();
    }


    [HttpGet("Borrowed")]
    public async Task<IEnumerable<Book>> BorrowedBooks()
    {
        var client = _factory.CreateClient();
        var loanResponse = await client.GetStringAsync("http://lib.cqu.edu.cn/user/loan");
        var userId = Regex.Match(loanResponse, "(?<=(id=\"hfldUserId\" value=\"))(\\d+)").Value;
        var userKey = Regex.Match(loanResponse, "(?<=(id=\"hfldUserKey\" value=\"))(\\w+)").Value;
        var query = $"{{\"UserID\":\"{userId}\",\"UserKey\":\"{userKey}\"}}";
        var response = await client.GetStringAsync("http://lib.cqu.edu.cn/api/v1/user/getCurrentBorrowList?query=" + query);
        var json = JsonNode.Parse(response);
        return JsonSerializer.Deserialize<IEnumerable<Book>>(json["result"]["borrowBookList"], Common.JsonOptions.Options);
    }

    [HttpGet("Renew/{userId}/{bookId}")]
    public async Task RenewBook(string userId, string bookId)
    {
        var client = _factory.CreateClient();
        var query = $"{{\"UserId\":{userId},\"BookId\":{bookId}}}";
        await client.GetAsync("http://lib.cqu.edu.cn/api/v1/user/renew?query=" + query);
    }
}

public record LibraryPayload(string Username,string Password);