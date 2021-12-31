using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MyCqu.Test;

public class jwttest
{
    private readonly ITestOutputHelper output;
    private readonly Aes aes;
    public jwttest(ITestOutputHelper output)
    {
        this.output = output;
        aes = Aes.Create();
        aes.KeySize = 128;
        aes.Key = Encoding.UTF8.GetBytes("FeMXkTKCdwjFwP6Z");
    }

    [Fact]
    [Trait("jwt","basicAuth")]
    public void basicAuth()
    {
        var client_id = "enroll-prod";
        var client_secret = "app-a-1234";
        output.WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{client_id}:{client_secret}")));
    }

    [Fact]
    [Trait("jwt","AES1")]
    public void AES1()
    {
        var text = Encoding.UTF8.GetBytes(new String('A',64)+"test");
        var resultAes = aes.EncryptCbc(text, Encoding.UTF8.GetBytes(new String('A', 16)));
        var result = Convert.ToBase64String(resultAes);
        output.WriteLine(result);
        var target = "zaJ+fXMadXWRNpf4JYDe8YvpMgDrf/JrlM9fGp3SYG/0ut1uHPJEqKKt6snhV7U0iKYU4PALuRgu3ZFloH8Ahm2UL6eWp5sq2mb3LoH9tiY=";
    }

}
