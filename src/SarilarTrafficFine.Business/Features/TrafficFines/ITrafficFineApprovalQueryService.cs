using SarilarTrafficFine.Business.Features.TrafficFines.Models;

namespace SarilarTrafficFine.Business.Features.TrafficFines;

public interface ITrafficFineApprovalQueryService
{
    Task<TrafficFineApprovalDetailsDto?> GetAsync(
        int trafficFineId,
        CancellationToken cancellationToken = default);
}