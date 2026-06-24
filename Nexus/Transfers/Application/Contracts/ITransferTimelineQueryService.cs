using Aidan.Core.Patterns;
using Nexus.Transfers.Application.Models;

namespace Nexus.Transfers.Application.Contracts;

public interface ITransferTimelineQueryService
{
    Task<IResult<TransferTimelineDetails>> GetTimelineAsync(string transferId);
}
