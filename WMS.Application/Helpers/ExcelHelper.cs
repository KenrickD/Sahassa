using OfficeOpenXml;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.DTOs.Products;
using WMS.Domain.Models;

namespace WMS.Application.Helpers
{
    public static class ExcelHelper
    {
        // Static constructor to set license context once
        static ExcelHelper()
        {
            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");
        }
        public static byte[] GenerateLocationImportTemplate()
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Locations");

                // Define headers
                var headers = new string[]
                {
                    "Warehouse Code", "Zone Code", "Location Name", "Location Code", "Row", "Bay", "Level", "Aisle", "Side", "Bin",
                    "Max Weight (kg)", "Max Volume (m³)", "Max Items", "Length (m)", "Width (m)", "Height (m)",
                     "Barcode", "X Coordinate", "Y Coordinate", "Z Coordinate",
                    "Is Active"
                };

                // Set headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Add sample data
                worksheet.Cells[2, 1].Value = "WH-01";
                worksheet.Cells[2, 2].Value = "RACK01";
                worksheet.Cells[2, 3].Value = "A0101";
                worksheet.Cells[2, 4].Value = "A0101";
                worksheet.Cells[2, 5].Value = "A";
                worksheet.Cells[2, 6].Value = 1;
                worksheet.Cells[2, 7].Value = 1;
                worksheet.Cells[2, 8].Value = "A01";
                worksheet.Cells[2, 9].Value = "L";
                worksheet.Cells[2, 10].Value = "";
                worksheet.Cells[2, 11].Value = 1000;
                worksheet.Cells[2, 12].Value = 10;
                worksheet.Cells[2, 13].Value = 100;
                worksheet.Cells[2, 14].Value = 2.4;
                worksheet.Cells[2, 15].Value = 1.2;
                worksheet.Cells[2, 16].Value = 1.8;
                worksheet.Cells[2, 17].Value = "";
                worksheet.Cells[2, 18].Value = 0;
                worksheet.Cells[2, 19].Value = 0;
                worksheet.Cells[2, 20].Value = 0;
                worksheet.Cells[2, 21].Value = "TRUE";

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Add instructions sheet
                var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
                var instructions = new string[]
                {
                    "Location Import Instructions:",
                    "",
                    "Required Fields:",
                    "- Warehouse Code: Must match existing warehouse code",
                    "- Zone Code: Must match existing zone code",
                    "- Location Name: Unique name for the location",
                    "- Location Code: Unique code for the location",
                    "",
                    "Notes:",
                    "- Row should be a single letter (A-Z)",
                    "- Bay and Level should be positive numbers",
                    "- Max Weight in kilograms, Max Volume in cubic meters",
                    "- Coordinates are optional and in meters",
                    "- Is Active should be TRUE or FALSE"
                };

                for (int i = 0; i < instructions.Length; i++)
                {
                    instructionsSheet.Cells[i + 1, 1].Value = instructions[i];
                    if (i == 0) instructionsSheet.Cells[i + 1, 1].Style.Font.Bold = true;
                }

