using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Exchange.WebServices.Data;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.API;
using WMS.Domain.DTOs.LocationGrids;
using WMS.Domain.Models;
using Size = SixLabors.ImageSharp.Size;
namespace WMS.Application.Helpers
{
    public class PDFHelper
    {
        // Colors for PDF generate location grid layout
        private readonly XColor AvailableColor = XColor.FromArgb(16, 185, 129); // #10b981
        private readonly XColor PartialColor = XColor.FromArgb(251, 191, 36);   // #fbbf24
        private readonly XColor OccupiedColor = XColor.FromArgb(239, 68, 68);   // #ef4444
        private readonly XColor EmptyColor = XColor.FromArgb(243, 244, 246);    // #f3f4f6
        private readonly XColor HeaderColor = XColor.FromArgb(249, 250, 251);   // #f9fafb

        public PdfDocument GenerateBarcodesInPDF(List<string> texts)
        {
            //create new MigraDoc Document
            var document = new MigraDoc.DocumentObjectModel.Document();

            foreach (var text in texts)
            {
                Section section = document.AddSection();
                section.PageSetup.PageFormat = PageFormat.A6;
                section.PageSetup.Orientation = Orientation.Landscape;
                section.PageSetup.BottomMargin = 30;
                section.PageSetup.LeftMargin = 20;
                section.PageSetup.TopMargin = 30;
                section.PageSetup.RightMargin = 20;

                var table = section.AddTable();
                table.AddColumn("13.5cm");
                table.Format.Font.Name = "times new roman";
                //table.Borders.Width = 1;
                var barcodeRow = table.AddRow();
                BarcodeHelper barcodeHelper = new BarcodeHelper();
                string barcodeBase64 = barcodeHelper.GenerateCode128BarcodeAsBase64String(text);
                barcodeRow.Format.Font.Name = "times new roman";
                barcodeRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                barcodeRow.Cells[0].Format.Alignment = ParagraphAlignment.Center;
                barcodeRow.Cells[0].AddParagraph().AddImage(barcodeBase64);

                var textRow = table.AddRow();
                textRow.BottomPadding = 3;
                textRow.Format.Font.Name = "times new roman";
                textRow.Format.Font.Size = 100;
                textRow.Cells[0].AddParagraph().AddFormattedText(text, TextFormat.Bold);
                textRow.Cells[0].VerticalAlignment = VerticalAlignment.Center;
                textRow.Cells[0].Format.Alignment = ParagraphAlignment.Center;

            }

            var pdfRenderer = new PdfDocumentRenderer();
            pdfRenderer.Document = document;

            //layout and render document to PDF
            pdfRenderer.RenderDocument();

            return pdfRenderer.PdfDocument;
        }

        public byte[] GenerateLayoutPDF(LocationGridPDFRequest request)
        {
            var document = new PdfDocument();

            // Pre-calculate layout parameters
            var layoutParams = CalculateLayoutParameters(request.Locations);

            // Determine optimal page distribution
            var pageLayout = CalculatePageDistribution(layoutParams);

            // Generate pages
            for (int pageIndex = 0; pageIndex < pageLayout.TotalPages; pageIndex++)
            {
                var page = CreatePage(document);
                var gfx = XGraphics.FromPdfPage(page);

                var pageData = pageLayout.GetPageData(pageIndex);
                RenderPage(gfx, request, pageData, pageIndex + 1, pageLayout.TotalPages);
            }

            return SaveDocument(document);
        }

