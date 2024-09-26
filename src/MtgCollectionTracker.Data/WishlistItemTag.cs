using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace MtgCollectionTracker.Data;

[Owned]
public class WishlistItemTag
{
    [MaxLength(48)]
    public required string Name { get; set; }
}
