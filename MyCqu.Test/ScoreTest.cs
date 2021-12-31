using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;
using Xunit.Abstractions;
using Xunit;
using System.Text.Json;
using System.Linq;

namespace MyCqu.Test;

public class ScoreTest
{
    private Stream scoreStream;
    private static JsonSerializerOptions options = new JsonSerializerOptions{ PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public record Score(
        string CourseName,
        string CourseCredit,
        bool PjBoo,
        string EffectiveScoreShow,
        string SessionId,
        string ExamType
    );
    private readonly ITestOutputHelper output;
    public ScoreTest(ITestOutputHelper output)
    {
        scoreStream = File.OpenRead(@"D:\Code\csharp\MyCqu\MyCqu.Test\score.json");
        this.output = output;
    }
    
    [Fact]
    public void Test1()
    {
        var json = JsonNode.Parse(scoreStream);
        List<Score> scores = new List<Score>();
        foreach (var term in json["data"].AsObject())
        {
            foreach (var item in term.Value.AsArray())
            {
                var CourseName = item["courseName"].GetValue<string>();
                var CourseCredit = item["courseCredit"].GetValue<string>();
                var PjBoo = item["pjBoo"].GetValue<bool>();
                var EffectiveScoreShow = PjBoo?item["effectiveScoreShow"].GetValue<string>():null;
                var SessionId = item["sessionId"].GetValue<string>();
                var ExamType = item["examType"].GetValue<string>();
                scores.Add(new Score(CourseName,CourseCredit, PjBoo,EffectiveScoreShow, SessionId,
                    ExamType));
            }
        }
        scores.ForEach(s => output.WriteLine(s.ToString()));
    }

    [Fact]
    public async void Test2()
    {
        var message = await JsonSerializer.DeserializeAsync<Message>(scoreStream,options);
        var data = message.Data;
        if (data == null) { return; }
        List<Score> scores = new List<Score>();
        foreach (var term in data)
        {
            foreach (var item in term.Value.AsArray())
            {
                var termScore = term.Value.AsArray().Select(item => new Score(
                        item["courseName"].GetValue<string>(),
                        item["courseCredit"].GetValue<string>(),
                        item["pjBoo"].GetValue<bool>(),
                        item["effectiveScoreShow"]?.GetValue<string>(),
                        item["sessionId"].GetValue<string>(),
                        item["examType"].GetValue<string>()
                    ));
                scores.AddRange(termScore);
            }
            
        }
        scores.ForEach(s => output.WriteLine(s.ToString()));
    }
}