        private LayoutParameters CalculateLayoutParameters(List<LocationGridItemDto> locations)
        {
            // Row configuration (matching frontend exactly)
            var rowGroups = new[]
            {
        new[] { "P" }, new[] { "O", "N" }, new[] { "M", "L" }, new[] { "K", "J" },
        new[] { "I", "H" }, new[] { "G", "F" }, new[] { "E", "D" }, new[] { "C", "B" }, new[] { "A" }
    };

            var rowOrder = new List<string>();
            foreach (var (group, index) in rowGroups.Select((g, i) => (g, i)))
            {
                rowOrder.AddRange(group);
                if (index < rowGroups.Length - 1)
                    rowOrder.Add(""); // Spacer
            }

            var maxBay = locations.Any() ? locations.Max(l => l.Bay) : 1;
            var maxLevel = 5;

            // A4 Landscape dimensions in points (842 x 595)
            const double pageWidth = 842;
            const double pageHeight = 595;
            const double margin = 30;

            var availableWidth = pageWidth - (2 * margin);
            var availableHeight = pageHeight - (2 * margin);

            // Calculate optimal cell dimensions
            var totalCols = rowOrder.Count + 1; // +1 for bay column
            var spacerCols = rowOrder.Count(r => string.IsNullOrEmpty(r));
            var regularCols = totalCols - spacerCols;

            // Calculate cell width (spacers are 30% of regular cell width)
            var cellWidth = availableWidth / (regularCols + (spacerCols * 0.3));
            cellWidth = Math.Max(Math.Min(cellWidth, 35), 18); // Between 18-35 points

            var spacerWidth = cellWidth * 0.3;

            // Calculate cell height based on fitting levels + header + separators
            var headerHeight = 80; // Header + legend space
            var availableGridHeight = availableHeight - headerHeight - 20; // 20pt bottom margin

            // Each bay needs: 5 levels + small separator (except last bay)
            var bayHeight = maxLevel; // levels per bay
            var separatorHeight = 0.2; // separator between bays as fraction of cell height

            var totalBayUnits = maxBay * bayHeight + (maxBay - 1) * separatorHeight;
            var cellHeight = availableGridHeight / (1 + totalBayUnits); // +1 for header row
            cellHeight = Math.Max(Math.Min(cellHeight, 20), 10); // Between 10-20 points

            return new LayoutParameters
            {
                RowOrder = rowOrder,
                MaxBay = maxBay,
                MaxLevel = maxLevel,
                CellWidth = cellWidth,
                CellHeight = cellHeight,
                SpacerWidth = spacerWidth,
                AvailableWidth = availableWidth,
                AvailableHeight = availableHeight,
                HeaderHeight = headerHeight,
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                Margin = margin
            };
        }

        private PageLayout CalculatePageDistribution(LayoutParameters layoutParams)
        {
            // Calculate how many bays can fit per page
            var bayHeight = layoutParams.MaxLevel * layoutParams.CellHeight;
            var separatorHeight = layoutParams.CellHeight * 0.2;
            var totalBayHeight = bayHeight + separatorHeight; // Height per bay including separator

            var availableGridHeight = layoutParams.AvailableHeight - layoutParams.HeaderHeight - 20;
            var headerRowHeight = layoutParams.CellHeight;

            var availableForBays = availableGridHeight - headerRowHeight;
            var baysPerPage = Math.Max(1, (int)(availableForBays / totalBayHeight));

            // Distribute bays across pages
            var totalPages = (int)Math.Ceiling((double)layoutParams.MaxBay / baysPerPage);
            var pages = new List<PageData>();

            for (int page = 0; page < totalPages; page++)
            {
                var startBay = (page * baysPerPage) + 1;
                var endBay = Math.Min(startBay + baysPerPage - 1, layoutParams.MaxBay);

                pages.Add(new PageData
                {
                    StartBay = startBay,
                    EndBay = endBay,
                    BayCount = endBay - startBay + 1
                });
            }

            return new PageLayout
            {
                TotalPages = totalPages,
                BaysPerPage = baysPerPage,
                Pages = pages
            };
        }

        private PdfPage CreatePage(PdfDocument document)
        {
            var page = document.AddPage();
            page.Orientation = PdfSharp.PageOrientation.Landscape;
            page.Size = PdfSharp.PageSize.A4;
            return page;
        }

        private void RenderPage(XGraphics gfx, LocationGridPDFRequest request, PageData pageData,
            int pageNumber, int totalPages)
        {
            var layoutParams = CalculateLayoutParameters(request.Locations);
            var currentY = layoutParams.Margin;

            // Draw header with page info
            var pageInfo = totalPages > 1 ? $" (Page {pageNumber} of {totalPages})" : "";
            currentY = DrawHeader(gfx, request, layoutParams.Margin, currentY, layoutParams.AvailableWidth, pageInfo);

            // Draw legend only on first page
            if (pageNumber == 1)
            {
                currentY = DrawLegend(gfx, layoutParams.Margin, currentY, layoutParams.AvailableWidth);
            }

            // Draw grid for this page's bay range
            DrawOptimizedGrid(gfx, request.Locations, layoutParams, currentY, pageData);
        }

