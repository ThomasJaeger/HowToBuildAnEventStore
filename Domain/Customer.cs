﻿using Shared;
using System;
using TeixeiraSoftware.Finance;

namespace Domain
{
    public class Customer : AggregateRoot
    {
        public CustomerId CustomerId { get; private set; }
        public Money AccountBalance { get; private set; } = new Money(0, Currency.ByAlphabeticCode("USD"));
        public DateTime Created { get; private set; }
        public bool Delinquent { get; private set; }
        public string Description { get; private set; }
        public string Email { get; private set; }
        public string FirstName { get; private set; }
        public string Lastname { get; private set; }
        public string Password { get; private set; }

        public Customer(string firstName, string lastName, string password, MessageCreateOptions mco)
        {
            ApplyChange(new SignedUp(firstName, lastName, password, new MessageCreateOptions(mco)));
        }

        public Customer()
        {
        }

        private void Apply(SignedUp e)
        {
            Email = e.CustomerId.Id;
            CustomerId = e.CustomerId;
            Created = e.Created;
            FirstName = e.FirstName;
            Lastname = e.Lastname;
            Password = e.Password;
        }

        public void Charge(Money amount, MessageCreateOptions mco)
        {
            ApplyChange(new CustomerCharged(amount, mco));
        }

        private void Apply(CustomerCharged e)
        {
            AccountBalance = AccountBalance + e.Amount;
        }

        public void CreateSnapshot(MessageCreateOptions mco)
        {
            CustomerSnapshotCreated customerSnapshotCreated = new CustomerSnapshotCreated(AccountBalance, 
                Delinquent, Description, Email, Created, FirstName, Lastname, Password, new MessageCreateOptions(mco));
            ApplyChange(customerSnapshotCreated);
        }

        private void Apply(CustomerSnapshotCreated e)
        {
            CustomerId = e.CustomerId;
            AccountBalance = e.AccountBalance;
            Delinquent = e.Delinquent;
            Description = e.Description;
            Email = e.Email;
            Created = e.Created;
            FirstName = e.FirstName;
            Lastname = e.Lastname;
            Password = e.Password;
        }
    }
}
