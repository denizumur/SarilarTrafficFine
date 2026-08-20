using SarilarTrafficFine.Entities.Enums;

namespace SarilarTrafficFine.Entities.Models;

public sealed class Vehicle
{
    public int Id { get; set; }

    public string PlateNumber { get; set; } = string.Empty;

    public VehicleType VehicleType { get; set; }

    public string Brand { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<TrafficFine> TrafficFines { get; set; }
        = new List<TrafficFine>();
}