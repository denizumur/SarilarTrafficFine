using System.ComponentModel.DataAnnotations;

namespace SarilarTrafficFine.Web.Models.TrafficFines;

public sealed class TrafficFineEditViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Araç seçimi zorunludur.")]
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Geçerli bir araç seçiniz.")]
    [Display(Name = "Araç")]
    public int? VehicleId { get; set; }

    [Required(ErrorMessage = "Ceza tarihi zorunludur.")]
    [DataType(DataType.Date)]
    [Display(Name = "Ceza Tarihi")]
    public DateOnly? FineDate { get; set; }

    [Required(ErrorMessage = "Ceza tutarý zorunludur.")]
    [Range(
        typeof(decimal),
        "0.01",
        "9999999999999999.99",
        ParseLimitsInInvariantCulture = true,
        ErrorMessage = "Ceza tutarý sýfýrdan büyük olmalýdýr.")]
    [Display(Name = "Ceza Tutarý")]
    public decimal? Amount { get; set; }

    [StringLength(
        1000,
        ErrorMessage = "Açýklama en fazla {1} karakter olabilir.")]
    [Display(Name = "Açýklama")]
    public string? Description { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;

    public IReadOnlyList<TrafficFineVehicleOptionViewModel> Vehicles
    {
        get;
        set;
    } = [];
}