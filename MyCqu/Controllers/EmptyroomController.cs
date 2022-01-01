using Microsoft.AspNetCore.Mvc;
using MyCqu.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class EmptyroomController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<EmptyroomController> _logger;

    public EmptyroomController(IHttpClientFactory factory,ILogger<EmptyroomController> logger)
    {
        this._factory = factory;
        this._logger = logger;
    }

    
    private async Task<HttpMessage> EmptyroomData(string jwt, EmptyroomPayload payload)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        var response = await client.PostAsync("https://my.cqu.edu.cn/api/timetable/class/timetable/temp-activity/available-room", JsonContent.Create(payload));
        var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<HttpMessage>(stream,Common.JsonOptions.Options);
    }

    [HttpPost]
    public async Task<IEnumerable<EmptyRoom>> EmptyRooms(string jwt, EmptyroomPayload payload)
    {
        var emptyroomData = await EmptyroomData(jwt, payload);
        var data = emptyroomData.Data;
        return JsonSerializer.Deserialize<IEnumerable<EmptyRoom>>(data);
    }
}

public record EmptyroomPayload(
    string campusId,
    string buildingId,
    string buildingName,
    string PeriodFormat,
    string? Week,
    string? WeekTime,
    IEnumerable<string> dateList
);

