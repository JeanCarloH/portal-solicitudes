using System.ComponentModel.DataAnnotations;

namespace Solicitudes.Api.Contracts;

public class CreateServiceRequestDto
{
    [Required]
    [MaxLength(100)]
    public string ExternalReference { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ServiceCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}