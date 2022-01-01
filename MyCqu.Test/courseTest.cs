using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MyCqu.Test;

public class courseTest
{
    private readonly ITestOutputHelper output;

    public courseTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("course","date")]
    public void parseDate()
    {
        var str = "2021/8/30";
        var date = DateOnly.Parse(str);
        output.WriteLine(date.ToShortDateString());
    }
}
