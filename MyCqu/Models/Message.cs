using System.Text.Json.Nodes;

namespace MyCqu.Models;

public class HttpMessage
{
    public string Code { get; set; }
    public string Msg { get; set; }
    public string Status { get; set; }
    public JsonObject Data { get; set; }
}