        private void DrawOptimizedGrid(XGraphics gfx, List<LocationGridItemDto> locations,
            LayoutParameters layoutParams, double startY, PageData pageData)
        {
            if (!locations.Any())
            {
                DrawNoDataMessage(gfx, layoutParams, startY);
                return;
            }

            var font = new XFont("Arial", Math.Max(6, layoutParams.CellHeight * 0.45), XFontStyleEx.Regular);
            var headerFont = new XFont("Arial", Math.Max(7, layoutParams.CellHeight * 0.5), XFontStyleEx.Bold);

            var currentY = startY;
            var currentX = layoutParams.Margin;

            // Draw header row
            foreach (var rowLabel in layoutParams.RowOrder)
            {
                var width = string.IsNullOrEmpty(rowLabel) ? layoutParams.SpacerWidth : layoutParams.CellWidth;
                if (!string.IsNullOrEmpty(rowLabel))
                {
                    DrawCell(gfx, rowLabel, currentX, currentY, width, layoutParams.CellHeight, HeaderColor, headerFont, true);
                }
                currentX += width;
            }
            DrawCell(gfx, "Bay", currentX, currentY, layoutParams.CellWidth, layoutParams.CellHeight, HeaderColor, headerFont, true);

            currentY += layoutParams.CellHeight;

            // Draw bays for this page
            for (int bay = pageData.StartBay; bay <= pageData.EndBay; bay++)
            {
                currentY = DrawBay(gfx, locations, layoutParams, currentY, bay, font);

                // Add separator between bays (except last bay on last page)
                if (bay < pageData.EndBay)
                {
                    currentY = DrawBaySeparator(gfx, layoutParams, currentY);
                }
            }
        }

        private double DrawBay(XGraphics gfx, List<LocationGridItemDto> locations, LayoutParameters layoutParams,
            double startY, int bay, XFont font)
        {
            var currentY = startY;

            // Draw levels for this bay (5 to 1, top to bottom)
            for (int level = layoutParams.MaxLevel; level >= 1; level--)
            {
                var currentX = layoutParams.Margin;

                // Draw cells for each row position
                foreach (var rowLabel in layoutParams.RowOrder)
                {
                    var width = string.IsNullOrEmpty(rowLabel) ? layoutParams.SpacerWidth : layoutParams.CellWidth;

                    if (!string.IsNullOrEmpty(rowLabel))
                    {
                        var location = locations.FirstOrDefault(l =>
                            l.Row == rowLabel && l.Bay == bay && l.Level == level);

                        if (location != null)
                        {
                            var color = GetLocationColor(location.StatusName);
                            var displayText = !string.IsNullOrEmpty(location.Barcode) ? location.Barcode : location.Code;
                            DrawCell(gfx, displayText, currentX, currentY, width, layoutParams.CellHeight, color, font, false);
                        }
                        else
                        {
                            DrawCell(gfx, "-", currentX, currentY, width, layoutParams.CellHeight, EmptyColor, font, false);
                        }
                    }

                    currentX += width;
                }

                // Draw bay number for first level only (spans all 5 levels)
                if (level == layoutParams.MaxLevel)
                {
                    DrawBayHeader(gfx, bay, currentX, currentY, layoutParams, font);
                }

                currentY += layoutParams.CellHeight;
            }

            return currentY;
        }

        private void DrawBayHeader(XGraphics gfx, int bay, double x, double y, LayoutParameters layoutParams, XFont font)
        {
            var bayHeight = layoutParams.CellHeight * layoutParams.MaxLevel;

            // Add small gap between the last location cell and bay header to preserve borders
            var gapFromLastCell = 1.0; // 1 point gap
            var bayX = x + gapFromLastCell;
            var bayWidth = layoutParams.CellWidth - gapFromLastCell;

            var bayRect = new XRect(bayX, y, bayWidth, bayHeight);

            // Draw bay header background
            gfx.DrawRectangle(new XSolidBrush(HeaderColor), bayRect);

            // Draw bay header border with same thickness as other cells
            var borderPen = new XPen(XColor.FromArgb(0, 0, 0), 1.0);
            gfx.DrawRectangle(borderPen, bayRect);

            // Draw bay number text
            var bayText = bay.ToString("00");
            var textSize = gfx.MeasureString(bayText, font);
            var textX = bayX + (bayWidth - textSize.Width) / 2;
            var textY = y + (bayHeight + textSize.Height) / 2;

            gfx.DrawString(bayText, font, XBrushes.Black, textX, textY);
        }

        private double DrawBaySeparator(XGraphics gfx, LayoutParameters layoutParams, double currentY)
        {
            var separatorHeight = layoutParams.CellHeight * 0.2;
            var separatorColor = XColor.FromArgb(229, 231, 235); // Light gray separator

            // Add small gap before separator to avoid overwriting cell borders
            var gapBeforeSeparator = 1.0; // 1 point gap
            var separatorY = currentY + gapBeforeSeparator;

            var currentX = layoutParams.Margin;
            foreach (var rowLabel in layoutParams.RowOrder)
            {
                var width = string.IsNullOrEmpty(rowLabel) ? layoutParams.SpacerWidth : layoutParams.CellWidth;
                if (!string.IsNullOrEmpty(rowLabel))
                {
                    // Draw separator with slight inset to not overlap cell borders
                    var inset = 0.5; // 0.5 point inset from each side
                    gfx.DrawRectangle(new XSolidBrush(separatorColor),
                        currentX + inset, separatorY, width - (2 * inset), separatorHeight);
                }
                currentX += width;
            }

            // Bay column separator with same inset
            var inset2 = 0.5;
            gfx.DrawRectangle(new XSolidBrush(separatorColor),
                currentX + inset2, separatorY, layoutParams.CellWidth - (2 * inset2), separatorHeight);

            return currentY + gapBeforeSeparator + separatorHeight;
        }