                instructionsSheet.Cells[instructionsSheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public static byte[] ExportLocations(List<LocationDto> locations)
        {

            try
            {
                List<LocationDto> filteredSortedLocations = new List<LocationDto>();

                filteredSortedLocations.AddRange(locations.OrderBy(l => l.ZoneName).ThenBy(l => l.Name).ToList());

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Locations");

                // Headers
                var headers = new string[]
                {
                    "Warehouse", "Zone Name", "Zone Code", "Location Name", "Location Code", "Row", "Bay", "Level", "Aisle", "Side", "Bin",
                    "Max Weight (kg)", "Max Volume (m³)", "Max Items", "Length (m)", "Width (m)", "Height (m)",
                    "Barcode", "X Coordinate", "Y Coordinate", "Z Coordinate",
                    "Is Active", "Is Empty", "Full Location Code", "Inventory Count"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Data rows
                for (int i = 0; i < filteredSortedLocations.Count; i++)
                {
                    var location = filteredSortedLocations[i];
                    var row = i + 2;

                    worksheet.Cells[row, 1].Value = location.WarehouseName ?? "";
                    worksheet.Cells[row, 2].Value = location.ZoneName ?? "";
                    worksheet.Cells[row, 3].Value = location.ZoneCode ?? "";
                    worksheet.Cells[row, 4].Value = location.Name;
                    worksheet.Cells[row, 5].Value = location.Code;
                    worksheet.Cells[row, 6].Value = location.Row ?? "";
                    worksheet.Cells[row, 7].Value = location.Bay?.ToString() ?? "";
                    worksheet.Cells[row, 8].Value = location.Level?.ToString() ?? "";
                    worksheet.Cells[row, 9].Value = location.Aisle ?? "";
                    worksheet.Cells[row, 10].Value = location.Side ?? "";
                    worksheet.Cells[row, 11].Value = location.Bin ?? "";
                    worksheet.Cells[row, 12].Value = location.MaxWeight;
                    worksheet.Cells[row, 13].Value = location.MaxVolume;
                    worksheet.Cells[row, 14].Value = location.MaxItems;
                    worksheet.Cells[row, 15].Value = location.Length;
                    worksheet.Cells[row, 16].Value = location.Width;
                    worksheet.Cells[row, 17].Value = location.Height;
                    worksheet.Cells[row, 18].Value = location.Barcode ?? "";
                    worksheet.Cells[row, 19].Value = location.XCoordinate?.ToString() ?? "";
                    worksheet.Cells[row, 20].Value = location.YCoordinate?.ToString() ?? "";
                    worksheet.Cells[row, 21].Value = location.ZCoordinate?.ToString() ?? "";
                    worksheet.Cells[row, 22].Value = location.IsActive ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 23].Value = location.IsEmpty ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 24].Value = location.FullLocationCode ?? "";
                    // Get inventory count
                    worksheet.Cells[row, 25].Value = location.InventoryCount;

                    //worksheet.Cells[row, 26].Value = location.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    //worksheet.Cells[row, 27].Value = location.CreatedBy;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public static byte[] GenerateProductImportTemplate(List<GeneralCodeDto> productTypeDtos, List<GeneralCodeDto> productCategorieDtos, List<GeneralCodeDto> unitOfMeasureDtos)
        {
            try
            {
                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Products");

                // Define headers
                var headers = new string[]
                {
                    "Client Code", "Product Type", "Product Name", "SKU", "Barcode", "Description", "Category", "Sub Category", "Unit of Measure",
                    "Weight (kg)", "Length (m)", "Width (m)", "Height (m)", "Requires Lot Tracking", "Requires Expiration Date",
                    "Requires Serial Number", "Is Hazardous", "Is Fragile", "Is Active"
                };

                // Set headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }
                var productTypes = productTypeDtos.Select(x => x.Name).ToList();
                var productCategories = productCategorieDtos.Select(x => x.Name).ToList();
                var unitOfMeasures = unitOfMeasureDtos.Select(x => x.Name).ToList();

                // Add sample data
                worksheet.Cells[2, 1].Value = "CLIENT001";
                worksheet.Cells[2, 2].Value = productTypes.First();
                worksheet.Cells[2, 3].Value = "Sample Product";
                worksheet.Cells[2, 4].Value = "SKU001";
                worksheet.Cells[2, 5].Value = "1234567890123";
                worksheet.Cells[2, 6].Value = "Sample product description";
                worksheet.Cells[2, 7].Value = productCategories.First();
                worksheet.Cells[2, 8].Value = "Components";
                worksheet.Cells[2, 9].Value = unitOfMeasures.First();
                worksheet.Cells[2, 10].Value = 0.5;
                worksheet.Cells[2, 11].Value = 0.1;
                worksheet.Cells[2, 12].Value = 0.05;
                worksheet.Cells[2, 13].Value = 0.02;
                worksheet.Cells[2, 14].Value = "FALSE";
                worksheet.Cells[2, 15].Value = "FALSE";
                worksheet.Cells[2, 16].Value = "TRUE";
                worksheet.Cells[2, 17].Value = "FALSE";
                worksheet.Cells[2, 18].Value = "FALSE";
                worksheet.Cells[2, 19].Value = "TRUE";

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Add instructions sheet
                var instructionsSheet = package.Workbook.Worksheets.Add("Instructions");
                string instructionProductType = $"- Product Type: Must match existing code ({string.Join(", ", productTypes)})";
                string instructionProductCategories = $"- Product Category: Must match existing code ({string.Join(", ", productCategories)})";
                string instructionUnitOfMeasure = $"- Unit of Measure: Must match existing code ({string.Join(", ", unitOfMeasures)})";

                var instructions = new string[]
                {
                    "Product Import Instructions:",
                    "",
                    "Required Fields:",
                    "- Client Code: Must match existing client code",
                    instructionProductType,
                    "- Product Name: Unique name for the product within the client",
                    "- SKU: Unique SKU for the product within the client",
                    instructionProductCategories,
                    instructionUnitOfMeasure,
                    "",
                    "Optional Fields:",
                    "- Barcode: Product barcode (must be unique across system)",
                    "- Description: Product description",
                    "- Sub Category: Product sub-category",
                    "",
                    "Physical Dimensions:",
                    "- Weight in kilograms",
                    "- Length, Width, Height in meters",
                    "",
                    "Boolean Fields (TRUE/FALSE):",
                    "- Requires Lot Tracking: Product needs lot/batch tracking",
                    "- Requires Expiration Date: Product has expiration dates",
                    "- Requires Serial Number: Product needs serial number tracking",
                    "- Is Hazardous: Product is hazardous material",
                    "- Is Fragile: Product requires careful handling",
                    "- Is Active: Product is active (default TRUE)"
                };

                for (int i = 0; i < instructions.Length; i++)
                {
                    instructionsSheet.Cells[i + 1, 1].Value = instructions[i];
                    if (i == 0) instructionsSheet.Cells[i + 1, 1].Style.Font.Bold = true;
                }

                instructionsSheet.Cells[instructionsSheet.Dimension.Address].AutoFitColumns();

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error generating product template: {ex.Message}", ex);
            }
        }

        public static byte[] ExportProducts(List<ProductDto> products)
        {
            try
            {
                List<ProductDto> filteredSortedProducts = new List<ProductDto>();
                filteredSortedProducts.AddRange(products.OrderBy(p => p.ClientName).ThenBy(p => p.Name).ToList());

                using var package = new ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Products");

                // Headers
                var headers = new string[]
                {
                    "Client Name", "Client Code", "Product Type", "Product Name", "SKU", "Barcode", "Description", "Category", "Sub Category",
                    "Unit of Measure", "Weight (kg)", "Length (m)", "Width (m)", "Height (m)", "Volume (m³)", "Inventory Items",
                    "Requires Lot Tracking", "Requires Expiration Date", "Requires Serial Number",
                    "Is Hazardous", "Is Fragile", "Is Active", "Created Date", "Created By"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                }

                // Data rows
                for (int i = 0; i < filteredSortedProducts.Count; i++)
                {
                    var product = filteredSortedProducts[i];
                    var row = i + 2;

                    // Calculate volume (L x W x H)
                    var volume = product.Length * product.Width * product.Height;

                    worksheet.Cells[row, 1].Value = product.ClientName ?? "";
                    worksheet.Cells[row, 2].Value = product.ClientCode ?? "";
                    worksheet.Cells[row, 3].Value = product.ProductTypeName ?? "";
                    worksheet.Cells[row, 4].Value = product.Name;
                    worksheet.Cells[row, 5].Value = product.SKU;
                    worksheet.Cells[row, 6].Value = product.Barcode ?? "";
                    worksheet.Cells[row, 7].Value = product.Description ?? "";
                    worksheet.Cells[row, 8].Value = product.ProductCategoryName ?? "";
                    worksheet.Cells[row, 9].Value = product.SubCategory ?? "";
                    worksheet.Cells[row, 10].Value = product.UnitOfMeasureCodeName ?? "";
                    worksheet.Cells[row, 11].Value = product.Weight;
                    worksheet.Cells[row, 12].Value = product.Length;
                    worksheet.Cells[row, 13].Value = product.Width;
                    worksheet.Cells[row, 14].Value = product.Height;
                    worksheet.Cells[row, 15].Value = volume;
                    worksheet.Cells[row, 16].Value = product.InventoryCount;
                    worksheet.Cells[row, 17].Value = product.RequiresLotTracking ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 18].Value = product.RequiresExpirationDate ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 19].Value = product.RequiresSerialNumber ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 20].Value = product.IsHazardous ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 21].Value = product.IsFragile ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 22].Value = product.IsActive ? "TRUE" : "FALSE";
                    worksheet.Cells[row, 23].Value = product.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 24].Value = product.CreatedBy;
                }

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Add summary sheet
                var summarySheet = package.Workbook.Worksheets.Add("Summary");

