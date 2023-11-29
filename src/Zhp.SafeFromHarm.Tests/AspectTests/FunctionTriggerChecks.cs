using Microsoft.Azure.Functions.Worker;
using System.Reflection;

namespace Zhp.SafeFromHarm.Tests.AspectTests;

public class FunctionTriggerChecks
{
    [Fact]
    public void NoRunOnStartup()
    {
        Assembly.GetAssembly(typeof(Program))
            .Types().ThatAreNotAbstract()
            .Methods().ThatAreDecoratedWith<FunctionAttribute>()
            .SelectMany(m => m.GetParameters())
            .SelectMany(p => p.GetCustomAttributes<TimerTriggerAttribute>())
            .Should()
            .AllSatisfy(t => t.RunOnStartup.Should().BeFalse());
    }
}
