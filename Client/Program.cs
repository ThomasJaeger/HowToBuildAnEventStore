using Commands;
using FakeStuff;
using Shared;
using System;

namespace Client
{
    class Program
    {
        private static string _customerId;

        static void Main(string[] args)
        {
            _customerId = Environment.GetEnvironmentVariable("CUSTOMER_ID");
            ShowMenu();
            WaitForMenuSelection();
        }

        private static void ShowMenu()
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("How to build an event store sample client");
            Console.WriteLine("=========================================");
            Console.WriteLine("[1] Sign up");
            Console.WriteLine("[2] Charge Customer $50");
            Console.WriteLine("[3] Create Customer Snapshot");
            Console.WriteLine("-----------------------------------------");
            Console.WriteLine("[?] Refresh menu");
            Console.WriteLine("[0] Exit");
            Console.WriteLine();
        }

        private static void WaitForMenuSelection()
        {
            ConsoleKeyInfo key = new ConsoleKeyInfo();
            do
            {
                key = Console.ReadKey();
                switch (key.KeyChar)
                {
                    case '1':
                        {
                            SignUp();
                            PressAnyKey();
                            break;
                        }
                    case '2':
                        {
                            ChargeCustomer(50);
                            PressAnyKey();
                            break;
                        }
                    case '3':
                        {
                            CreateCustomerSnapshot();
                            PressAnyKey();
                            break;
                        }
                    case '?':
                        {
                            PressAnyKey();
                            break;
                        }
                }

            } while (key.KeyChar != '0');
        }

        private static void PressAnyKey()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
            ShowMenu();
        }

        private static void SignUp()
        {
            // 1. Create command
            MessageCreateOptions mco = new MessageCreateOptions();
            mco.Created = DateTime.Now;
            mco.CustomerId = new CustomerId(_customerId);

            SignUp signUp = new SignUp(mco);
            signUp.Email = "test@gmail.com";
            signUp.FirstName = "Thomas";
            signUp.LastName = "Jaeger";
            signUp.Password = "password";

            // 2. Send command to the Internet
            TheInternet.Enqueue(signUp);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

        private static void ChargeCustomer(decimal amount)
        {
            // 1. Create command
            MessageCreateOptions mco = new MessageCreateOptions();
            mco.Created = DateTime.Now;
            mco.CustomerId = new CustomerId(_customerId);

            // We did get some DTO from the ReadModel about the customer
            // ...
            // For testing purposes, we get the latest version of the aggregate
            // from the backend.
            int version = TheBackend.GetAggregateVersion(_customerId);

            ChargeCustomer chargeCustomer = new ChargeCustomer(amount, version, mco);

            // 2. Send command to the Internet
            TheInternet.Enqueue(chargeCustomer);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

        private static void CreateCustomerSnapshot()
        {
            // 1. Create command
            MessageCreateOptions mco = new MessageCreateOptions();
            mco.Created = DateTime.Now;
            mco.CustomerId = new CustomerId(_customerId);

            CreateCustomerSnapshot createCustomerSnapshot = new CreateCustomerSnapshot(mco);

            // 2. Send command to the Internet
            TheInternet.Enqueue(createCustomerSnapshot);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

    }
}
