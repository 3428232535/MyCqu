namespace MyCqu.Models;

public record Score(
        string CourseName,
        string CourseCredit,
        bool PjBoo,
        string EffectiveScoreShow,
        string SessionId,
        string ExamType
    );