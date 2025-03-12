using PopulateF1Database.Services.Results.CommandHandlers.Commands;
using System.Threading.Tasks;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IWriteResultsCommandHandler
    {
        Task Handle(WriteResultsCommand command);
    }
}
