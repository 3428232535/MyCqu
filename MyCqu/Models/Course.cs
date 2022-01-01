namespace MyCqu.Models;

public record Course(
    string CourseName,
    string CourseCode,
    string ClassNbr,
    string TeachingWeek,
    string? WeekDay,
    string? Period,
    bool? WholeWeekOccupy,
    string? RoomName,
    string Credit
    );
