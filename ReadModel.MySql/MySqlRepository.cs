using Domain;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ReadModel.Interfaces;
using ReadModel.Queries;
using Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace ReadModel.MySql
{
    public class MySqlRepository : IReadModelRepository
    {
        // Make sure you set this correctly in the Client Visual Studio Project settings
        private const string CONNECTION_STRING_NAME = "MY_SQL_DB_CONNECTION_STRING"; // This would be populated with the env variable of the Lambda

        public MySqlRepository()
        {
        }

        public void Handle(Event e)
        {
            using (MySqlConnection connection = new MySqlConnection(Environment.GetEnvironmentVariable(CONNECTION_STRING_NAME)))
            {
                connection.Open();
                try
                {
                    ProcessDomainEvent(e, connection);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }
        }

        private void ProcessDomainEvent(Event e, MySqlConnection connection)
        {
            Console.WriteLine("Processing domain event: " + JsonConvert.SerializeObject(e));

            if (e is SignedUp)
                Handle((SignedUp)e, connection);
            //else if (e is ProblemOccured)
            //    Handle((ProblemOccured)e, connection);
            else if (e is CustomerCharged)
                Handle((CustomerCharged)e, connection);
            //else if (e is CustomerSnapshotCreated)  // Don't want to process this event
            //    Handle((CustomerSnapshotCreated)e, connection);
            else
                Console.WriteLine("Domain event not registered for processing: " + JsonConvert.SerializeObject(e));
        }

        public string Handle(Query q)
        {
            Console.WriteLine("Processing query: " + JsonConvert.SerializeObject(q));

            string result = "";

            if (q is QueryCustomerDetails)
                result = Handle((QueryCustomerDetails)q);
            else if (q is QueryCustomerList)
                result = Handle((QueryCustomerList)q);
            else
                Console.WriteLine("Query not registered for processing: " + JsonConvert.SerializeObject(q));

            return result;
        }

        private void Handle(SignedUp e, MySqlConnection connection)
        {
            Console.WriteLine("Processing domain event " + JsonConvert.SerializeObject(e));

            var sql = "select count(*) from CustomerDetails where CustomerId = '" + e.CustomerId.Id + "'";
            if (RecordExists(sql, connection))
            {
                Console.WriteLine("Customer already exists, CustomerId: " + e.CustomerId.Id);
                return;
            }

            MySqlTransaction tr = connection.BeginTransaction();
            var cmd = connection.CreateCommand();
            try
            {
                cmd.CommandText = "INSERT INTO CustomerDetails(" +
                                    "CustomerId,Created,Email,FirstName,LastName,Version) VALUES(" +
                                    "@CustomerId,@Created,@Email,@FirstName,@LastName,@Version)";
                cmd.Parameters.AddWithValue("@CustomerId", e.CustomerId.Id);
                cmd.Parameters.AddWithValue("@Created", e.Created);
                cmd.Parameters.AddWithValue("@Email", e.CustomerId.Id);
                cmd.Parameters.AddWithValue("@FirstName", e.FirstName);
                cmd.Parameters.AddWithValue("@LastName", e.Lastname);
                cmd.Parameters.AddWithValue("@Version", e.Version);
                cmd.ExecuteNonQuery();

                cmd = connection.CreateCommand();
                cmd.CommandText = "INSERT INTO CustomerList(" +
                                  "CustomerId,FirstName,LastName,Version) VALUES(" +
                                  "@CustomerId,@FirstName,@LastName,@Version)";
                cmd.Parameters.AddWithValue("@CustomerId", e.CustomerId.Id);
                cmd.Parameters.AddWithValue("@FirstName", e.FirstName);
                cmd.Parameters.AddWithValue("@LastName", e.Lastname);
                cmd.Parameters.AddWithValue("@Version", e.Version);
                cmd.ExecuteNonQuery();

                tr.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                tr.Rollback();
                throw;
            }
        }

        private void Handle(ProblemOccured e, MySqlConnection connection)
        {
            Console.WriteLine("Processing domain event " + JsonConvert.SerializeObject(e));
        }

        private bool RecordExists(string sql, MySqlConnection connection)
        {
            var exists = false;
            try
            {
                var cmd = connection.CreateCommand();
                cmd.CommandText = sql;
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    var count = reader[0];
                    if (count != null)
                        exists = (long)count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(ex.StackTrace);
                throw;
            }
            return exists;
        }

        private void Handle(CustomerCharged e, MySqlConnection connection)
        {
            Console.WriteLine("Processing domain event " + JsonConvert.SerializeObject(e));

            decimal accountBalance = GetAccountBalance(e.CustomerId.Id);
            accountBalance = accountBalance + e.Amount.Amount;

            MySqlTransaction tr = connection.BeginTransaction();
            var cmd = connection.CreateCommand();
            try
            {
                cmd.CommandText = "UPDATE CustomerDetails SET " +
                                  "AccountBalance = " + accountBalance +
                                  ", Version = " + e.Version +
                                  " WHERE CustomerId = '" + e.CustomerId.Id + "'";
                cmd.ExecuteNonQuery();

                cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE CustomerList SET " +
                                  "AccountBalance = " + accountBalance +
                                  ", Version = " + e.Version +
                                  " WHERE CustomerId = '" + e.CustomerId.Id + "'";
                cmd.ExecuteNonQuery();

                tr.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                tr.Rollback();
                throw;
            }
        }

        private decimal GetAccountBalance(string customerId)
        {
            decimal result = 0;

            using (MySqlConnection connection = new MySqlConnection(Environment.GetEnvironmentVariable(CONNECTION_STRING_NAME)))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                try
                {
                    cmd.CommandText = "select * from CustomerDetails where CustomerId = '" + customerId + "'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows)
                        {
                            if (!Convert.IsDBNull(reader["AccountBalance"]))
                                result = Convert.ToDecimal(reader["AccountBalance"]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    throw;
                }
            }

            return result;
        }

        private CustomerListItem CreateCustomerListItem(MySqlDataReader reader)
        {
            CustomerListItem item =
                new CustomerListItem
                {
                    CustomerId = Convert.ToString(reader["CustomerId"]),
                    AccountBalance = Convert.ToDecimal(reader["AccountBalance"]),
                    FirstName = Convert.ToString(reader["FirstName"]),
                    LastName = Convert.ToString(reader["LastName"]),
                    Version = Convert.ToInt32(reader["Version"])
                };
            return item;
        }

        private string Handle(QueryCustomerDetails q)
        {
            CustomerDetails customerDetails = new CustomerDetails();

            using (MySqlConnection connection = new MySqlConnection(Environment.GetEnvironmentVariable(CONNECTION_STRING_NAME)))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                try
                {
                    cmd.CommandText = "select * from CustomerDetails where CustomerId = '" + q.CustomerId + "'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        if (reader.HasRows)
                        {
                            customerDetails.CustomerId = Convert.ToString(reader["CustomerId"]);
                            if (!Convert.IsDBNull(reader["AccountBalance"]))
                                customerDetails.AccountBalance = Convert.ToDecimal(reader["AccountBalance"]);
                            customerDetails.Created = (DateTime)reader["Created"];
                            if (!Convert.IsDBNull(reader["Delinquent"]))
                                customerDetails.Delinquent = Convert.ToBoolean(reader["Delinquent"]);
                            if (!Convert.IsDBNull(reader["Description"]))
                                customerDetails.Description = Convert.ToString(reader["Description"]);
                            customerDetails.Email = Convert.ToString(reader["Email"]);
                            customerDetails.FirstName = Convert.ToString(reader["FirstName"]);
                            customerDetails.LastName = Convert.ToString(reader["LastName"]);
                            customerDetails.Version = Convert.ToInt32(reader["Version"]);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            return JsonConvert.SerializeObject(customerDetails, Formatting.Indented);
        }

        private string Handle(QueryCustomerList q)
        {
            List<CustomerListItem> list = new List<CustomerListItem>();

            using (MySqlConnection connection = new MySqlConnection(Environment.GetEnvironmentVariable(CONNECTION_STRING_NAME)))
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                try
                {
                    cmd.CommandText = "select * from CustomerList order by LastName desc limit 1000";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(CreateCustomerListItem(reader));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            return JsonConvert.SerializeObject(list, Formatting.Indented);
        }

    }
}
