using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VehicleExplorer.Server.Data.Models;
using VehicleExplorer.Server.Data.Models.Auth;

namespace VehicleExplorer.Server.Data.Source
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        public DbSet<Vehicle> Vehicles => Set<Vehicle>();
        public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    }
}