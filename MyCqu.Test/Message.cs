namespace MyCqu.Test;


public record Message(
    string Code,
    string Msg,
    string Status,
    System.Text.Json.Nodes.JsonObject Data
    );