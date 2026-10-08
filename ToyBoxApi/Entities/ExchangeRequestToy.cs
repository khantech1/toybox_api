using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ToyBoxApi.Entities;

[Table("Exchange_Request_Toys")]
[PrimaryKey(nameof(RequestId), nameof(ToyId))]
public class ExchangeRequestToy
{
    [Column("request_id")]
    public int RequestId { get; set; }

    [Column("toy_id")]
    public int ToyId { get; set; }

    /// <summary>offered | requested</summary>
    [Required]
    [MaxLength(20)]
    [Column("exchange_role")]
    public string ExchangeRole { get; set; } = string.Empty;

    /// <summary>Snapshot of the toy's estimated value at the time the request was created.</summary>
    [Column("value_at_request", TypeName = "decimal(10,2)")]
    public decimal? ValueAtRequest { get; set; }

    [ForeignKey(nameof(RequestId))]
    public ExchangeRequest? ExchangeRequest { get; set; }

    [ForeignKey(nameof(ToyId))]
    public Toy? Toy { get; set; }
}
