using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace VehicleExplorer.Server.Data.Models
{
    [Table("Manufacturers")]
    [Index(nameof(Name))]
    public class Manufacturer
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        [StringLength(10)]
        public string? Code { get; set; }

        [JsonIgnore]
        public ICollection<Vehicle>? Vehicles { get; set; }
    }
}