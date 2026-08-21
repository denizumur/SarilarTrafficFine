using System.ComponentModel.DataAnnotations;
using SarilarTrafficFine.Business.Constants;
using VehicleTypeEnum = SarilarTrafficFine.Entities.Enums.VehicleType;

namespace SarilarTrafficFine.Web.Models.Vehicles;

public sealed class VehicleCreateViewModel
{
    [Required(ErrorMessage = "Plaka alaný zorunludur.")]
    [StringLength(
        VehicleRules.PlateNumberMaxLength,
        ErrorMessage = "Plaka en fazla {1} karakter olabilir.")]
    [Display(Name = "Plaka")]
    public string PlateNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Araç tipi seçiniz.")]
    [EnumDataType(
        typeof(VehicleTypeEnum),
        ErrorMessage = "Geçerli bir araç tipi seçiniz.")]
    [Display(Name = "Araç Tipi")]
    public VehicleTypeEnum? VehicleType { get; set; }

    [Required(ErrorMessage = "Marka alaný zorunludur.")]
    [StringLength(
        VehicleRules.BrandMaxLength,
        ErrorMessage = "Marka en fazla {1} karakter olabilir.")]
    [Display(Name = "Marka")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model alaný zorunludur.")]
    [StringLength(
        VehicleRules.ModelMaxLength,
        ErrorMessage = "Model en fazla {1} karakter olabilir.")]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    public static IReadOnlyList<VehicleTypeOption> VehicleTypeOptions { get; } =
    [
        new(VehicleTypeEnum.PassengerCar, "Binek"),
        new(VehicleTypeEnum.TruckTractor, "Çekici"),
        new(VehicleTypeEnum.Trailer, "Dorse"),
        new(VehicleTypeEnum.RentalVehicle, "Kiralýk Araç")
    ];
}

public sealed record VehicleTypeOption(
    VehicleTypeEnum Value,
    string Label);