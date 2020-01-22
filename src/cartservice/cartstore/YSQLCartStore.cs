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
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using cartservice.interfaces;
using Google.Protobuf;
using Grpc.Core;
using Hipstershop;
using Npgsql;

namespace cartservice.cartstore
{
    public class YSQLCartStore : ICartStore
    {
        private const string CART_FIELD_NAME = "cart";
        private const int YSQL_RETRY_NUM = 5;

        private readonly object locker = new object();
        private readonly byte[] emptyCartBytes;
        private readonly string ysqlconnectionString;

        public YSQLCartStore(string ysqlAddress)
        {
            // Serialize empty cart into byte array.
            var cart = new Hipstershop.Cart();
            emptyCartBytes = cart.ToByteArray();
            ysqlconnectionString = $"Server={ysqlAddress};Port=5433;Database=sample;User Id=yugabyte;Password=yugabyte;No Reset On Close=true;Pooling=true;";
        }

        public Task InitializeAsync()
        {
            
            return Task.CompletedTask;
        }

        public async Task AddItemAsync(string userId, string productId, int quantity)
        {
            Console.WriteLine($"AddItemAsync called with userId={userId}, productId={productId}, quantity={quantity}");

            try
            {
                Hipstershop.Cart cart = new Hipstershop.Cart();
                int count = 0;
                bool existingitem = false;
                bool emptycart = true;

                // Access cart from YSQL
                using (var conn = new NpgsqlConnection(ysqlconnectionString))
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand("SELECT * FROM carts WHERE userid = @n", conn))
                    {
                        command.Parameters.AddWithValue("n", userId);
                        var reader = command.ExecuteReader();
                        cart.UserId = userId;
                        while (reader.Read())
                        {
                            emptycart = false;
                            Console.Out.WriteLine("add to cart " + reader.GetString(2) + " q " + reader.GetInt32(3).ToString());
                            if (productId == reader.GetString(2))
                            {
                                count++;
                                existingitem = true;
                                quantity += reader.GetInt32(3);
                                // ready to add new quantity to object
                                cart.Items.Add(new Hipstershop.CartItem { ProductId = reader.GetString(2), Quantity = quantity });
                            }
                            else
                            {
                                // adding old quantity back into object
                                cart.Items.Add(new Hipstershop.CartItem { ProductId = reader.GetString(2), Quantity = reader.GetInt32(3) });
                            }
                        }
                    }
                    conn.Close();
                }
                //cart is prepped
                if (existingitem)
                {
                    using (var conn = new NpgsqlConnection(ysqlconnectionString))
                    {
                        conn.Open();
                        using (var command = new NpgsqlCommand("UPDATE carts SET quantity = @q WHERE userId = @n AND productId = @p", conn))
                        {
                            command.Parameters.AddWithValue("n", userId);
                            command.Parameters.AddWithValue("p", productId);
                            command.Parameters.AddWithValue("q", quantity);
                    
                            int nRows = command.ExecuteNonQuery();
                            Console.Out.WriteLine(String.Format("Number of rows updated={0}", nRows));
                        }
                        conn.Close();
                    }
                }
                else // new item
                {
                    if (emptycart)
                    {
                        // add new product and quantity into object
                        cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity }); 
                    }
                    using (var conn = new NpgsqlConnection(ysqlconnectionString))
                    {
                        conn.Open();
                        cart.Items.Add(new Hipstershop.CartItem { ProductId = productId, Quantity = quantity });
                        using (var command = new NpgsqlCommand("INSERT INTO carts (userId, productId, quantity) VALUES (@n1, @p1, @q1)", conn))
                        {
                            command.Parameters.AddWithValue("n1", userId);
                            command.Parameters.AddWithValue("p1", productId);
                            command.Parameters.AddWithValue("q1", quantity);
                            int nRows = command.ExecuteNonQuery();
                            Console.Out.WriteLine(String.Format("Number of rows inserted={0}", nRows));
                        }
                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
        }

        public async Task EmptyCartAsync(string userId)
        {
            Console.WriteLine($"EmptyCartAsync called with userId={userId}");
            try
            {
                // Update the cache with empty cart from YSQL
                using (var conn = new NpgsqlConnection(ysqlconnectionString))
                {
                    conn.Open();

                    using (var command = new NpgsqlCommand("DELETE FROM carts WHERE userid = @n", conn))
                    {
                        command.Parameters.AddWithValue("n", userId);
                        int nRows = command.ExecuteNonQuery();
                        Console.Out.WriteLine(String.Format("Number of rows deleted={0}", nRows));
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
        }

        public async Task<Hipstershop.Cart> GetCartAsync(string userId)
        {
            Console.WriteLine($"GetCartAsync called with userId={userId}");

            try
            {
                Hipstershop.Cart cart = new Hipstershop.Cart();
                // Access cart from YSQL
                using (var conn = new NpgsqlConnection(ysqlconnectionString))
                {
                    conn.Open();
                    using (var command = new NpgsqlCommand("SELECT * FROM carts WHERE userid = @n", conn))
                    {
                        command.Parameters.AddWithValue("n", userId);
                        var reader = command.ExecuteReader();
                        int count = 0;
                        cart.UserId = userId;
                        while (reader.Read())
                        {
                            count++;
                            Console.Out.WriteLine("get cart " + reader.GetString(2) + " q " + reader.GetInt32(3).ToString());
                            cart.Items.Add(new Hipstershop.CartItem { ProductId = reader.GetString(2), Quantity = reader.GetInt32(3) });
                        }
                        if (count == 0)
                        {
                            conn.Close();
                            return new Hipstershop.Cart();
                        }
                    }
                    conn.Close();
                    return cart;
                }
            }
            catch (Exception ex)
            {
                throw new RpcException(new Status(StatusCode.FailedPrecondition, $"Can't access cart storage. {ex}"));
            }
        }

        public bool Ping()
        {
            // need to add separate YSQL service validation health check compatible code
            return true;
            /*try
            {
                var cache = ysql.GetDatabase();
                var res = cache.Ping();
                return res != TimeSpan.Zero;
            }
            catch (Exception)
            {
                return false;
            }*/
        }
    }
}
