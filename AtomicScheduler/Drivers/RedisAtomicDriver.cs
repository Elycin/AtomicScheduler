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

        /// <summary>
        /// Initialize from connection string.
        /// </summary>
        /// <param name="connectionString"></param>
        public RedisAtomicDriver(string connectionString)
        {
            var redisManager = new RedisManagerPool(connectionString);
            redisClient = redisManager.GetClient();
        }

        /// <summary>
        /// Initialize from redis client from servicestack management pool.
        /// </summary>
        /// <param name="redisManagerPool"></param>
        public RedisAtomicDriver(RedisManagerPool redisManagerPool)
        {
            redisClient = redisManagerPool.GetClient();
        }

        /// <summary>
        /// Initialize from redis client from servicestack.
        /// </summary>
        /// <param name="redisClient"></param>
        public RedisAtomicDriver(IRedisClient redisClient)
        {
            this.redisClient = redisClient;
        }

        /// <summary>
        /// Wrapper method to check if key exists.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Has(string key)
        {
            return redisClient.Custom(Commands.Exists, key).Text == "1";
        }

        /// <summary>
        /// Wrapper method to set lock.
        /// </summary>
        /// <param name="key"></param>
        public void Set(string key)
        {
            redisClient.Set(key, true, DateTime.Now.AddSeconds(60));
        }
    }
}
