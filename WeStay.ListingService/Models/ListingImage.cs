using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WeStay.ListingService.Models;

namespace WeStay.ListingService.Models
{
    public class ListingImage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ListingId { get; set; }

        [ForeignKey("ListingId")]
        public virtual Listing Listing { get; set; }

        [Required]
        [Url]
        [MaxLength(500)]
        public string ImageUrl { get; set; }

        [MaxLength(200)]
        public string Caption { get; set; }

        public bool IsPrimary { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}