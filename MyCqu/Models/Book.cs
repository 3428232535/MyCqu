namespace MyCqu.Models;

public record Book(
    string BookId,
    string Title,
    string BorrowTime,
    string ShouldReturnTime,
    int RenewCount
    );
