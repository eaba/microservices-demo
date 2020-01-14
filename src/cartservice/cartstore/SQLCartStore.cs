// Copyright 2018 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cartservice.interfaces;
using Google.Protobuf;
using Grpc.Core;
using Hipstershop;
//npgsql added to test YBDB connectivity
using Npgsql;
//https://stackexchange.github.io/StackExchange.Redis/Basics.html
//using StackExchange.Redis;

namespace cartservice.cartstore
{
    public class SQLCartStore : ICartStore
    {
        private const string CART_FIELD_NAME = "cart";
        // private const int SQL_RETRY_NUM = 5;

        // private volatile bool isSQLConnectionOpened = false;

        private readonly object locker = new object();
        private readonly byte[] emptyCartBytes;
        private readonly string connectionString; 
        // Build connection string using parameters from yaml
        private static string User = "postgres";
        private static string DBname = "sample";
        private static string Password = "postgres";
        private static string Port = "5433";

        //private readonly ConfigurationOptions sqlConnectionOptions;

        public SQLCartStore(string ysqlAddress)
        {
            // Serialize empty cart into byte array.
            var cart = new Hipstershop.Cart();
            emptyCartBytes = cart.ToByteArray();
            connectionString = 
                String.Format(
                    "Server={0};Username={1};Database={2};Port={3};Password={4};",
                    ysqlAddress,
                    User,
                    DBname,
                    Port,
                    Password);
            /*
            redisConnectionOptions = ConfigurationOptions.Parse(connectionString);

            // Try to reconnect if first retry failed (up to 5 times with exponential backoff)
            redisConnectionOptions.ConnectRetry = REDIS_RETRY_NUM;
            redisConnectionOptions.ReconnectRetryPolicy = new ExponentialRetry(100);

            redisConnectionOptions.KeepAlive = 180;
            */
        }

        public Task InitializeAsync()
        { 
            Console.WriteLine("Initialized");
            return Task.CompletedTask;
            /*
            EnsureRedisConnected();
            return Task.CompletedTask;
            */
        }

        private void EnsureRedisConnected()
        {
            Console.WriteLine("Connected");
            return;
            /*
            if (isRedisConnectionOpened)
            {
                return;
            }

            // Connection is closed or failed - open a new one but only at the first thread
            lock (locker)
            {
                if (isRedisConnectionOpened)
                {
                    return;
                }

                Console.WriteLine("Connecting to Redis: " + connectionString);
                redis = ConnectionMultiplexer.Connect(redisConnectionOptions);

                if (redis == null || !redis.IsConnected)
                {
                    Console.WriteLine("Wasn't able to connect to redis");

                    // We weren't able to connect to redis despite 5 retries with exponential backoff
                    throw new ApplicationException("Wasn't able to connect to redis");
                }

                Console.WriteLine("Successfully connected to Redis");
                var cache = redis.GetDatabase();

                Console.WriteLine("Performing small test");
                cache.StringSet("cart", "OK" );
                object res = cache.StringGet("cart");
                Console.WriteLine($"Small test result: {res}");

                redis.InternalError += (o, e) => { Console.WriteLine(e.Exception); };
                redis.ConnectionRestored += (o, e) =>
                {
                    isRedisConnectionOpened = true;
                    Console.WriteLine("Connection to redis was retored successfully");
                };
                redis.ConnectionFailed += (o, e) =>
                {
                    Console.WriteLine("Connection failed. Disposing the object");
                    isRedisConnectionOpened = false;
                };

                isRedisConnectionOpened = true;
            }
            */
        }

        public Task AddItemAsync(string userId, string productId, int quantity)
        {
            Console.WriteLine($"AddItemAsync called with userId={userId}, productId={productId}, quantity={quantity}");
            using (var conn = new NpgsqlConnection(connectionString))
            try
            {
                conn.Open();
                using (var command = new NpgsqlCommand("INSERT INTO orders (name, quantity) VALUES (@n1, @q1), (@n2, @q2), (@n3, @q3)", conn))
                    {
                        command.Parameters.AddWithValue("n1", "banana");
                        command.Parameters.AddWithValue("q1", 150);
                        command.Parameters.AddWithValue("n2", "orange");
                        command.Parameters.AddWithValue("q2", 154);
                        command.Parameters.AddWithValue("n3", "apple");
                        command.Parameters.AddWithValue("q3", 100);
                    
                        int nRows = command.ExecuteNonQuery();
                        Console.Out.WriteLine(String.Format("Number of rows inserted={0}", nRows));
                    }
                // EnsureRedisConnected();

                // var db = redis.GetDatabase();

                // Access the cart from the cache
                //var value = await db.HashGetAsync(userId, CART_FIELD_NAME);
                var value = userId;
                Hipstershop.Cart cart;
                cart = new Hipstershop.Cart();
                cart.UserId = userId;
                cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                /* if (value.IsNull)
                {
                    cart = new Hipstershop.Cart();
                    cart.UserId = userId;
                    cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                }
                else
                {
                    cart = Hipstershop.Cart.Parser.ParseFrom(value);
                    var existingItem = cart.Items.SingleOrDefault(i => i.ProductId == productId);
                    if (existingItem == null)
                    {
                        cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                    }
                    else
                    {
                        existingItem.Quantity += quantity;
                    }
                }
                */
                //await db.HashSetAsync(userId, new[]{ new HashEntry(CART_FIELD_NAME, cart.ToByteArray()) });
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
            return Task.CompletedTask;
        }

        public Task EmptyCartAsync(string userId)
        {
            Console.WriteLine($"EmptyCartAsync called with userId={userId}");
            return Task.CompletedTask;
            /*
            try
            {
                EnsureRedisConnected();
                var db = redis.GetDatabase();

                // Update the cache with empty cart for given user
                await db.HashSetAsync(userId, new[] { new HashEntry(CART_FIELD_NAME, emptyCartBytes) });
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
            */
        }

        public async Task<Hipstershop.Cart> GetCartAsync(string userId)
        {
            Console.WriteLine($"GetCartAsync called with userId={userId}");
            try
            {
                //EnsureRedisConnected();

                //var db = redis.GetDatabase();

                // Access the cart from the cache
                //var value = await db.HashGetAsync(userId, CART_FIELD_NAME);

                /*if (!value.IsNull)
                {
                    return Hipstershop.Cart.Parser.ParseFrom(value);
                }*/

                // We decided to return empty cart in cases when user wasn't in the cache before
                //var value = new byte[] { 0x20, 0x20, 0x20, 0x20, 0x20, 0x20, 0x20 };
                //return Hipstershop.Cart.Parser.ParseFrom(value);
                return new Hipstershop.Cart();
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
            
        }

        public bool Ping()
        {
            Console.WriteLine("Pinged");
            return true;
            /*
            try
            {
                var cache = redis.GetDatabase();
                var res = cache.Ping();
                return res != TimeSpan.Zero;
            }
            catch (Exception)
            {
                return false;
            }
            */
        }
    }
}