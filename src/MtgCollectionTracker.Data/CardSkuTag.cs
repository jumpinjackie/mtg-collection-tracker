using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

[Owned]
public class CardSkuTag
{
    [MaxLength(48)]
    public required string Name { get; set; }
}
