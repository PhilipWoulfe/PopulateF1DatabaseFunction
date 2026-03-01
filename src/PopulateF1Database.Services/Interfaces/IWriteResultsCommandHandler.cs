using PopulateF1Database.Services.Results.CommandHandlers.Commands;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IWriteResultsCommandHandler
    {
        Task Handle(WriteResultsCommand command);
    }
}
