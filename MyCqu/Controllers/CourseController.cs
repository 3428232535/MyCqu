using Microsoft.AspNetCore.Mvc;
using MyCqu.Models;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class CourseController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<CourseController> _logger;
    private readonly IConfiguration _configuration;
    public string Session { get; private set; }
    public DateTime StartDate { get; private set; }

    public CourseController(IHttpClientFactory factory,ILogger<CourseController> logger,IConfiguration configuration)
    {
        this._factory = factory;
        this._logger = logger;
        this._configuration = configuration;
        this.Session = _configuration["Session"];
        this.StartDate = DateTime.Parse(_configuration["StartDate"]);
    }

    private async Task<JsonNode> CourseData(string jwt,string username)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        var content = JsonContent.Create(new string[] { username });
        var response = await client.PostAsync($"https://my.cqu.edu.cn/api/timetable/class/timetable/student/table-detail?sessionId={this.Session}",content);
        var stream = await response.Content.ReadAsStreamAsync();
        var root = await JsonSerializer.DeserializeAsync<JsonNode>(stream);
        return root["classTimetableVOList"];
    }
    
    [HttpGet("{username}/All")]
    public async Task<IEnumerable<Course>> AllCourses(string jwt,string username)
    {
        var courseData = await CourseData(jwt,username);
        return JsonSerializer.Deserialize<IEnumerable<Course>>(courseData,Common.JsonOptions.Options);
    }

    [HttpGet("{username}/Today")]
    public async Task<IEnumerable<Course>> TodayCourse(string jwt,string username)
    {
        var courseData = await this.CourseData(jwt,username);
        var data = JsonSerializer.Deserialize<IEnumerable<Course>>(courseData, Common.JsonOptions.Options);
        var today = DateTime.Now.Date;
        var week = (int)(today - StartDate).TotalDays / 7;
        var weekCourses = data.Where(c => c.TeachingWeek.PadRight(30, '0')[week] == '1');
        if (weekCourses.Count() <= 1) return weekCourses;
        else return weekCourses.Where(c => c.WeekDay == ((int)today.DayOfWeek).ToString())
                .OrderByDescending(c => c.Period.PadRight(12, '0'));
    }
}
