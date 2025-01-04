using Integration.Service;

namespace Integration;

public abstract class Program
{
    public static void Main(string[] args)
    {
        var service = new ItemIntegrationService();

        // Demonstrate Single Server Scenario
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("a", useDistributedLock: false).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("b", useDistributedLock: false).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("c", useDistributedLock: false).Message));

        Thread.Sleep(500);

        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("a", useDistributedLock: false).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("b", useDistributedLock: false).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("c", useDistributedLock: false).Message));

        Thread.Sleep(5000);

        Console.WriteLine("Single Server Scenario - Everything recorded:");
        service.GetAllItems().ForEach(Console.WriteLine);

        // Demonstrate Distributed System Scenario
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("a", useDistributedLock: true).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("b", useDistributedLock: true).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("c", useDistributedLock: true).Message));

        Thread.Sleep(500);

        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("a", useDistributedLock: true).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("b", useDistributedLock: true).Message));
        ThreadPool.QueueUserWorkItem(_ => Console.WriteLine(service.SaveItem("c", useDistributedLock: true).Message));

        Thread.Sleep(5000);

        Console.WriteLine("Distributed System Scenario - Everything recorded:");
        service.GetAllItems().ForEach(Console.WriteLine);

        Console.ReadLine();
    }
}