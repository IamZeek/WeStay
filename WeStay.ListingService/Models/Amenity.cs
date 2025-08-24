using System.ComponentModel.DataAnnotations;
using WeStay.ListingService.Models;

namespace WeStay.ListingService.Models
{
    public class Amenity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [MaxLength(500)]
        public string Description { get; set; }

        // Many-to-many with Listing
        public virtual ICollection<Listing> Listings { get; set; }
    }
}