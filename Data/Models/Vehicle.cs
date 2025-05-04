using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace VehicleExplorer.Server.Data.Models
{
    [Table("Vehicles")]
    [Index(nameof(ModelName))]
    [Index(nameof(Year))]
    public class Vehicle
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ModelName { get; set; } = null!;

        public int Year { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? CombinedMpg { get; set; }

        [Column(TypeName = "decimal(7,2)")]
        public decimal? AnnualFuelCost { get; set; }

        [ForeignKey(nameof(Manufacturer))]
        public int ManufacturerId { get; set; }

        public Manufacturer? Manufacturer { get; set; }
    }
}