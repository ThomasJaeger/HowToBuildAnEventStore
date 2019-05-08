using Commands;
using FakeStuff;
using Shared;
using System;

namespace Client
{
    class Program
    {
        private static CustomerId _customerId;

        static void Main(string[] args)
        {
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
            _customerId = new CustomerId(Guid.NewGuid().ToString());
            mco.CustomerId = _customerId;

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
            mco.CustomerId = _customerId;

            ChargeCustomer chargeCustomer = new ChargeCustomer(amount, mco);

            // 2. Send command to the Internet
            TheInternet.Enqueue(chargeCustomer);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }

    }
}