        private void DrawNoDataMessage(XGraphics gfx, LayoutParameters layoutParams, double startY)
        {
            var font = new XFont("Arial", 12, XFontStyleEx.Regular);
            var message = "No locations found matching the selected criteria";
            var messageSize = gfx.MeasureString(message, font);

            var x = layoutParams.Margin + (layoutParams.AvailableWidth - messageSize.Width) / 2;
            var y = startY + (layoutParams.AvailableHeight - startY) / 2;

            gfx.DrawString(message, font, XBrushes.Gray, x, y);
        }

        private byte[] SaveDocument(PdfDocument document)
        {
            using var stream = new MemoryStream();
            document.Save(stream);
            document.Close();
            return stream.ToArray();
        }

        // Supporting classes
        private class LayoutParameters
        {
            public List<string> RowOrder { get; set; }
            public int MaxBay { get; set; }
            public int MaxLevel { get; set; }
            public double CellWidth { get; set; }
            public double CellHeight { get; set; }
            public double SpacerWidth { get; set; }
            public double AvailableWidth { get; set; }
            public double AvailableHeight { get; set; }
            public double HeaderHeight { get; set; }
            public double PageWidth { get; set; }
            public double PageHeight { get; set; }
            public double Margin { get; set; }
        }

        private class PageLayout
        {
            public int TotalPages { get; set; }
            public int BaysPerPage { get; set; }
            public List<PageData> Pages { get; set; }

            public PageData GetPageData(int pageIndex) => Pages[pageIndex];
        }

        private class PageData
        {
            public int StartBay { get; set; }
            public int EndBay { get; set; }
            public int BayCount { get; set; }
        }

        private double DrawHeader(XGraphics gfx, LocationGridPDFRequest request, double x, double y, double width, string pageInfo = "")
        {
            var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);
            var subtitleFont = new XFont("Arial", 10, XFontStyleEx.Regular);

            // Zone title with optional page info
            var title = $"Location Grid Layout - {request.Zone.Warehouse?.Name} / {request.Zone.Name}{pageInfo}";
            gfx.DrawString(title, titleFont, XBrushes.Black, x, y);
            y += 25;

            // Generated timestamp
            var timestamp = $"Generated: {request.GeneratedAt:yyyy-MM-dd HH:mm:ss}";
            gfx.DrawString(timestamp, subtitleFont, XBrushes.Gray, x, y);

            // Filter info (right aligned)
            if (!request.IncludeAllLocations)
            {
                var filterInfo = GetFilterInfoText(request.Filters);
                var filterSize = gfx.MeasureString(filterInfo, subtitleFont);
                gfx.DrawString(filterInfo, subtitleFont, XBrushes.Gray, x + width - filterSize.Width, y);
            }
            else
            {
                var allLocationsText = "Showing: All locations in zone";
                var allLocationsSize = gfx.MeasureString(allLocationsText, subtitleFont);
                gfx.DrawString(allLocationsText, subtitleFont, XBrushes.Gray, x + width - allLocationsSize.Width, y);
            }

            return y + 30;
        }
        private double DrawLegend(XGraphics gfx, double x, double y, double width)
        {
            var font = new XFont("Arial", 9, XFontStyleEx.Regular);
            var legendItems = new[]
            {
            ("Available", AvailableColor),
            ("Partial", PartialColor),
            ("Occupied", OccupiedColor)
        };

            gfx.DrawString("Legend:", font, XBrushes.Black, x, y);
            var currentX = x + 50;

            foreach (var (label, color) in legendItems)
            {
                // Draw color box
                gfx.DrawRectangle(new XSolidBrush(color), currentX, y - 8, 12, 12);
                gfx.DrawRectangle(XPens.Black, currentX, y - 8, 12, 12);

                // Draw label
                gfx.DrawString(label, font, XBrushes.Black, currentX + 18, y);
                currentX += 80;
            }

            return y + 25;
        }

