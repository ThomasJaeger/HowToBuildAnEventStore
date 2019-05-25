using ReadModel.Queries;
using Shared;

namespace ReadModel.Interfaces
{
    public interface IReadModelRepository
    {
        void Handle(Event e);
        string Handle(Query q);
    }
}
