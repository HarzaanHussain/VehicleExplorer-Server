using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleExplorer.Server.Data;
using VehicleExplorer.Server.Data.Models;
using System.Globalization;
using System.Text;
using VehicleExplorer.Server.Data.Source;

namespace VehicleExplorer.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SeedController(
        ApplicationDbContext context,
        IWebHostEnvironment env,
        ILogger<SeedController> logger) : ControllerBase
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IWebHostEnvironment _env = env;
        private readonly ILogger<SeedController> _logger = logger;

        [HttpGet]
        public async Task<ActionResult> Import()
        {
            try
            {
                string pathToCsv = Path.Combine(_env.ContentRootPath, "Data", "Source", "VehicleData.csv");

                if (!System.IO.File.Exists(pathToCsv))
                {
                    _logger.LogError("CSV file not found: {path}", pathToCsv);
                    return NotFound($"CSV file not found: {pathToCsv}");
                }

                _logger.LogInformation("Starting import from: {path}", pathToCsv);

                // Dictionary to store manufacturers we've seen
                var manufacturerDict = (await _context.Manufacturers.ToListAsync())
                    .ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

                // Add existing manufacturers
                foreach (var m in await _context.Manufacturers.ToListAsync())
                {
                    manufacturerDict[m.Name] = m;
                }

                var manufacturersAdded = 0;
                var vehiclesAdded = 0;

                // Read all lines from the CSV file
                string[] lines = await System.IO.File.ReadAllLinesAsync(pathToCsv);

                // Get the header row to find column indexes
                string[] headers = lines[0].Split(',');

                // Find indexes for the columns we need
                int makeIndex = Array.FindIndex(headers, h => h.Equals("Make", StringComparison.OrdinalIgnoreCase));
                int modelIndex = Array.FindIndex(headers, h => h.Equals("Model", StringComparison.OrdinalIgnoreCase));
                int yearIndex = Array.FindIndex(headers, h => h.Equals("Year", StringComparison.OrdinalIgnoreCase));
                int combinedMpgIndex = Array.FindIndex(headers, h => h.Equals("Combined Mpg For Fuel Type1", StringComparison.OrdinalIgnoreCase));
                int annualFuelCostIndex = Array.FindIndex(headers, h => h.Equals("Annual Fuel Cost For Fuel Type1", StringComparison.OrdinalIgnoreCase));
                int mfrCodeIndex = Array.FindIndex(headers, h => h.Equals("MFR Code", StringComparison.OrdinalIgnoreCase));

                if (makeIndex == -1 || modelIndex == -1)
                {
                    _logger.LogError("Required columns 'Make' or 'Model' not found in CSV");
                    return BadRequest("Required columns not found in CSV file");
                }

                // Process each line (skip header)
                for (int i = 1; i < lines.Length; i++)
                {
                    // Split the line by comma, but handle quoted values correctly
                    string[] values = SplitCsvLine(lines[i]);

                    if (values.Length <= Math.Max(makeIndex, modelIndex))
                    {
                        // Skip lines that don't have enough columns
                        continue;
                    }

                    string makeName = values[makeIndex].Trim();
                    string modelName = values[modelIndex].Trim();

                    if (string.IsNullOrEmpty(makeName) || string.IsNullOrEmpty(modelName))
                    {
                        continue;
                    }

                    // Extract other values if available
                    string? mfrCode = mfrCodeIndex >= 0 && mfrCodeIndex < values.Length
                        ? values[mfrCodeIndex].Trim()
                        : null;

                    int year = 2023; // Default value
                    if (yearIndex >= 0 && yearIndex < values.Length &&
                        int.TryParse(values[yearIndex].Trim(), out int parsedYear))
                    {
                        year = parsedYear;
                    }

                    decimal? combinedMpg = null;
                    if (combinedMpgIndex >= 0 && combinedMpgIndex < values.Length &&
                        decimal.TryParse(values[combinedMpgIndex].Trim(), out decimal parsedMpg))
                    {
                        combinedMpg = parsedMpg;
                    }

                    decimal? annualFuelCost = null;
                    if (annualFuelCostIndex >= 0 && annualFuelCostIndex < values.Length &&
                        decimal.TryParse(values[annualFuelCostIndex].Trim(), out decimal parsedCost))
                    {
                        annualFuelCost = parsedCost;
                    }

                    // Add the manufacturer if we haven't seen it
                    if (!manufacturerDict.TryGetValue(makeName, out var manufacturer))
                    {
                        manufacturer = new Manufacturer
                        {
                            Name = makeName,
                            Code = mfrCode
                        };
                        _context.Manufacturers.Add(manufacturer);
                        await _context.SaveChangesAsync(); // Save to get ID
                        manufacturerDict[makeName] = manufacturer;
                        manufacturersAdded++;
                    }

                    // Check if this vehicle already exists
                    bool exists = await _context.Vehicles
                        .AnyAsync(v => v.ModelName == modelName && v.Year == year && v.ManufacturerId == manufacturer.Id);

                    if (!exists)
                    {
                        var vehicle = new Vehicle
                        {
                            ModelName = modelName,
                            Year = year,
                            CombinedMpg = combinedMpg,
                            AnnualFuelCost = annualFuelCost,
                            ManufacturerId = manufacturer.Id
                        };

                        _context.Vehicles.Add(vehicle);
                        vehiclesAdded++;
                    }

                    // Save every 100 vehicles to prevent memory issues
                    if (vehiclesAdded > 0 && vehiclesAdded % 100 == 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }

                // Final save
                if (vehiclesAdded > 0)
                {
                    await _context.SaveChangesAsync();
                }

                return Ok(new { Manufacturers = manufacturersAdded, Vehicles = vehiclesAdded });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing data");
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        // Helper method to split CSV line correctly handling quoted values
        private static string[] SplitCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var field = new StringBuilder();

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(field.ToString());
                    field.Clear();
                }
                else
                {
                    field.Append(c);
                }
            }

            // Add the last field
            result.Add(field.ToString());
            return [.. result];
        }
    }
}