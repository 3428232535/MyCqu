using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class CardController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<CardController> _logger;

    public CardController(IHttpClientFactory factory,ILogger<CardController> logger)
    {
        this._factory = factory;
        this._logger = logger;
    }

    private bool LoginStatus = false;

    [HttpPost("login")]
    public async Task<IActionResult> Login(JwtPayload data)
    {
        var username = data.username;
        var password = data.password;
        var client = _factory.CreateClient();
        var response = await client.GetStringAsync("http://authserver.cqu.edu.cn/authserver/login?service=http%3A%2F%2Fcard.cqu.edu.cn%3A7280%2Fias%2Fprelogin%3Fsysid%3DFWDT%26continueurl%3Dhttp%253a%252f%252fcard.cqu.edu.cn%252fcassyno%252findex");
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
        var ticketResponse = await client.PostAsync("http://authserver.cqu.edu.cn/authserver/login?service=http%3A%2F%2Fcard.cqu.edu.cn%3A7280%2Fias%2Fprelogin%3Fsysid%3DFWDT%26continueurl%3Dhttp%253a%252f%252fcard.cqu.edu.cn%252fcassyno%252findex", ticketContent);
        var ticketResponseStr = await ticketResponse.Content.ReadAsStringAsync();
        var ssoticketid = Regex.Match(ticketResponseStr, "(?<=(ssoticketid\" value=\"))\\w+").Value;
        var ssoContent = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "errorcode", "1" },
            { "continueurl", "http://card.cqu.edu.cn/cassyno/index" },
            { "ssoticketid", ssoticketid }
        });
        await client.PostAsync("http://card.cqu.edu.cn/cassyno/index", ssoContent);
        this.LoginStatus = true;
        return Ok();
    }

    [HttpGet("Balance/Card")]
    public async Task<CardBalance> CardBalance()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("http://card.cqu.edu.cn/NcAccType/GetCurrentAccountList", null);
        var contentStr = await response.Content.ReadAsStringAsync();
        var content = Regex.Unescape(contentStr);
        var json = JsonNode.Parse(content);
        return JsonSerializer.Deserialize<CardBalance>(json["objs"][0],Common.JsonOptions.Options);
    }

    private async Task<string> FeeToken()
    {
        var client = _factory.CreateClient();
        var payload = new Dictionary<string, string>()
            {
                { "flowID", "10002" },
                { "type", "1" },
                { "apptype", "4" },
                {
                    "Url",
                    "http%3a%2f%2fcard.cqu.edu.cn%3a8080%2fblade-auth%2ftoken%2fthirdToToken%2ffwdt%3freferer%3dpc"
                }
            };
        var responseMessage = await client.PostAsync("http://card.cqu.edu.cn/Page/Page", new FormUrlEncodedContent(payload));
        var content = await responseMessage.Content.ReadAsStringAsync();
        var href = Regex.Match(content, @"ticket=\w+").Value;
        var tokenResponseMessage = await client.GetAsync($"http://card.cqu.edu.cn:8080/blade-auth/token/thirdToToken/fwdt?referer=app&{href}");
        var tokenStr = tokenResponseMessage.RequestMessage.RequestUri.ToString();
        return Regex.Match(tokenStr, @"ey[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+\.[a-zA-z0-9_-]+").Value;
    }

    [HttpGet("Balance/Electricity")]
    public async Task<ElectricityBalance> ElectricityBalance(string room,string type="181")
    {
        var token = await FeeToken();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("synjones-auth", $"bearer {token}");
        var payload = new Dictionary<string, string>()
            {
                {"feeitemid", type },
                {"type","IEC" },
                {"room",room }
            };
        var response = await client.PostAsync("http://card.cqu.edu.cn:8080/charge/feeitem/getThirdData", new FormUrlEncodedContent(payload));
        var contentStr = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(contentStr);
        var data = json["map"]["data"];
        return type switch
        {
            "181" => new ElectricityBalance(data["cashBalance"].GetValue<string>(), data["subsidiesBalance"].GetValue<string>()),
            "182" => new ElectricityBalance(data["amount"].GetValue<string>(), data["eamount"].GetValue<string>()),
            _ => null
        };
    
    
    }
}





/// <summary>
/// 
/// </summary>
/// <param name="AcctAmt">一卡通</param>
/// <param name="CardBal">卡账户</param>
public record CardBalance(int AcctAmt,int CardBal);


/// <summary>
/// 
/// </summary>
/// <param name="CashBalance">现金余额</param>
/// <param name="SubsidiesBalance">补贴余额</param>
public record ElectricityBalance(string CashBalance,string SubsidiesBalance);