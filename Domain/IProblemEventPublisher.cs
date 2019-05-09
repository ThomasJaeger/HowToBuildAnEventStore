namespace Domain
{
    public interface IProblemEventPublisher
    {
        /// <summary>
        /// Publish a serialized version of a ProblemOccured in JSON format
        /// to all subscribers to ProblemOccured topic.
        /// </summary>
        /// <param name="problemEvent"></param>
        void Publish(ProblemOccured e);
    }
}
