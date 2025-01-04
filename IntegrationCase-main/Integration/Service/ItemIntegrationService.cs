using Integration.Common;
using Integration.Backend;
using System.Collections.Concurrent;
using StackExchange.Redis;

namespace Integration.Service;

public sealed class ItemIntegrationService
{
    //This is a dependency that is normally fulfilled externally.
    private ItemOperationBackend ItemIntegrationBackend { get; set; } = new();
    // For Single Server Scenario
    private ConcurrentDictionary<string, object> _locks = new();
    // For Distributed System Scenariog
    private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("localhost:6379");
    private static readonly IDatabase Db = Redis.GetDatabase();


    // This is called externally and can be called multithreaded, in parallel.
    // More than one item with the same content should not be saved. However,
    // calling this with different contents at the same time is OK, and should
    // be allowed for performance reasons.
    public Result SaveItem(string itemContent, bool useDistributedLock = false)
    {
        if (useDistributedLock)
        {
            return SaveItemDistributed(itemContent);
        }
        else
        {
            return SaveItemSingleServer(itemContent);
        }  
    }

    // Single Server Scenario
    private Result SaveItemSingleServer(string itemContent)
    {
        var lockObject = _locks.GetOrAdd(itemContent, new object());

        lock (lockObject)
        {
            // Check the backend to see if the content is already saved.
            if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
            {
                return new Result(false, $"Duplicate item received with content {itemContent}.");
            }

            var item = ItemIntegrationBackend.SaveItem(itemContent);

            // Remove the lock object after saving
            _locks.TryRemove(itemContent, out _);

            return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
        }
    }

    // Distributed System Scenario
    private Result SaveItemDistributed(string itemContent)
    {
        var lockKey = $"lock:{itemContent}";
        var lockToken = Guid.NewGuid().ToString();

        try
        {
            // Try to get the lock
            if (Db.LockTake(lockKey, lockToken, TimeSpan.FromSeconds(40)))
            {
                // Check the backend to see if the content is already saved.
                if (ItemIntegrationBackend.FindItemsWithContent(itemContent).Count != 0)
                {
                    return new Result(false, $"Duplicate item received with content {itemContent}.");
                }

                var item = ItemIntegrationBackend.SaveItem(itemContent);

                return new Result(true, $"Item with content {itemContent} saved with id {item.Id}");
            }
            else
            {
                return new Result(false, $"Could not acquire lock for content {itemContent}. Try again later.");
            }
        }
        finally
        {
            // Release the lock
            Db.LockRelease(lockKey, lockToken);
        }
    }

    public List<Item> GetAllItems()
    {
        return ItemIntegrationBackend.GetAllItems();
    }
}