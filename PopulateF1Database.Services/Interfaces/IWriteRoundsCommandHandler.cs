using System.Threading.Tasks;
using PopulateF1Database.Services.Rounds.CommandHandlers.Commands;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IWriteRoundsCommandHandler
    {
        Task Handle(WriteRoundsCommand command);
    }
}
