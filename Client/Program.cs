using Commands;
using FakeStuff;
using ReadModel.Queries;
using Shared;
using System;

namespace Client
{
    class Program
    {
        private static string _customerId;
        private static string _tenantId;
        private static string _userId;

        static void Main(string[] args)
        {
            _customerId = Environment.GetEnvironmentVariable("CUSTOMER_ID");
            _tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
            _userId = _customerId;
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
            Console.WriteLine("[4] Ad-Hoc Projection: Create Customer Summary");
            Console.WriteLine("[5] Query Customer Details");
            Console.WriteLine("[6] Query Customer List");
            Console.WriteLine("[7] Display all domain events stored so far");
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
                    case '4':
                        {
                            CreateCustomerSummary();
                            PressAnyKey();
                            break;
                        }
                    case '5':
                        {
                            QueryCustomerDetails();
                            PressAnyKey();
                            break;
                        }
                    case '6':
                        {
                            QueryCustomerList();
                            PressAnyKey();
                            break;
                        }
                    case '7':
                        {
                            DisplayAllDomainEvents();
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
            mco.TenantId = new TenantId(_tenantId);
            mco.UserId = new UserId(_userId);

            SignUp signUp = new SignUp(mco);
            signUp.Email = _customerId;
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
            mco.TenantId = new TenantId(_tenantId);
            mco.UserId = new UserId(_userId);

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
            mco.TenantId = new TenantId(_tenantId);
            mco.UserId = new UserId(_userId);

            CreateCustomerSnapshot createCustomerSnapshot = new CreateCustomerSnapshot(_customerId, mco);

            // 2. Send command to the Internet
            TheInternet.Enqueue(createCustomerSnapshot);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

        private static void CreateCustomerSummary()
        {
            // 1. Create command
            MessageCreateOptions mco = new MessageCreateOptions();
            mco.Created = DateTime.Now;
            mco.CustomerId = new CustomerId(_customerId);
            mco.TenantId = new TenantId(_tenantId);
            mco.UserId = new UserId(_userId);

            CreateCustomerSummary createCustomerSummary = new CreateCustomerSummary(mco);

            // 2. Send command to the Internet
            TheInternet.Enqueue(createCustomerSummary);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

        private static void QueryCustomerDetails()
        {
            QueryCustomerDetails query = new QueryCustomerDetails();
            query.CustomerId = _customerId;

            string customerDetails = TheBackend.QueryCustomerDetails(query);

            Console.WriteLine();
            Console.WriteLine(customerDetails);
        }

        private static void QueryCustomerList()
        {
            QueryCustomerList query = new QueryCustomerList();

            string customerList = TheBackend.QueryCustomerList(query);

            Console.WriteLine();
            Console.WriteLine(customerList);
        }

        private static void DisplayAllDomainEvents()
        {
            string result = TheBackend.DisplayAllDomainEvents(_customerId);

            Console.WriteLine();
            Console.WriteLine(result);
        }

    }
}
