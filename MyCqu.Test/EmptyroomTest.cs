using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MyCqu.Test;

public class EmptyroomTest
{
    private readonly ITestOutputHelper output;

    public EmptyroomTest(ITestOutputHelper output)
    {
        this.output = output;
    }
    [Fact]
    [Trait("Room","strcmp")]
    public void strcmp()
    {
        string str1 = "2";
        string str2 = "12";
        output.WriteLine(str1.CompareTo(str2).ToString());
    }
}
