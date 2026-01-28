namespace Autoredi.Benchmarks.Services;

// Manual registration services (identical to Autoredi services but without attributes)
public class ManualServiceA : IServiceA
{
    public void DoWork() { }
}

public class ManualServiceB : IServiceB
{
    public void DoWork() { }
}

public class ManualServiceC : IServiceC
{
    public void DoWork() { }
}

public class ManualKeyedService1 : IKeyedService
{
    public string Name => "Key1";
    public void DoWork() { }
}

public class ManualKeyedService2 : IKeyedService
{
    public string Name => "Key2";
    public void DoWork() { }
}

public class ManualKeyedService3 : IKeyedService
{
    public string Name => "Key3";
    public void DoWork() { }
}

public class ManualConcreteService
{
    public Guid Id { get; } = Guid.NewGuid();
}