        private void DrawGrid(XGraphics gfx, List<LocationGridItemDto> locations, double x, double y, double width, double height)
        {
            if (!locations.Any()) return;

            // Calculate grid parameters
            var maxRow = locations.Where(l => !string.IsNullOrEmpty(l.Row))
                .Select(l => l.Row[0] - 'A' + 1).DefaultIfEmpty(1).Max();
            var maxBay = locations.Any() ? locations.Max(l => l.Bay) : 1;
            var maxLevel = 5;

            // Row spacing configuration (matching your frontend)
            var rowGroups = new[]
            {
            new[] { "P" }, new[] { "O", "N" }, new[] { "M", "L" }, new[] { "K", "J" },
            new[] { "I", "H" }, new[] { "G", "F" }, new[] { "E", "D" }, new[] { "C", "B" }, new[] { "A" }
        };

            var rowOrder = new List<string>();
            for (int i = 0; i < rowGroups.Length; i++)
            {
                rowOrder.AddRange(rowGroups[i]);
                if (i < rowGroups.Length - 1)
                    rowOrder.Add(""); // Spacer
            }

            // Calculate cell dimensions
            var totalCols = rowOrder.Count + 1; // +1 for bay column
            var totalRows = 1 + (maxBay * 6); // header + (5 levels + separator) per bay

            var cellWidth = Math.Min(width / totalCols, 25); // Max 25 points per cell
            var cellHeight = Math.Min(height / totalRows, 20); // Max 20 points per cell

            // Adjust for readability
            cellWidth = Math.Max(cellWidth, 15); // Minimum cell width
            cellHeight = Math.Max(cellHeight, 12); // Minimum cell height

            var font = new XFont("Arial", Math.Max(6, cellHeight * 0.4), XFontStyleEx.Regular);

            // Draw header row
            var currentX = x;
            foreach (var rowLabel in rowOrder)
            {
                if (!string.IsNullOrEmpty(rowLabel))
                {
                    DrawCell(gfx, rowLabel, currentX, y, cellWidth, cellHeight, HeaderColor, font, true);
                }
                currentX += cellWidth;
            }
            DrawCell(gfx, "Bay", currentX, y, cellWidth, cellHeight, HeaderColor, font, true);

            var currentY = y + cellHeight;

            // Draw bay groups
            for (int bay = 1; bay <= maxBay; bay++)
            {
                // Draw levels for this bay
                for (int level = maxLevel; level >= 1; level--)
                {
                    currentX = x;

                    // Draw location cells for each row position
                    foreach (var rowLabel in rowOrder)
                    {
                        if (string.IsNullOrEmpty(rowLabel))
                        {
                            // Spacer
                            currentX += cellWidth;
                            continue;
                        }

                        var location = locations.FirstOrDefault(l =>
                            l.Row == rowLabel && l.Bay == bay && l.Level == level);

                        if (location != null)
                        {
                            var color = GetLocationColor(location.StatusName);
                            var text = !string.IsNullOrEmpty(location.Barcode) ? location.Barcode : location.Code;
                            DrawCell(gfx, text, currentX, currentY, cellWidth, cellHeight, color, font, false);
                        }
                        else
                        {
                            DrawCell(gfx, "-", currentX, currentY, cellWidth, cellHeight, EmptyColor, font, false);
                        }

                        currentX += cellWidth;
                    }

                    // Draw bay number for first level only
                    if (level == maxLevel)
                    {
                        var bayRect = new XRect(currentX, currentY, cellWidth, cellHeight * maxLevel);
                        gfx.DrawRectangle(new XSolidBrush(HeaderColor), bayRect);
                        gfx.DrawRectangle(XPens.Black, bayRect);

                        // Rotate text for bay number
                        var bayText = bay.ToString("00");
                        var textSize = gfx.MeasureString(bayText, font);
                        gfx.DrawString(bayText, font, XBrushes.Black,
                            currentX + cellWidth / 2 - textSize.Width / 2,
                            currentY + (cellHeight * maxLevel) / 2 + textSize.Height / 2);
                    }

                    currentY += cellHeight;
                }

                // Add separator between bays (except last)
                if (bay < maxBay)
                {
                    currentY += cellHeight * 0.3; // Smaller separator
                }
            }
        }

