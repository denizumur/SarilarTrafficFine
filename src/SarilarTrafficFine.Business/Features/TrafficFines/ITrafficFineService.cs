using SarilarTrafficFine.Business.Features.TrafficFines.Models;
using SarilarTrafficFine.Business.Security;

namespace SarilarTrafficFine.Business.Features.TrafficFines;

public interface ITrafficFineService
{
    Task<IReadOnlyList<TrafficFineListItemDto>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<TrafficFineDetailsDto?> GetDetailsAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<TrafficFineCommandResult> CreateAsync(
        TrafficFineCreateRequest request,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default);

    Task<TrafficFineCommandResult> EditAsync(
        TrafficFineEditRequest request,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default);

    Task<TrafficFineCommandResult> SubmitAsync(
        int id,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default);

    Task<TrafficFineCommandResult> ApproveAsync(
        int id,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default);

    Task<TrafficFineCommandResult> RejectAsync(
        int id,
        string reason,
        CurrentUserContext currentUser,
        CancellationToken cancellationToken = default);
}