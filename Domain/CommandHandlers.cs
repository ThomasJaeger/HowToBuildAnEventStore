using Commands;
using Newtonsoft.Json;
using Shared;
using System;
using TeixeiraSoftware.Finance;

namespace Domain
{
    public class CommandHandlers
    {
        private readonly IRepository<Customer> _customerRepository;

        public CommandHandlers(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public void Handle(Command cmd)
        {
            Console.WriteLine("********** Billing domain processing command: " + JsonConvert.SerializeObject(cmd));

            if (cmd is SignUp)
                Handle((SignUp)cmd);
            else if (cmd is ChargeCustomer)
                Handle((ChargeCustomer)cmd);
            else
            {
                Console.WriteLine("Could not determine the command type, is the class type included in the command?: " + JsonConvert.SerializeObject(cmd));
            }
        }

        private void Handle(SignUp cmd)
        {
            // Create a new customer
            Customer customer = new Customer(cmd.GetMessageCreateOptions());

            // Commit the customer object's domain event(s) into the event store
            _customerRepository.Save(customer, customer.CustomerId.Id, -1);  // Customer will be at version 0 after successfull commit into event store
        }

        private void Handle(ChargeCustomer cmd)
        {
            // Get the customer from the event store
            Customer customer = _customerRepository.GetById(cmd.CustomerId.Id);

            // Create proper currency value based on USD passed from the client
            Currency currency = Currency.ByAlphabeticCode(cmd.CurrencyCode);

            int version = customer.Version;  // should be version 0 since we only created a customer

            // Execute aggregate root method
            customer.Charge(new Money(cmd.Amount, currency), cmd.GetMessageCreateOptions());

            // Commit all possible domain events created by customer
            _customerRepository.Save(customer, customer.CustomerId.Id, version);

            // Customer is now at version 1, verify in Aggregates table
        }
    }
}
