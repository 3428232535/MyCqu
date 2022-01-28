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
            var termScores = term.Value["stuScoreHomePgVoS"].AsArray();
            foreach (var item in termScores)
            { 
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
        var currentData = json.Data[currentTerm]["stuScoreHomePgVoS"].AsArray();
        return currentData.Select(item => JsonSerializer.Deserialize<Score>(item,Common.JsonOptions.Options));
    }
    private async Task<HttpMessage> ScoreData(string jwt)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        return await client.GetFromJsonAsync<HttpMessage>("https://my.cqu.edu.cn/api/sam/score/student/score",Common.JsonOptions.Options);
    }
    
    [HttpPost("Gpa")]
    public double CurrentGpa(IEnumerable<Score> scores)
    {
        var scoreItems1 = scores.Select(s => new
        {
            Credit = double.Parse(s.CourseCredit),
            Grade = double.TryParse(s.EffectiveScoreShow, out var grade) 
                ? grade : s.EffectiveScoreShow switch
            {
                "优" => 95,
                "良" or "合格" => 85,
                "中" => 75,
                "及格" => 65,
                "不及格" or _ => 0
            }
        });
        var scoreItems2 = scoreItems1.Select(s => new
        {
            Credit = s.Credit,
            GradePoint = s.Grade switch
            {
                >= 90 and <= 100 => 4.0,
                < 60 => 0,
                < 90 and >= 60 => (s.Grade - 50) / 10,
                _ => 0
            }
        });
        return scoreItems2.Sum(s => s.Credit * s.GradePoint) / scoreItems2.Sum(s => s.Credit);
    }
}