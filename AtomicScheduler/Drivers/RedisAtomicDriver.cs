using ServiceStack;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AtomicScheduler.Drivers
{
    class RedisAtomicDriver : IAtomicDriver
    {
        readonly static string Driver = "redis";
        readonly IRedisClient redisClient;

        public RedisAtomicDriver(string connectionString)
        {
            var redisManager = new RedisManagerPool(connectionString);
            redisClient = redisManager.GetClient();
        }

        public bool Has(string key)
        {
            return redisClient.Custom(Commands.Exists, key).Text == "1";
        }

        public void Set(string key)
        {
            redisClient.Set(key, true, DateTime.Now.AddSeconds(60));
        }
    }
}