                // Summary statistics
                var totalProducts = filteredSortedProducts.Count;
                var activeProducts = filteredSortedProducts.Count(p => p.IsActive);
                var inactiveProducts = totalProducts - activeProducts;
                var hazardousProducts = filteredSortedProducts.Count(p => p.IsHazardous);
                var fragileProducts = filteredSortedProducts.Count(p => p.IsFragile);
                var lotTrackedProducts = filteredSortedProducts.Count(p => p.RequiresLotTracking);
                var serialTrackedProducts = filteredSortedProducts.Count(p => p.RequiresSerialNumber);
                var expirationTrackedProducts = filteredSortedProducts.Count(p => p.RequiresExpirationDate);

                // Summary data
                var summaryData = new object[,]
                {
                    {"Export Summary", ""},
                    {"", ""},
                    {"Total Products", totalProducts},
                    {"Active Products", activeProducts},
                    {"Inactive Products", inactiveProducts},
                    {"", ""},
                    {"Special Handling", ""},
                    {"Hazardous Products", hazardousProducts},
                    {"Fragile Products", fragileProducts},
                    {"", ""},
                    {"Tracking Requirements", ""},
                    {"Lot Tracking Required", lotTrackedProducts},
                    {"Serial Number Required", serialTrackedProducts},
                    {"Expiration Date Required", expirationTrackedProducts},
                    {"", ""},
                    {"Export Date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")},
                    {"Total Inventory Items", filteredSortedProducts.Sum(p => p.InventoryCount)},
                    {"Total Current Stock", filteredSortedProducts.Sum(p => p.CurrentStockLevel)}
                };

                // Populate summary sheet
                for (int i = 0; i < summaryData.GetLength(0); i++)
                {
                    summarySheet.Cells[i + 1, 1].Value = summaryData[i, 0];
                    summarySheet.Cells[i + 1, 2].Value = summaryData[i, 1];

                    // Style the headers
                    if (summaryData[i, 0].ToString().Contains("Summary") ||
                        summaryData[i, 0].ToString().Contains("Handling") ||
                        summaryData[i, 0].ToString().Contains("Requirements"))
                    {
                        summarySheet.Cells[i + 1, 1].Style.Font.Bold = true;
                        summarySheet.Cells[i + 1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        summarySheet.Cells[i + 1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                    }
                }

                summarySheet.Cells[summarySheet.Dimension.Address].AutoFitColumns();

                // Add categories sheet if we have category data
                var categories = filteredSortedProducts
                    .Where(p => !string.IsNullOrEmpty(p.ProductCategoryName))
                    .GroupBy(p => p.ProductCategoryName)
                    .Select(g => new { ProductCategoryName = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                if (categories.Any())
                {
                    var categorySheet = package.Workbook.Worksheets.Add("Categories");

                    // Category headers
                    categorySheet.Cells[1, 1].Value = "Category";
                    categorySheet.Cells[1, 2].Value = "Product Count";
                    categorySheet.Cells[1, 1].Style.Font.Bold = true;
                    categorySheet.Cells[1, 2].Style.Font.Bold = true;
                    categorySheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    categorySheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    categorySheet.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    categorySheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);

                    // Category data
                    for (int i = 0; i < categories.Count; i++)
                    {
                        categorySheet.Cells[i + 2, 1].Value = categories[i].ProductCategoryName;
                        categorySheet.Cells[i + 2, 2].Value = categories[i].Count;
                    }

                    categorySheet.Cells[categorySheet.Dimension.Address].AutoFitColumns();
                }

                return package.GetAsByteArray();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error exporting products: {ex.Message}", ex);
            }
        }

        // Additional helper method for validating product import data
        public static bool ValidateProductImportRow(ExcelWorksheet worksheet, int row, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                // Required fields validation
                var clientCode = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                var productName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                var sku = worksheet.Cells[row, 3].Value?.ToString()?.Trim();

                if (string.IsNullOrEmpty(clientCode))
                    errors.Add("Client Code is required");

                if (string.IsNullOrEmpty(productName))
                    errors.Add("Product Name is required");

                if (string.IsNullOrEmpty(sku))
                    errors.Add("SKU is required");

                // Validate numeric fields
                var weightStr = worksheet.Cells[row, 9].Value?.ToString();
                if (!string.IsNullOrEmpty(weightStr) && !decimal.TryParse(weightStr, out _))
                    errors.Add("Weight must be a valid decimal number");

                var lengthStr = worksheet.Cells[row, 10].Value?.ToString();
                if (!string.IsNullOrEmpty(lengthStr) && !decimal.TryParse(lengthStr, out _))
                    errors.Add("Length must be a valid decimal number");

                var widthStr = worksheet.Cells[row, 11].Value?.ToString();
                if (!string.IsNullOrEmpty(widthStr) && !decimal.TryParse(widthStr, out _))
                    errors.Add("Width must be a valid decimal number");

                var heightStr = worksheet.Cells[row, 12].Value?.ToString();
                if (!string.IsNullOrEmpty(heightStr) && !decimal.TryParse(heightStr, out _))
                    errors.Add("Height must be a valid decimal number");

                // Validate stock levels
                var minStockStr = worksheet.Cells[row, 13].Value?.ToString();
                var maxStockStr = worksheet.Cells[row, 14].Value?.ToString();
                var reorderPointStr = worksheet.Cells[row, 15].Value?.ToString();
                var reorderQtyStr = worksheet.Cells[row, 16].Value?.ToString();

                if (!string.IsNullOrEmpty(minStockStr) && !decimal.TryParse(minStockStr, out _))
                    errors.Add("Min Stock Level must be a valid decimal number");

                if (!string.IsNullOrEmpty(maxStockStr) && !decimal.TryParse(maxStockStr, out _))
                    errors.Add("Max Stock Level must be a valid decimal number");

                if (!string.IsNullOrEmpty(reorderPointStr) && !decimal.TryParse(reorderPointStr, out _))
                    errors.Add("Reorder Point must be a valid decimal number");

                if (!string.IsNullOrEmpty(reorderQtyStr) && !decimal.TryParse(reorderQtyStr, out _))
                    errors.Add("Reorder Quantity must be a valid decimal number");

                // Validate boolean fields
                var booleanFields = new[]
                {
                    (17, "Requires Lot Tracking"),
                    (18, "Requires Expiration Date"),
                    (19, "Requires Serial Number"),
                    (20, "Is Hazardous"),
                    (21, "Is Fragile"),
                    (22, "Is Active")
                };

                foreach (var (column, fieldName) in booleanFields)
                {
                    var value = worksheet.Cells[row, column].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(value) &&
                        !value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) &&
                        !value.Equals("FALSE", StringComparison.OrdinalIgnoreCase) &&
                        !value.Equals("1") && !value.Equals("0"))
                    {
                        errors.Add($"{fieldName} must be TRUE or FALSE");
                    }
                }

                return !errors.Any();
            }
            catch (Exception ex)
            {
                errors.Add($"Error validating row: {ex.Message}");
                return false;
            }
        }

        // Helper method to convert Excel boolean values
        public static bool ParseExcelBoolean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            value = value.Trim();
            return value.Equals("TRUE", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("1") ||
                   value.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                   value.Equals("Y", StringComparison.OrdinalIgnoreCase);
        }

        // Helper method to safely parse decimal values from Excel
        public static decimal ParseExcelDecimal(string value, decimal defaultValue = 0)
        {
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return decimal.TryParse(value.Trim(), out var result) ? result : defaultValue;
        }
    }
}
