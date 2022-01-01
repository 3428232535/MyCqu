using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyCqu.Models;


namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class ScoreController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ScoreController> _logger;
    private readonly IConfiguration _configuration;
    public ScoreController(IHttpClientFactory factory, ILogger<ScoreController> logger,IConfiguration configuration)
    {
        _factory = factory;
        _logger = logger;
        _configuration = configuration;
    }
    [HttpGet("All")]
    public async Task<IEnumerable<Score>> All(string jwt)
    {
        var json = await this.ScoreData(jwt);
        List<Score> scores = new List<Score>();
        foreach (var term in json.Data.AsObject())
        {
            foreach (var item in term.Value.AsArray())
            {
                //var CourseName = item["courseName"].GetValue<string>();
                //var CourseCredit = item["courseCredit"].GetValue<string>();
                //var PjBoo = item["pjBoo"].GetValue<bool>();
                //var EffectiveScoreShow = PjBoo ? item["effectiveScoreShow"].GetValue<string>() : null;
                //var SessionId = item["sessionId"].GetValue<string>();
                //var ExamType = item["examType"].GetValue<string>();
                //scores.Add(new Score(CourseName, CourseCredit, PjBoo, EffectiveScoreShow, SessionId,
                //    ExamType));
                scores.Add(JsonSerializer.Deserialize<Score>(item, Common.JsonOptions.Options));
            }
        }
        return scores;
    }

    [HttpGet("Current")]
    public async Task<IEnumerable<Score>> Current(string jwt)
    {
        var currentTerm = _configuration["CurrentTerm"];
        var json = await ScoreData(jwt);
        List<Score> scores = new List<Score>();
        var currentData = json.Data[currentTerm].AsArray();
        return currentData.Select(item => JsonSerializer.Deserialize<Score>(item,Common.JsonOptions.Options));
    }
    private async Task<HttpMessage> ScoreData(string jwt)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return await client.GetFromJsonAsync<HttpMessage>("https://my.cqu.edu.cn/api/sam/score/student/score",Common.JsonOptions.Options);
    }
    
    
}