        private void DrawCell(XGraphics gfx, string text, double x, double y, double width, double height,
            XColor backgroundColor, XFont font, bool isHeader)
        {
            var rect = new XRect(x, y, width, height);
            var borderColor = XColor.FromArgb(0, 0, 0); // Black border for all cells
            var borderPen = new XPen(borderColor, 1.0); // Bold 1.0 point border

            // Draw background
            gfx.DrawRectangle(new XSolidBrush(backgroundColor), rect);
            gfx.DrawRectangle(borderPen, rect);

            // Draw text
            if (!string.IsNullOrEmpty(text))
            {
                var textColor = isHeader || backgroundColor == EmptyColor ? XBrushes.Black : XBrushes.White;
                var textSize = gfx.MeasureString(text, font);

                // Center text in cell
                var textX = x + (width - textSize.Width) / 2;
                var textY = y + (height + textSize.Height) / 2;

                gfx.DrawString(text, font, textColor, textX, textY);
            }
        }

        private XColor GetLocationColor(string statusName)
        {
            return statusName switch
            {
                "Available" => AvailableColor,
                "Partial" => PartialColor,
                "Occupied" => OccupiedColor,
                _ => EmptyColor
            };
        }

        private string GetFilterInfoText(LocationGridFilters filters)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(filters.StatusFilter) && filters.StatusFilter != "all")
                parts.Add($"Status: {filters.StatusFilter}");

            if (!string.IsNullOrEmpty(filters.RowFilter) && filters.RowFilter != "all")
                parts.Add($"Row: {filters.RowFilter}");

            if (!string.IsNullOrEmpty(filters.SearchTerm))
                parts.Add($"Search: {filters.SearchTerm}");

            return parts.Any() ? $"Filters: {string.Join(", ", parts)}" : "No filters applied";
        }

        public byte[] GenerateContainerReport(
    GIV_Container container,
    List<GIV_RM_Receive> receives,
    JobVesselCntrInfoDto externalData,
    Dictionary<string, MemoryStream> photoStreams,
    String ClientName)
        {
            var document = new Document();
            document.Info.Title = "Unstuffing Report";
            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4;
            section.PageSetup.Orientation = Orientation.Portrait;
            section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.TopMargin = Unit.FromCentimeter(1.5);
            section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);

            var tempFiles = new List<string>();

            try
            {
                AddPageHeader(section, container, externalData, ClientName);

                var table = section.AddTable();
                table.Borders.Width = 0.75;
                table.Borders.Color = Colors.Gray;
                table.AddColumn(Unit.FromCentimeter(4));   // MaterialNo
                table.AddColumn(Unit.FromCentimeter(5));   // Description
                table.AddColumn(Unit.FromCentimeter(3));   // BatchNo
                table.AddColumn(Unit.FromCentimeter(6));   // MHU

                // Header row
                var hdr = table.AddRow();
                hdr.Shading.Color = Colors.LightGray;
                hdr.Format.Font.Bold = true;
                hdr.Format.Alignment = ParagraphAlignment.Center;
                hdr.Cells[0].AddParagraph("Material No");
                hdr.Cells[1].AddParagraph("Description");
                hdr.Cells[2].AddParagraph("Batch No");
                hdr.Cells[3].AddParagraph("MHU");

                // Process each receive and add to table
                foreach (var receive in receives)
                {
                    var pallets = receive.RM_ReceivePallets.ToList();
                    int palletCount = pallets.Count;
                    int itemCount = pallets.Sum(p => p.RM_ReceivePalletItems.Count);

                    // Collect all pallet codes for this receive
                    var palletCodes = pallets
                        .Select(p => p.PalletCode)
                        .Where(p => !string.IsNullOrWhiteSpace(p))
                        .ToList();
                    string palletCodesText = string.Join(", ", palletCodes);

                    // — Data row for this receive —
                    var dataRow = table.AddRow();
                    dataRow.Cells[0].AddParagraph(receive.RawMaterial?.MaterialNo ?? "-");
                    dataRow.Cells[1].AddParagraph(receive.RawMaterial?.Description ?? "-");
                    dataRow.Cells[2].AddParagraph(receive.BatchNo ?? "-");
                    dataRow.Cells[3].AddParagraph(palletCodesText);

                    // — Totals row for this receive —
                    var totalsRow = table.AddRow();
                    totalsRow.Cells[0].MergeRight = 3;
                    var totalText = totalsRow.Cells[0]
                        .AddParagraph($"Total Pallets: {palletCount}     Total Items: {itemCount}");
                    totalText.Format.Alignment = ParagraphAlignment.Right;


                    // — Spacing before next receive —
                    if (receives.IndexOf(receive) < receives.Count - 1)
                    {
                        var spacer = section.AddParagraph();
                        spacer.Format.SpaceBefore = Unit.FromPoint(2); 
                        spacer.Format.SpaceAfter = Unit.FromPoint(2);
                    }
                }

                var contKeys = container.ContainerPhotos.Select(c => c.PhotoFile).ToList();
                if (contKeys.Any())
                {
                    var contHdr = section.AddParagraph("Container Photos");
                    contHdr.Format.Font.Bold = true;
                    contHdr.Format.SpaceBefore = Unit.FromPoint(12);
                    contHdr.Format.SpaceAfter = Unit.FromPoint(6);
                    contHdr.Format.Borders.Bottom = new Border { Width = 0.75, Color = Colors.LightGray };

                    var contTable = section.AddTable();
                    contTable.Borders.Width = 0;
                    // Reduced column widths
                    contTable.AddColumn(Unit.FromCentimeter(5.0));
                    contTable.AddColumn(Unit.FromCentimeter(5.0));
                    contTable.AddColumn(Unit.FromCentimeter(5.0));

                    Row contRow = null;
                    int cidx = 0;

                    var photoRowHeight = Unit.FromCentimeter(4.5);
                    foreach (var key in contKeys)
                    {
                        if (cidx % 3 == 0)
                        {
                            contRow = contTable.AddRow();
                            
                            contRow.Height = photoRowHeight;
                        }

                        if (photoStreams.TryGetValue(key, out var ms))
                            AddPhotoToCell(contRow.Cells[cidx % 3], ms, key, tempFiles);

                        cidx++;
                    }
                }

                // — Footer & render —
                var footerSpacer = section.AddParagraph();
                footerSpacer.Format.SpaceBefore = Unit.FromCentimeter(0.5);
                AddFooter(section, receives);

                var renderer = new PdfDocumentRenderer(true) { Document = document };
                renderer.RenderDocument();
                using var outStream = new MemoryStream();
                renderer.PdfDocument.Save(outStream);
                return outStream.ToArray();
            }
            finally
            {
                foreach (var f in tempFiles)
                    try { File.Delete(f); } catch { }
            }
        }


        private void AddPhotoToCell(
    Cell cell,
    MemoryStream ms,
    string photoFile,
    List<string> tempFiles)
        {
            var originalFileName = Path.GetFileName(photoFile);
            var tempPath = Path.GetTempFileName();
            var pngPath = Path.ChangeExtension(tempPath, ".png");
            ms.Position = 0;
            using (var img = Image.Load<Rgb24>(ms))
            {
                const int maxW = 280, maxH = 200;
                if (img.Width > maxW || img.Height > maxH)
                {
                    var scale = Math.Min((double)maxW / img.Width, (double)maxH / img.Height);
                    img.Mutate(x => x.Resize((int)(img.Width * scale),
                                             (int)(img.Height * scale),
                                             KnownResamplers.Lanczos3));
                }
                img.Mutate(x => x.Quantize());
                img.Mutate(x => x.GaussianBlur(0.5f));
                img.Metadata.HorizontalResolution = 96;
                img.Metadata.VerticalResolution = 96;
                var pngEncoder = new PngEncoder
                {
                    CompressionLevel = PngCompressionLevel.BestCompression,
                    ColorType = PngColorType.Palette,
                    FilterMethod = PngFilterMethod.Adaptive
                };
                img.Save(pngPath, pngEncoder);
            }

            tempFiles.Add(pngPath);

            var p = cell.AddParagraph();
            p.Format.Alignment = ParagraphAlignment.Center;

            var pdfImage = p.AddImage(pngPath);
            pdfImage.LockAspectRatio = true;
            pdfImage.Width = Unit.FromCentimeter(4.5);

        }


        private void AddPageHeader(Section section, GIV_Container container, JobVesselCntrInfoDto externalData,string ClientName)
        {
            var title = section.AddParagraph("HUP SOON CHEONG SERVICES PTE LTD");
            title.Format.Font.Size = 14;
            title.Format.Font.Bold = true;
            title.Format.Alignment = ParagraphAlignment.Center;
            title.Format.SpaceAfter = Unit.FromPoint(5);

            // Document Title
            var subtitle = section.AddParagraph("UNSTUFFING REPORT");
            subtitle.Format.Font.Size = 12;
            subtitle.Format.Font.Bold = true;
            subtitle.Format.Alignment = ParagraphAlignment.Center;
            subtitle.Format.SpaceAfter = Unit.FromPoint(15);

            // Create header table with better alignment
            var headerTable = section.AddTable();
            headerTable.Borders.Width = 0;
            headerTable.Format.Font.Name = "Arial";
            headerTable.Format.Font.Size = 9;

            // Define columns with fixed widths for better alignment
            headerTable.AddColumn(Unit.FromCentimeter(2)); // Label column
            headerTable.AddColumn(Unit.FromCentimeter(4)); // Value column
            headerTable.AddColumn(Unit.FromCentimeter(2)); // Label column
            headerTable.AddColumn(Unit.FromCentimeter(4)); // Value column
            headerTable.AddColumn(Unit.FromCentimeter(2)); // Label column
            headerTable.AddColumn(Unit.FromCentimeter(4)); // Value column

            // Row 1: Client, Voy No, Date
            var row1 = headerTable.AddRow();
            row1.Cells[0].AddParagraph("Client:");
            row1.Cells[0].Format.Font.Bold = true;
            row1.Cells[1].AddParagraph(ClientName);

            row1.Cells[2].AddParagraph("Voy No:");
            row1.Cells[2].Format.Font.Bold = true;
            row1.Cells[3].AddParagraph(externalData?.VesselInfoInVoy ?? "-");

            row1.Cells[4].AddParagraph("Date:");
            row1.Cells[4].Format.Font.Bold = true;
            row1.Cells[5].AddParagraph(container.UnstuffedDate?.ToString("dd/MM/yy HH:mm") ?? "-");

            // Row 2: Vessel, Seal No, POL
            var row2 = headerTable.AddRow();
            row2.Cells[0].AddParagraph("Vessel:");
            row2.Cells[0].Format.Font.Bold = true;
            row2.Cells[1].AddParagraph(externalData?.VesselInfoFullName ?? "-");

            row2.Cells[2].AddParagraph("Seal No:");
            row2.Cells[2].Format.Font.Bold = true;
            row2.Cells[3].AddParagraph(externalData?.SealNumber ?? "-");

            row2.Cells[4].AddParagraph("POL:");
            row2.Cells[4].Format.Font.Bold = true;
            row2.Cells[5].AddParagraph(externalData?.POL ?? "-");

            // Row 3: Cntr No, Size, HBL (moved from table)
            var row3 = headerTable.AddRow();
            row3.Cells[0].AddParagraph("Cntr No:");
            row3.Cells[0].Format.Font.Bold = true;
            row3.Cells[1].AddParagraph(container.ContainerNo_GW);

            row3.Cells[2].AddParagraph("Size:");
            row3.Cells[2].Format.Font.Bold = true;
            row3.Cells[3].AddParagraph(externalData?.ContainerSize.ToString() ?? "-");

            // Row 4: Your Ref, ETA, Marks (moved from table)
            var row4 = headerTable.AddRow();
            row4.Cells[0].AddParagraph("Your Ref:");
            row4.Cells[0].Format.Font.Bold = true;
            row4.Cells[1].AddParagraph(externalData?.YourRef ?? "-");

            row4.Cells[2].AddParagraph("ETA:");
            row4.Cells[2].Format.Font.Bold = true;
            row4.Cells[3].AddParagraph(externalData?.VesselInfoETA?.ToString("dd/MM/yyyy HH:mm") ?? "-");


            var row5 = headerTable.AddRow();
            row5.Cells[0].AddParagraph("HBL:");
            row5.Cells[0].Format.Font.Bold = true;
            row5.Cells[1].AddParagraph(externalData?.HBL ?? "-");

            row5.Cells[2].AddParagraph("PO:");
            row5.Cells[2].Format.Font.Bold = true;
            row5.Cells[3].AddParagraph(externalData?.Marks ?? "-");

            // Add space after header
            var spacer = section.AddParagraph();
            spacer.Format.SpaceAfter = Unit.FromCentimeter(0.5);
        }



        private void AddFooter(Section section, List<GIV_RM_Receive> receives)
        {
            // Add separator line
            var separator = section.AddParagraph();
            separator.Format.SpaceBefore = Unit.FromCentimeter(1);
            separator.Format.SpaceAfter = Unit.FromCentimeter(0.5);
            separator.Format.Borders.Bottom = new Border { Width = 1, Color = Colors.LightGray };

            // Get handlers names
            var names = receives
                .SelectMany(r => r.RM_ReceivePallets)
                .Select(p => p.HandledBy)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            // Create footer paragraph for better alignment
            var footerPara = section.AddParagraph();

            // Tallied By
            footerPara.AddFormattedText("Tallied By: ", TextFormat.Bold);
            footerPara.AddText(names.Any() ? string.Join(", ", names) : "-");
            footerPara.AddLineBreak();

            // Verified By
            footerPara.AddFormattedText("Verified By: ", TextFormat.Bold);
            footerPara.AddText("__________________");

            // Add page numbers
            var footer = section.Footers.Primary.AddParagraph();
            footer.AddText("Page ");
            footer.AddPageField();
            footer.AddText(" of ");
            footer.AddNumPagesField();
            footer.Format.Alignment = ParagraphAlignment.Right;
            footer.Format.Font.Size = 8;
        }



    }
}
