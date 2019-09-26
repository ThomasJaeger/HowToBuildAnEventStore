using Commands;
using Newtonsoft.Json;
using Shared;
using System;
using System.Collections.Generic;
using TeixeiraSoftware.Finance;

namespace Domain
{
    public class CommandHandlers
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IProblemEventPublisher _problemEventPublisher;

        public CommandHandlers(IRepository<Customer> customerRepository, IProblemEventPublisher problemEventPublisher)
        {
            _customerRepository = customerRepository;
            _problemEventPublisher = problemEventPublisher;
        }

        public void Handle(Command cmd)
        {
            Console.WriteLine("********** Billing domain processing command: " + JsonConvert.SerializeObject(cmd));

            if (cmd is SignUp)
                Handle((SignUp)cmd);
            else if (cmd is ChargeCustomer)
                Handle((ChargeCustomer)cmd);
            else if (cmd is CreateCustomerSnapshot)
                Handle((CreateCustomerSnapshot)cmd);
            else if (cmd is CreateCustomerSummary)
                Handle((CreateCustomerSummary)cmd);
            else
            {
                Console.WriteLine("Could not determine the command type, is the class type included in the command?: " + JsonConvert.SerializeObject(cmd));
            }
        }

        private void Handle(SignUp cmd)
        {
            // Create a new customer
            Customer customer = new Customer(cmd.FirstName, cmd.LastName, cmd.Password, cmd.GetMessageCreateOptions());

            // Commit the customer object's domain event(s) into the event store
            _customerRepository.Save(customer, customer.CustomerId.Id, -1);  // Customer will be at version 0 after successfull commit into event store
        }

        private void Handle(ChargeCustomer cmd)
        {
            // Get the customer from the event store
            Customer customer = _customerRepository.GetById(Aggregates.Customer, cmd.CustomerId.Id);

            // Create proper currency value based on USD passed from the client
            Currency currency = Currency.ByAlphabeticCode(cmd.CurrencyCode);

            //int version = customer.Version;  // should be version 0 since we only created a customer
            int version = cmd.ExpectedVersion;

            try
            {
                // Execute aggregate root method
                customer.Charge(new Money(cmd.Amount, currency), cmd.GetMessageCreateOptions());

                // Commit all possible domain events created by customer
                _customerRepository.Save(customer, customer.CustomerId.Id, version);

                // Customer is now at version 1, verify in Aggregates table
            }
            catch (ConcurrencyException e)
            {
                ProblemOccured problemOccured = new ProblemOccured(cmd.GetMessageCreateOptions())
                {
                    Created = DateTime.Now,
                    AggregateId = cmd.CustomerId.Id,
                    AggregateType = "Customer",
                    AggregateVersionInEventStore = _customerRepository.GetAggregateVersion(Aggregates.Customer, cmd.CustomerId.Id),
                    AggregateVersionExpectedByClient = version,
                    ProblemCode = ProblemCode.Concurrency,
                    CommandIdThatTriggeredProblem = cmd.MessageId,
                    CommandThatTriggeredProblem = JsonConvert.SerializeObject(cmd)
                };
                _problemEventPublisher.Publish(problemOccured);
                Console.WriteLine(JsonConvert.SerializeObject(problemOccured));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Handle(CreateCustomerSnapshot cmd)
        {
            try
            {
                Customer customer = _customerRepository.GetById(Aggregates.Customer, cmd.AggregateId);
                int version = customer.Version;
                customer.CreateSnapshot(cmd.GetMessageCreateOptions());
                _customerRepository.Save(customer, customer.CustomerId.Id, version);
            }
            catch (ConcurrencyException e)
            {
                ProblemOccured problemOccured = new ProblemOccured(cmd.GetMessageCreateOptions())
                {
                    Created = DateTime.Now,
                    AggregateId = cmd.AggregateId,
                    AggregateType = "Customer",
                    AggregateVersionInEventStore = _customerRepository.GetAggregateVersion(Aggregates.Customer, cmd.AggregateId),
                    AggregateVersionExpectedByClient = 0,
                    ProblemCode = ProblemCode.Concurrency,
                    CommandIdThatTriggeredProblem = cmd.MessageId,
                    CommandThatTriggeredProblem = JsonConvert.SerializeObject(cmd)
                };
                _problemEventPublisher.Publish(problemOccured);
                Console.WriteLine(JsonConvert.SerializeObject(problemOccured));
                Console.WriteLine(e.StackTrace);
                throw;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Handle(CreateCustomerSummary cmd)
        {
            // Note: Get events from last snapshot only, if exists
            List<Event> events = _customerRepository.GetEventsForAggregate(Aggregates.Customer, cmd.CustomerId.Id);

            int totalCharges = 0;
            Currency currency = Currency.ByAlphabeticCode("USD");
            Money total = new Money(0, currency);

            Console.WriteLine();
            Console.WriteLine("------------------------------------------------------");
            Console.WriteLine("                  Customer Summary");
            Console.WriteLine("                  ----------------");

            foreach (var e in events)
            {
                if (e is SignedUp)
                {
                    SignedUp evt = e as SignedUp;
                    Console.WriteLine($"{evt.FirstName} {evt.Lastname}");
                    Console.WriteLine($"Customer Id: {evt.CustomerId.Id}");
                    Console.WriteLine();
                    Console.WriteLine("Created                                         Amount");
                    Console.WriteLine("------------------------------------------------------");
                }

                if (e is CustomerCharged)
                {
                    CustomerCharged evt = e as CustomerCharged;
                    Console.WriteLine($"{evt.Created}                           {evt.Amount.Amount}");
                    total = total + evt.Amount;
                    totalCharges++;
                }
            }

            Console.WriteLine("======================================================");
            Console.WriteLine($"Total charges: {totalCharges},    Total Amount: {total.Amount}");
            Console.WriteLine("======================================================");
        }
    }
}
