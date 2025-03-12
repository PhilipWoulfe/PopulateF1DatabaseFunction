using PopulateF1Database.Services.Drivers.CommandHandlers.Commands;
using System.Threading.Tasks;

namespace PopulateF1Database.Services.Interfaces
{
    public interface IWriteDriversCommandHandler
    {
        Task Handle(WriteDriversCommand command);
    }
}
