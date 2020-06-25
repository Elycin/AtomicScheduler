# AtomicScheduler
Cron based scheduler for C# designed for multi-server applications.

## Features:
- Full Cron support
- Atomic locks - reliably execute tasks on a single server for each execution interval
- Overlapping prevention/mutex locks
- [ServiceStack.Redis](https://github.com/ServiceStack/ServiceStack.Redis) support.

### Working Example
```csharp
// Initialize Redis Driver
var driver = new RedisAtomicDriver("localhost:6379"); // Connection string

// Additionally, you can use one of the following from ServiceStack.Redis
// var driver = new RedisAtomicDriver(redisManagerPool); // Service Stack IRedisManagerPool
// var driver = new RedisAtomicDriver(redisClient); // Service Stack IRedisClient

// Initialize new Scheduler
var scheduler = new Scheduler();
scheduler.SetAtomicDriver(driver);

// Add tasks
scheduler.AddTask(...);
scheduler.Run();

```

### Atomic Locks / Cluster Support
If your application is running on multiple servers, you may limit a scheduled job to only execute on a single server by specifying `oneMachineOnly` as `true` when instantiating your `ScheduledTask` object.
```csharp
var task = new ScheduledTask {
  ...
  oneMachineOnly = true,
  ...
}
```

### Mutex Locking
If you are scheduling tasks that may potentially take longer than a minute and should overlap eachother, you should enable the `allowOverlapping` flag by specifying `true`.
```csharp
var task = new ScheduledTask {
  ...
  allowOverlapping = true,
  ...
}
```

### Contributing / Building Drivers
If you would like to add support for a medium that can interface with the scheduler, please extend the `IAtomicDriver` interface and make a pull request.
