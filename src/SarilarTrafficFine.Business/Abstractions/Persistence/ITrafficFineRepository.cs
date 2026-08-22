using SarilarTrafficFine.Business.Abstractions.Persistence.Models;
using SarilarTrafficFine.Entities.Models;

namespace SarilarTrafficFine.Business.Abstractions.Persistence;

public interface ITrafficFineRepository
{
    Task<IReadOnlyList<TrafficFineListRecord>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<TrafficFineDetailsRecord?> GetDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TrafficFineApprovalContextRecord?> GetApprovalContextAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ApprovalHistoryRecord>> GetApprovalHistoryAsync(
        int trafficFineId,
        CancellationToken cancellationToken = default);

    Task<TrafficFine?> GetForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default);

    void SetOriginalRowVersion(
        TrafficFine trafficFine,
        byte[] rowVersion);
}