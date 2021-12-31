using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using MyCqu.Models;

namespace MyCqu.Controllers;

[ApiController]
[Route("[Controller]")]
public class ExamScheduleController : ControllerBase
{
    private readonly IHttpClientFactory _factory;
    private readonly ILogger<ExamScheduleController> _logger;

    public ExamScheduleController(IHttpClientFactory factory,ILogger<ExamScheduleController> logger)
    {
        _factory = factory;
        _logger = logger;
    }
    [HttpGet("StudentId/{username}")]
    public string StudentId(string username)
    {
        return AesStudentId.GetStudentId(username);
    }

    private Task<HttpMessage> ScheduleData(string jwt,string username)
    {
        var studentId = this.StudentId(username);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);
        return client.GetFromJsonAsync<HttpMessage>($"https://my.cqu.edu.cn/api/exam/examTask/get-student-exam-list-outside?studentId={studentId}");
    }
    
    [HttpGet]
    public async Task<IEnumerable<ExamSchedule>> ExamSchedules(string jwt, string username)
    {
        var json = await this.ScheduleData(jwt, username);
        var content = json.Data["Content"].AsArray();
        return content.Select(item => JsonSerializer.Deserialize<ExamSchedule>(item,Common.JsonOptions.Options));
    }
}

public static class AesStudentId
{
    private static Aes aes;

    private static void InitAes()
    {
        aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes("cquisse123456789");
    }

    public static string GetStudentId(string username)
    {
        if (aes == null) InitAes();
        var encBts = aes.EncryptEcb(Encoding.UTF8.GetBytes(username),PaddingMode.PKCS7);
        return encBts.Select(x => x.ToString("x2")).Aggregate((a,n)=> a+n).ToUpper();
    }
}