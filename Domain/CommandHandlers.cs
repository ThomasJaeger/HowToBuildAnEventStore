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
            Customer customer = new Customer(cmd.GetMessageCreateOptions());
            _customerRepository.Save(customer, customer.CustomerId.Id, -1);
        }

        private void Handle(ChargeCustomer cmd)
        {
            Customer customer = _customerRepository.GetById(cmd.CustomerId.Id);
            Currency currency = Currency.ByAlphabeticCode(cmd.CurrencyCode);
            customer.Charge(new Money(cmd.Amount, currency), cmd.GetMessageCreateOptions());
            _customerRepository.Save(customer, customer.CustomerId.Id, customer.Version);
        }
    }
}
