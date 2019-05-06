namespace EventStore
{
    public class ChangesetDocument
    {
        public const long InitialValue = 0;
        public string AggregateId { get; set; }
        public long AggregateVersion { get; set; }

        /// <summary>
        /// Composite key consisting of: aggregate id + verion number of the aggregate
        /// e.g. 43a85971-5651-4d10-bda8-f79bc71a0726:1
        /// This is important so that PUT requests into the table are unique due to 
        /// duplicate commands being sent to the domain.
        /// </summary>
        public string ChangesetId { get; set; }

        /// <summary>
        /// If it is null, it is the start of the changeset
        /// </summary>
        public string ParentChangesetId { get; set; }

        // Will hold a serialzed collection of 1 or more domain events
        //public byte[] Content { get; set; }

        /// <summary>
        /// Using Json serialization for now
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Marks this changset as snapshot. The changeset then would hold
        /// a serialized version of the aggregate's state instead of a domain 
        /// event in Content.
        /// </summary>
        public bool IsSnapshot { get; set; }

        public long NumberOfEvents { get; set; }

        //        public Stream GetContentStream()
        //        {
        //            return new MemoryStream(Content, false);
        //        }
    }
}
