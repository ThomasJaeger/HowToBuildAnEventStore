using Commands;
using FakeStuff;
using System;

namespace Client
{
    class Program
    {
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
            SignUp signUp = new SignUp("test@gmail.com", "Thomas", "Jaeger", "password");

            // 2. Send command to the Internet
            TheInternet.Enqueue(signUp);

            // 3. Let the backend process the command
            TheBackend.ProcessCommand();
        }
    }
}
