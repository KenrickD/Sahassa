using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceiveItemPhoto;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.SkuDto;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_FinishedGood.Web;
using WMS.Domain.DTOs.GIV_Invoicing;
using WMS.Domain.DTOs.GIV_RawMaterial;
//using WMS.Domain.DTOs.GIV_RawMaterial.Web;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;

using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Services
{
    public class FinishedGoodService : IFinishedGoodService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly AwsS3Config _awsS3Config;
        private readonly IAwsS3Service _awsS3Service;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<FinishedGoodService> _logger;
        private readonly ILocationService _locationService;
        private IConfiguration _configuration;
        public FinishedGoodService(
            AppDbContext dbContext,
            IMapper mapper,
            IConfiguration configuration,
            ICurrentUserService currentUserService,
            IOptions<AwsS3Config> config,
            IAwsS3Service s3Service,
            ILogger<FinishedGoodService> logger,
            ILocationService locationService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _configuration = configuration;
            _currentUserService = currentUserService;
            _awsS3Config = config.Value;
            _awsS3Service = s3Service;
            _logger = logger;
            _locationService = locationService;
        }
        // Service Method (Refactored to use AwsS3Service)
        public async Task<ApiResponseDto<string>> CreateFinishedGoodAsync(
    List<FG_ReceiveCreateDto> receiveDtos,
    List<FG_ReceivePalletPhotoUploadDto> photos,
    string username,
    Guid warehouseId)
        {
            _logger.LogInformation("Starting CreateFinishedGoodsAsync by user {Username} for warehouse {WarehouseId}", username, warehouseId);

            try
            {
                var now = DateTime.UtcNow.ToLocalTime();

                var warehouse = await _dbContext.Warehouses
                    .FirstOrDefaultAsync(w => w.Id == warehouseId && !w.IsDeleted);
                if (warehouse == null)
                {
                    return ApiResponseDto<string>.ErrorResult($"Warehouse with ID '{warehouseId}' not found.");
                }

                var client = await _dbContext.Clients
                    .FirstOrDefaultAsync(c => c.Id == _currentUserService.CurrentClientId && !c.IsDeleted);
                if (client == null)
                {
                    return ApiResponseDto<string>.ErrorResult($"User client not found.");
                }

                var packageTypeCodeType = await _dbContext.GeneralCodeTypes
                    .FirstOrDefaultAsync(t => t.Name == AppConsts.GeneralCodeType.PRODUCT_TYPE && !t.IsDeleted);

                if (packageTypeCodeType == null)
                {
                    return ApiResponseDto<string>.ErrorResult("System setup missing: 'Product Type' code group.");
                }

                var packageTypeLookup = await _dbContext.GeneralCodes
                    .Where(c => c.GeneralCodeTypeId == packageTypeCodeType.Id && !c.IsDeleted)
                    .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id);

                var allIncomingPalletCodes = receiveDtos
                    .SelectMany(r => r.Pallets)
                    .Where(p => !string.IsNullOrWhiteSpace(p.PalletCode))
                    .Select(p => p.PalletCode!.Trim())
                    .ToList();

                var existingPalletCodes = await _dbContext.GIV_FG_ReceivePallets
                    .Where(p => allIncomingPalletCodes.Contains(p.PalletCode!) && !p.IsDeleted)
                    .Select(p => p.PalletCode!)
                    .ToListAsync();

                if (existingPalletCodes.Any())
                {
                    return ApiResponseDto<string>.ErrorResult(
                        "One or more pallet codes already exist.",
                        existingPalletCodes.Select(code => $"PalletCode '{code}' already exists.").ToList());
                }

                var invalidPhotos = photos.Where(p => string.IsNullOrWhiteSpace(p.PalletCode)).ToList();
                if (invalidPhotos.Any())
                {
                    return ApiResponseDto<string>.ErrorResult(
                        "One or more photo entries have missing PalletCode.",
                        invalidPhotos.Select((_, i) => $"Photo index {i} is missing a valid PalletCode.").ToList());
                }

                var receives = new List<GIV_FG_Receive>();
                var pallets = new List<GIV_FG_ReceivePallet>();
                var items = new List<GIV_FG_ReceivePalletItem>();
                var photoEntities = new List<GIV_FG_ReceivePalletPhoto>();

                foreach (var dto in receiveDtos)
                {
                    Guid? packageTypeId = null;
                    if (!string.IsNullOrWhiteSpace(dto.PackageType) &&
                        packageTypeLookup.TryGetValue(dto.PackageType.Trim(), out var id))
                    {
                        packageTypeId = id;
                    }

                    var receive = new GIV_FG_Receive
                    {
                        Id = Guid.NewGuid(),
                        BatchNo = dto.BatchNo,
                        ReceivedDate = DateTime.SpecifyKind(dto.ReceivedDate, DateTimeKind.Utc),
                        ReceivedBy = dto.ReceivedBy,
                        CreatedBy = username,
                        CreatedAt = now,
                        IsDeleted = false,
                        WarehouseId = warehouseId,
                        PO = dto.PO?.Trim(),
                        PackageTypeId = packageTypeId
                    };

                    receives.Add(receive);

                    foreach (var palletDto in dto.Pallets)
                    {
                        var pallet = new GIV_FG_ReceivePallet
                        {
                            Id = Guid.NewGuid(),
                            PalletCode = palletDto.PalletCode?.Trim(),
                            HandledBy = palletDto.HandledBy,
                            ReceivedBy = palletDto.ReceivedBy,
                            ReceivedDate = palletDto.ReceivedDate.HasValue
    ? DateTime.SpecifyKind(palletDto.ReceivedDate.Value, DateTimeKind.Utc)
    : now,
                            PackSize = palletDto.PackSize ?? 1,
                            CreatedBy = username,
                            CreatedAt = now,
                            IsDeleted = false,
                            WarehouseId = warehouseId,
                            Receive = receive
                        };

                        pallets.Add(pallet);

                        if (palletDto.Items != null && palletDto.Items.Any())
                        {
                            foreach (var itemDto in palletDto.Items)
                            {
                                if (itemDto != null)
                                {
                                    items.Add(new GIV_FG_ReceivePalletItem
                                    {
                                        Id = Guid.NewGuid(),
                                        ItemCode = itemDto.ItemCode,
                                        BatchNo = itemDto.BatchNo,
                                        ProdDate = itemDto.ProdDate == null ? null : DateTime.SpecifyKind(itemDto.ProdDate.Value, DateTimeKind.Utc),
                                        Remarks = itemDto.Remarks,
                                        CreatedBy = username,
                                        CreatedAt = now,
                                        IsDeleted = false,
                                        WarehouseId = warehouseId,
                                        GIV_FG_ReceivePallet = pallet
                                    });
                                }
                            }
                        }

                        foreach (var photoDto in photos.Where(p => p.PalletCode == pallet.PalletCode))
                        {
                            var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(photoDto.PhotoFile.FileName)}";
                            var key = $"{_awsS3Config.FolderEnvironment}/{warehouse.Code}/{client.Code}/FG/{receive.Id}/{pallet.PalletCode}/{uniqueFileName}";

                            var s3Key = await _awsS3Service.UploadFileAsync(photoDto.PhotoFile, key);
                            var fileType = _awsS3Service.DetermineFileType(photoDto.PhotoFile);

                            photoEntities.Add(new GIV_FG_ReceivePalletPhoto
                            {
                                Id = Guid.NewGuid(),
                                PhotoFile = s3Key,
                                FileName = photoDto.PhotoFile.FileName,
                                ContentType = photoDto.PhotoFile.ContentType,
                                FileSizeBytes = photoDto.PhotoFile.Length,
                                FileType = fileType,
                                CreatedBy = username,
                                CreatedAt = now,
                                IsDeleted = false,
                                WarehouseId = warehouseId,
                                ReceivePallet = pallet
                            });
                        }
                    }
                }

                await _dbContext.GIV_FG_Receives.AddRangeAsync(receives);
                await _dbContext.GIV_FG_ReceivePallets.AddRangeAsync(pallets);
                await _dbContext.GIV_FG_ReceivePalletItems.AddRangeAsync(items);
                await _dbContext.GIV_FG_ReceivePalletPhotos.AddRangeAsync(photoEntities);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Finished Goods and Pallets created successfully: Count={Count}", receives.Count);

                return ApiResponseDto<string>.SuccessResult("OK", $"Successfully created {receives.Count} receive(s).");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateFinishedGoodsAsync");
                return ApiResponseDto<string>.ErrorResult("Unexpected error occurred.", new List<string> { ex.Message });
            }
        }










        public async Task<List<GIV_FinishedGood>> GetAllFinishedGoodsAsync()
        {
            _logger.LogInformation("Fetching all finished goods with receives, pallets, and items.");
            try
            {
                var result = await _dbContext.GIV_FinishedGoods
                    .Include(fg => fg.FG_Receive)
                        .ThenInclude(r => r.FG_ReceivePallets)
                            .ThenInclude(p => p.FG_ReceivePalletItems)
                    .Where(fg => !fg.IsDeleted)
                    .ToListAsync();

                _logger.LogInformation("Fetched {Count} finished goods.", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching finished goods.");
                throw;
            }
        }

        public async Task<List<FinishedGoodDetailsDto>> GetFinishedGoodsAsync()
        {
            _logger.LogInformation("Mapping finished goods to DTOs.");
            try
            {
                var entities = await GetAllFinishedGoodsAsync();
                var dtoList = _mapper.Map<List<FinishedGoodDetailsDto>>(entities);
                _logger.LogInformation("Mapped {Count} finished goods to DTOs.", dtoList.Count);
                return dtoList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while mapping finished goods to DTOs.");
                throw;
            }
        }

        public async Task<(bool success, string message, List<string> url)> GetPalletPhotoPathByIdAsync(Guid palletId)
        {
            var pallet = await _dbContext.GIV_FG_ReceivePallets
        .Include(p => p.FG_ReceivePalletPhotos)
        .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            if (pallet == null || !pallet.FG_ReceivePalletPhotos.Any())
                return (false, "No photo found for this pallet", new());

            var photoKeys = pallet.FG_ReceivePalletPhotos
                .Where(p => !string.IsNullOrWhiteSpace(p.PhotoFile))
                .Select(p => p.PhotoFile)
                .ToList();

            if (!photoKeys.Any())
                return (false, "No valid photo keys found", new());

            var urls = await _awsS3Service.GenerateMultiplePresignedUrlsAsync(photoKeys, FileType.Photo);
            return (true, "Photos found", urls.Values.ToList());
        }

        public async Task<List<FG_ReceivePalletDetailsDto>> GetPalletsByIdAsync(Guid ReceiveId)
        {
            _logger.LogInformation("Fetching finished good pallets by receive ID: {ReceiveId}", ReceiveId);

            var pallets = await _dbContext.GIV_FG_ReceivePallets
                .Where(p => p.ReceiveId == ReceiveId && !p.IsDeleted)
                .Include(p => p.FG_ReceivePalletItems)
                .ToListAsync();

            var dto = pallets.Select(p => new FG_ReceivePalletDetailsDto
            {
                Id = p.Id,
                PalletCode = p.PalletCode,
                FG_ReceivePalletItems = p.FG_ReceivePalletItems
                    .Select(i => new FG_ReceivePalletItemDetailsDto
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode
                    }).ToList()
            }).ToList();

            _logger.LogInformation("Found {Count} pallets for receive ID: {ReceiveId}", dto.Count, ReceiveId);

            return dto;
        }


        public async Task<FinishedGoodDetailsDto> GetFinishedGoodDetailsByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching finished good details by ID: {Id}", id);

            var finishedGood = await _dbContext.GIV_FinishedGoods
                .Include(rm => rm.FG_Receive)
                    .ThenInclude(r => r.FG_ReceivePallets)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                .FirstOrDefaultAsync(rm => rm.Id == id);

            var dto = _mapper.Map<FinishedGoodDetailsDto>(finishedGood);

            _logger.LogInformation("Fetched finished good details for ID: {Id}", id);

            return dto;
        }





        public async Task<List<FinishedGoodItemDto>> GetItemsByReceive(Guid receiveId)
        {
            _logger.LogInformation("Fetching finished good items by receive ID: {ReceiveId}", receiveId);

            var items = await _dbContext.GIV_FG_ReceivePalletItems
                .Include(i => i.GIV_FG_ReceivePallet)
                    .ThenInclude(p => p.Location)
                .Where(i => i.GIV_FG_ReceivePallet.ReceiveId == receiveId )
                .Select(i => new FinishedGoodItemDto
                {
                    HU = i.ItemCode,
                    BatchNo = i.BatchNo,
                    MHU = i.GIV_FG_ReceivePallet.PalletCode,
                    ProdDate = i.ProdDate,
                    IsReleased = i.IsReleased,
                    Remarks = i.Remarks,
                    Location = i.GIV_FG_ReceivePallet.Location != null
                        ? i.GIV_FG_ReceivePallet.Location.Barcode
                        : null
                }).ToListAsync();

            _logger.LogInformation("Fetched {Count} items for receive ID: {ReceiveId}", items.Count, receiveId);

            return items;
        }


        public async Task<List<FinishedGoodGroupedItemDto>> GetGroupedItemsByReceive(Guid receiveId)
        {
            _logger.LogInformation("Getting grouped items for receive ID: {ReceiveId}", receiveId);

            var items = await GetItemsByReceive(receiveId);

            var grouped = items
                .GroupBy(i => i.BatchNo)
                .Select(g => new FinishedGoodGroupedItemDto
                {
                    BatchNo = g.Key ?? "N/A",
                    HUList = g.Select(x => x.HU).Distinct().ToList(),
                    MHUList = g.Select(x => x.MHU).Distinct().ToList(),
                    Qty = g.Count(),
                    BalQty = g.Count(x => !x.IsReleased),
                    ProdDate = g.First().ProdDate,
                    DG = g.Select(x => x.DG).ToList(),
                    Remarks = g.Select(x => x.Remarks).ToList(),
                    Location = g.Select(x => x.Location).ToList()
                }).ToList();

            _logger.LogInformation("Grouped {GroupCount} batch groups for receive ID: {ReceiveId}", grouped.Count, receiveId);
            return grouped;
        }

        public async Task<(List<FG_ReceiveGroupDto> UnassignedGroups, List<SkuDto> Skus)> GetUnassignedPalletsAndSkusAsync()
        {
            var grouped = await _dbContext.GIV_FG_ReceivePallets
                .Where(p => p.ReceiveId != null && p.Receive.FinishedGoodId == null)
                .GroupBy(p => p.ReceiveId)
                .Select(g => new FG_ReceiveGroupDto
                {
                    ReceiveId = g.Key!.Value,
                    ReceivedDate = g.First().ReceivedDate,
                    Pallets = g.Select(p => new PalletDto
                    {
                        Id = p.Id,
                        PalletCode = p.PalletCode ?? "(No Code)"
                    }).ToList()
                })
                .ToListAsync();

            var skus = await _dbContext.GIV_FinishedGoods
                .Select(fg => new SkuDto
                {
                    Id = fg.Id,
                    SKU = fg.SKU ?? "(No SKU)"
                })
                .ToListAsync();

            return (grouped, skus);
        }

        public async Task<List<FG_ReceiveGroupDto>> GetPalletsBySkuAsync(Guid skuId)
        {
            _logger.LogInformation("Fetching assigned pallets for SKU: {SkuId}", skuId);

            var pallets = await _dbContext.GIV_FG_ReceivePallets
                .Where(p => p.ReceiveId != null &&
                            p.Receive.FinishedGoodId == skuId)
                .Select(p => new PalletDto
                {
                    Id = p.Id,
                    PalletCode = p.PalletCode ?? "(No Code)",
                    ReceiveId = p.ReceiveId!.Value,
                    ReceivedDate = p.ReceivedDate
                })
                .ToListAsync();

            var grouped = pallets
                .GroupBy(p => p.ReceiveId)
                .Select(g => new FG_ReceiveGroupDto
                {
                    ReceiveId = g.Key,
                    ReceivedDate = g.First().ReceivedDate,
                    Pallets = g.ToList()
                })
                .ToList();

            return grouped;
        }


        public async Task AssignPalletsToSkuAsync(Guid skuId, List<Guid> palletIds, List<Guid> unassignedReceiveIds)
        {
            _logger.LogInformation("Assigning pallets to SKU: {SkuId}, unassigning {UnassignedCount} receives", skuId, unassignedReceiveIds.Count);

            // Assign pallets by updating their receive's FinishedGoodId
            var selectedPallets = await _dbContext.GIV_FG_ReceivePallets
                .Where(p => palletIds.Contains(p.Id))
                .Include(p => p.Receive)
                .ToListAsync();

            foreach (var pallet in selectedPallets)
            {
                if (pallet.Receive != null)
                    pallet.Receive.FinishedGoodId = skuId;
            }

            // Unassign receives
            var unassignedReceives = await _dbContext.GIV_FG_Receives
                .Where(r => unassignedReceiveIds.Contains(r.Id))
                .ToListAsync();

            foreach (var r in unassignedReceives)
            {
                r.FinishedGoodId = null;
            }

            await _dbContext.SaveChangesAsync();
        }



        public async Task<List<ReceiveViewDto>> GetReceivesBySkuAsync(Guid skuId)
        {
            _logger.LogInformation("Fetching receives for SKU: {SkuId}", skuId);

            var receives = await _dbContext.GIV_FG_Receives
                .Include(r => r.FG_ReceivePallets)
                    .ThenInclude(p => p.FG_ReceivePalletItems)
                .Where(r => r.FinishedGoodId == skuId)
                .ToListAsync();

            var result = receives.Select(r =>
            {
                var allItems = r.FG_ReceivePallets.SelectMany(p => p.FG_ReceivePalletItems);
                int qty = allItems.Count();
                int plt = r.FG_ReceivePallets.Count;
                int totalPackSize = r.FG_ReceivePallets.Sum(p => p.PackSize);
                int usedQty = 0;  // Placeholder
                int usedPlt = 0;  // Placeholder

                return new ReceiveViewDto
                {
                    ReceivedDate = r.ReceivedDate,
                    PackSize = totalPackSize,
                    Qty = qty,
                    Plt = plt,
                    BalQty = qty - usedQty,
                    BalPlt = plt - usedPlt
                };
            }).ToList();

            _logger.LogInformation("Retrieved {Count} receives for SKU: {SkuId}", result.Count, skuId);
            return result;
        }

        public async Task<FinishedGoodReleaseDto> GetFinishedGoodReleaseDetailsAsync(Guid finishedgoodId)
        {
            _logger.LogInformation("Fetching finished good release details for ID: {Id}", finishedgoodId);

            var finishedGood = await _dbContext.GIV_FinishedGoods
                .Include(r => r.FG_Receive)
                    .ThenInclude(rcv => rcv.FG_ReceivePallets)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                .FirstOrDefaultAsync(r => r.Id == finishedgoodId);

            return new FinishedGoodReleaseDto
            {
                FinishedGoodId = finishedGood.Id,
                SKU = finishedGood.SKU,
                Description = finishedGood.Description,
                Receives = finishedGood.FG_Receive.Select(rcv => new FG_ReceiveReleaseDto
                {
                    Id = rcv.Id,
                    BatchNo = rcv.BatchNo,
                    ReceivedBy = rcv.ReceivedBy,
                    ReceivedDate = rcv.ReceivedDate,
                    Pallets = rcv.FG_ReceivePallets.Select(p => new FG_PalletReleaseDto
                    {
                        Id = p.Id,
                        PalletCode = p.PalletCode,
                        IsReleased = p.IsReleased,
                        HandledBy = p.HandledBy,
                        Items = p.FG_ReceivePalletItems.Select(i => new FG_PalletItemReleaseDto
                        {
                            Id = i.Id,
                            ItemCode = i.ItemCode,
                            IsReleased = i.IsReleased
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<ServiceWebResult> ReleaseFinishedGoodAsync(FinishedGoodReleaseSubmitDto dto, string userId)
        {
            _logger.LogInformation("Releasing finished good: {FinishedGoodId} by {UserId}", dto.FinishedGoodId, userId);
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var isToday = DateTime.UtcNow.Date;
                var allReleases = new List<GIV_FG_Release>();

                // Track which pallets are being explicitly released as whole units
                var explicitlyReleasedPallets = dto.PalletReleases?
                    .Select(pr => pr.PalletId)
                    .ToHashSet() ?? new HashSet<Guid>();

                // Group releases by date
                var releasesByDate = new Dictionary<DateTime, (List<Guid> PalletIds, List<Guid> ItemIds)>();

                // Process pallet releases and group by date
                if (dto.PalletReleases != null && dto.PalletReleases.Any())
                {
                    foreach (var palletRelease in dto.PalletReleases)
                    {
                        if (!DateTime.TryParse(palletRelease.ReleaseDate, out DateTime releaseDate))
                        {
                            releaseDate = DateTime.UtcNow.AddDays(1); // Default to tomorrow if invalid date
                        }

                        // Standardize the date
                        releaseDate = releaseDate.Date;

                        if (!releasesByDate.ContainsKey(releaseDate))
                        {
                            releasesByDate[releaseDate] = (new List<Guid>(), new List<Guid>());
                        }

                        releasesByDate[releaseDate].PalletIds.Add(palletRelease.PalletId);
                    }
                }

                // Process item releases and group by date
                if (dto.ItemIds != null && dto.ItemIds.Any() && dto.ItemReleaseDates != null)
                {
                    foreach (var itemId in dto.ItemIds)
                    {
                        // Get the item to check its pallet
                        var item = await _dbContext.GIV_FG_ReceivePalletItems
                            .Where(i => i.Id == itemId && !i.IsReleased)
                            .Select(i => new { i.Id, i.GIV_FG_ReceivePalletId })
                            .FirstOrDefaultAsync();

                        if (item == null) continue; // Skip if not found or already released

                        // Skip items whose pallets are being explicitly released
                        if (explicitlyReleasedPallets.Contains(item.GIV_FG_ReceivePalletId))
                            continue;

                        // Get release date for this item
                        if (!dto.ItemReleaseDates.TryGetValue(itemId.ToString(), out string releaseDateStr) ||
                            !DateTime.TryParse(releaseDateStr, out DateTime releaseDate))
                        {
                            releaseDate = DateTime.UtcNow.AddDays(1); // Default to tomorrow if invalid date
                        }

                        // Standardize the date
                        releaseDate = releaseDate.Date;

                        if (!releasesByDate.ContainsKey(releaseDate))
                        {
                            releasesByDate[releaseDate] = (new List<Guid>(), new List<Guid>());
                        }

                        releasesByDate[releaseDate].ItemIds.Add(itemId);
                    }
                }

                // Process each date group
                foreach (var dateGroup in releasesByDate)
                {
                    var releaseDate = dateGroup.Key;
                    var palletIds = dateGroup.Value.PalletIds;
                    var itemIds = dateGroup.Value.ItemIds;

                    // Skip if nothing to release on this date
                    if (!palletIds.Any() && !itemIds.Any()) continue;

                    // Create a release record for this date
                    var releaseRecord = new GIV_FG_Release
                    {
                        Id = Guid.NewGuid(),
                        GIV_FinishedGoodId = dto.FinishedGoodId,
                        ReleaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc),
                        ReleasedBy = userId,
                        WarehouseId = Guid.Empty, // Set this later
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        GIV_FG_ReleaseDetails = new List<GIV_FG_ReleaseDetails>()
                    };

                    // Process pallets for this date
                    if (palletIds.Any())
                    {
                        var pallets = await _dbContext.GIV_FG_ReceivePallets
                            .Include(p => p.FG_ReceivePalletItems)
                            .Include(p => p.Receive)
                            .Where(p => palletIds.Contains(p.Id) && !p.IsReleased)
                            .ToListAsync();

                        if (pallets.Any())
                        {
                            // Set warehouse ID if not already set
                            if (releaseRecord.WarehouseId == Guid.Empty)
                            {
                                releaseRecord.WarehouseId = pallets.First().WarehouseId;
                            }

                            foreach (var pallet in pallets)
                            {
                                // Create a detail record for the entire pallet
                                var releaseDetail = new GIV_FG_ReleaseDetails
                                {
                                    Id = Guid.NewGuid(),
                                    GIV_FG_ReleaseId = releaseRecord.Id,
                                    GIV_FG_ReceiveId = pallet.ReceiveId.Value,
                                    GIV_FG_ReceivePalletId = pallet.Id,
                                    // Note: No need to set GIV_FG_ReceivePalletItemId for entire pallet releases
                                    IsEntirePallet = true,
                                    WarehouseId = pallet.WarehouseId,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId
                                };

                                releaseRecord.GIV_FG_ReleaseDetails.Add(releaseDetail);
                            }
                        }
                    }

                    // Process individual items for this date
                    if (itemIds.Any())
                    {
                        var items = await _dbContext.GIV_FG_ReceivePalletItems
                            .Include(i => i.GIV_FG_ReceivePallet)
                                .ThenInclude(p => p.Receive)
                            .Where(i => itemIds.Contains(i.Id) && !i.IsReleased)
                            .ToListAsync();

                        if (items.Any())
                        {
                            // Set warehouse ID if not already set
                            if (releaseRecord.WarehouseId == Guid.Empty)
                            {
                                releaseRecord.WarehouseId = items.First().GIV_FG_ReceivePallet.WarehouseId;
                            }

                            // Process each item
                            foreach (var item in items)
                            {
                                var pallet = item.GIV_FG_ReceivePallet;
                                var receive = pallet.Receive;

                                if (receive == null) continue;

                                // Create a detail record for this individual item
                                var releaseDetail = new GIV_FG_ReleaseDetails
                                {
                                    Id = Guid.NewGuid(),
                                    GIV_FG_ReleaseId = releaseRecord.Id,
                                    GIV_FG_ReceiveId = receive.Id,
                                    GIV_FG_ReceivePalletId = pallet.Id,
                                    GIV_FG_ReceivePalletItemId = item.Id,
                                    IsEntirePallet = false,
                                    WarehouseId = pallet.WarehouseId,
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId
                                };

                                releaseRecord.GIV_FG_ReleaseDetails.Add(releaseDetail);
                            }
                        }
                    }

                    // Add the release record to the list
                    if (releaseRecord.GIV_FG_ReleaseDetails.Any())
                    {
                        allReleases.Add(releaseRecord);
                    }
                }

                // Save all changes
                _dbContext.GIV_FG_Releases.AddRange(allReleases);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Finished good released successfully: {FinishedGoodId}, {Count} release records created",
                    dto.FinishedGoodId, allReleases.Count);

                return new ServiceWebResult { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error releasing finished good: {FinishedGoodId}", dto.FinishedGoodId);
                return new ServiceWebResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        public async Task ProcessFinishedGoodReleases(DateTime today, CancellationToken stoppingToken)
        {
            // Convert the input date to UTC if it's not already
            DateTime todayUtc = today.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(today, DateTimeKind.Utc)
                : today.ToUniversalTime();

            _logger.LogInformation("Processing finished good releases scheduled for today or earlier: {Today}", todayUtc);

            // Get all FG releases scheduled for today or earlier
            var pendingReleases = await _dbContext.GIV_FG_Releases
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePallet)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                .Where(r => r.ReleaseDate.Date <= todayUtc.Date)
                .ToListAsync(stoppingToken);

            _logger.LogInformation("Found {Count} pending finished good releases to process", pendingReleases.Count);

            if (pendingReleases.Count == 0)
            {
                _logger.LogInformation("No pending finished good releases found for today or earlier.");
                return;
            }

            int palletCount = 0;
            int itemCount = 0;

            foreach (var release in pendingReleases)
            {
                _logger.LogInformation("Processing FG release {ReleaseId} scheduled for {ReleaseDate} with {DetailCount} details",
                    release.Id, release.ReleaseDate, release.GIV_FG_ReleaseDetails.Count);

                // Process all release details
                foreach (var detail in release.GIV_FG_ReleaseDetails)
                {
                    var pallet = detail.GIV_FG_ReceivePallet;

                    if (pallet != null && !pallet.IsReleased)
                    {
                        _logger.LogInformation("Releasing FG pallet {PalletId}", pallet.Id);

                        pallet.IsReleased = true;
                        palletCount++;

                        // Ensure pallet items are loaded
                        if (!_dbContext.Entry(pallet).Collection(p => p.FG_ReceivePalletItems).IsLoaded)
                        {
                            await _dbContext.Entry(pallet).Collection(p => p.FG_ReceivePalletItems).LoadAsync(stoppingToken);
                        }

                        // Release all items in the pallet that aren't already released
                        foreach (var item in pallet.FG_ReceivePalletItems)
                        {
                            if (!item.IsReleased)
                            {
                                item.IsReleased = true;
                                itemCount++;
                                _logger.LogInformation("Releasing FG item {ItemId} in pallet {PalletId}", item.Id, pallet.Id);
                            }
                        }
                    }
                    else if (pallet != null && pallet.IsReleased)
                    {
                        _logger.LogInformation("FG Pallet {PalletId} is already released, skipping", pallet.Id);
                    }
                    else
                    {
                        _logger.LogWarning("FG Release detail {DetailId} has no associated pallet", detail.Id);
                    }
                }
            }

            if (palletCount > 0 || itemCount > 0)
            {
                await _dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Finished Good scheduled releases processed: {PalletCount} pallets and {ItemCount} items marked as released",
                    palletCount, itemCount);
            }
            else
            {
                _logger.LogInformation("No new finished good releases to process. All pallets in scheduled releases are already released.");
            }
        }

        //Paginations
        public async Task<PaginatedResult<FinishedGoodDetailsDto>> GetPaginatedFinishedGoodsAsync(int start, int length, string search, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Fetching paginated finished goods: start={Start}, length={Length}, search='{Search}', sortColumn={SortColumn}, sortAscending={SortAscending}",
                start, length, search, sortColumn, sortAscending);

            var query = _dbContext.GIV_FinishedGoods
                .Include(fg => fg.FG_Receive)
                    .ThenInclude(r => r.FG_ReceivePallets)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                .Where(fg => !fg.IsDeleted);

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(fg => fg.SKU.Contains(search) || fg.Description.Contains(search));
            }

            var totalCount = await query.CountAsync();

            query = sortColumn switch
            {
                0 => (sortAscending ? query.OrderBy(fg => fg.SKU) : query.OrderByDescending(fg => fg.SKU)),
                1 => (sortAscending ? query.OrderBy(fg => fg.Description) : query.OrderByDescending(fg => fg.Description)),
                _ => query.OrderByDescending(fg => fg.CreatedAt)
            };

            var paged = await query.Skip(start).Take(length).ToListAsync();

            var dto = paged.Select(fg =>
            {
                var allPallets = fg.FG_Receive.SelectMany(r => r.FG_ReceivePallets);
                var allItems = allPallets.SelectMany(p => p.FG_ReceivePalletItems);

                return new FinishedGoodDetailsDto
                {
                    Id = fg.Id,
                    SKU = fg.SKU,
                    Description = fg.Description,
                    TotalQty = allItems.Count(),
                    TotalBalanceQty = allItems.Count(i => !i.IsReleased),
                    TotalPallet = allPallets.Count(),
                    TotalBalancePallet = allPallets.Count(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased)),
                    Group3 = fg.Group3,
                    Group4_1 = fg.Group4_1,
                    Group6 = fg.Group6,
                    Group8 = fg.Group8,
                    Group9 = fg.Group9,
                    NDG = fg.NDG,
                    Scentaurus = fg.Scentaurus
                };
            }).ToList();

            _logger.LogInformation("Retrieved {Count} finished goods (start={Start}, length={Length}) from total of {TotalCount}",
                dto.Count, start, length, totalCount);

            return new PaginatedResult<FinishedGoodDetailsDto>
            {
                Items = dto,
                TotalCount = totalCount,
                FilteredCount = totalCount
            };
        }


        public async Task<FinishedGoodDetailsDto> GetFinishedGoodSummaryByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching finished good summary for ID: {FinishedGoodId}", id);

            var entity = await _dbContext.GIV_FinishedGoods
                .Where(fg => fg.Id == id && !fg.IsDeleted)
                .Select(fg => new FinishedGoodDetailsDto
                {
                    Id = fg.Id,
                    SKU = fg.SKU,
                    Description = fg.Description,
                    TotalBalanceQty = fg.FG_Receive
                        .SelectMany(r => r.FG_ReceivePallets)
                        .SelectMany(p => p.FG_ReceivePalletItems)
                        .Count(i => !i.IsReleased),
                    TotalBalancePallet = fg.FG_Receive
                        .SelectMany(r => r.FG_ReceivePallets)
                        .Count(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased))
                })
                .FirstOrDefaultAsync();

            if (entity == null)
            {
                _logger.LogWarning("No finished good found with ID: {FinishedGoodId}", id);
            }
            else
            {
                _logger.LogInformation("Finished good summary retrieved: SKU={SKU}, Qty={Qty}, Pallets={Pallets}",
                    entity.SKU, entity.TotalBalanceQty, entity.TotalBalancePallet);
            }

            return entity!;
        }

        public async Task<PaginatedResult<FG_ReceiveSummaryDto>> GetPaginatedReceivesByFinishedGoodIdAsync(
    Guid finishedGoodId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            _logger.LogDebug("Getting paginated receives for FinishedGoodId={FinishedGoodId}, start={Start}, length={Length}, search='{Search}', sortColumn={SortColumn}, sortAscending={SortAscending}",
                finishedGoodId, start, length, searchTerm, sortColumn, sortAscending);

            var query = _dbContext.GIV_FG_Receives
                .Where(r => r.FinishedGoodId == finishedGoodId && !r.IsDeleted)
                .Include(r => r.FG_ReceivePallets)
                    .ThenInclude(p => p.FG_ReceivePalletItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(r => r.Remarks.Contains(searchTerm));
            }

            var total = await query.CountAsync();

            var orderedQuery = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(r => r.ReceivedDate) : query.OrderByDescending(r => r.ReceivedDate),
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var receives = await orderedQuery.Skip(start).Take(length).ToListAsync();

            var result = receives.Select(r =>
            {
                var allPallets = r.FG_ReceivePallets ?? new List<GIV_FG_ReceivePallet>();
                var allItems = allPallets.SelectMany(p => p.FG_ReceivePalletItems ?? new List<GIV_FG_ReceivePalletItem>());

                return new FG_ReceiveSummaryDto
                {
                    Id = r.Id,
                    ReceivedDate = r.ReceivedDate,
                    PackSize = allPallets.Sum(p => p.PackSize),
                    TotalQuantity = allItems.Count(),
                    TotalPallet = allPallets.Count(),
                    BalanceQuantity = allItems.Count(i => !i.IsReleased),
                    BalancePallet = allPallets.Count(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased)),
                    FinishedGoodId = r.FinishedGoodId
                };
            }).ToList();

            _logger.LogInformation("Retrieved {Count} receive records for FG {FinishedGoodId}", result.Count, finishedGoodId);

            return new PaginatedResult<FG_ReceiveSummaryDto>
            {
                Items = result,
                TotalCount = total,
                FilteredCount = total
            };
        }


        public async Task<PaginatedResult<FinishedGoodItemDto>> GetPaginatedItemsByReceive(Guid receiveId, int start, int length)
        {
            _logger.LogInformation("Getting paginated items for receive ID: {ReceiveId}, start: {Start}, length: {Length}", receiveId, start, length);

            var query = _dbContext.GIV_FG_ReceivePalletItems
                .Include(i => i.GIV_FG_ReceivePallet)
                    .ThenInclude(p => p.Location)
                .Where(i => i.GIV_FG_ReceivePallet.ReceiveId == receiveId );

            var total = await query.CountAsync();

            var items = await query
                .Skip(start)
                .Take(length)
                .Select(i => new FinishedGoodItemDto
                {
                    HU = i.ItemCode,
                    BatchNo = i.BatchNo ?? "N/A",
                    MHU = i.GIV_FG_ReceivePallet.PalletCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks,
                    IsReleased = i.IsReleased,
                    Location = i.GIV_FG_ReceivePallet.Location != null
                        ? i.GIV_FG_ReceivePallet.Location.Barcode
                        : null
                }).ToListAsync();

            _logger.LogInformation("Retrieved {Count} paginated items (total={Total}) for receive ID: {ReceiveId}", items.Count, total, receiveId);

            return new PaginatedResult<FinishedGoodItemDto>
            {
                Items = items,
                TotalCount = total,
                FilteredCount = total
            };
        }


        public async Task<PaginatedResult<FinishedGoodGroupedItemDto>> GetPaginatedGroupedItemsByReceive(Guid receiveId, int start, int length)
        {
            _logger.LogInformation("Getting paginated grouped items for receive ID: {ReceiveId}, start: {Start}, length: {Length}", receiveId, start, length);

            var items = await GetItemsByReceive(receiveId);

            var grouped = items
                .GroupBy(i => i.BatchNo)
                .Select(g => new FinishedGoodGroupedItemDto
                {
                    BatchNo = g.Key,
                    HUList = g.Select(x => x.HU).Distinct().ToList(),
                    MHUList = g.Select(x => x.MHU).Distinct().ToList(),
                    Qty = g.Count(),
                    BalQty = g.Count(x => !x.IsReleased),
                    ProdDate = g.First().ProdDate,
                    DG = g.Select(x => x.DG).ToList(),
                    Remarks = g.Select(x => x.Remarks).ToList(),
                    Location = g.Select(x => x.Location).ToList()
                }).ToList();

            var paged = grouped.Skip(start).Take(length).ToList();

            _logger.LogInformation("Grouped and paginated {PageCount} out of {TotalCount} groups for receive ID: {ReceiveId}", paged.Count, grouped.Count, receiveId);

            return new PaginatedResult<FinishedGoodGroupedItemDto>
            {
                Items = paged,
                TotalCount = grouped.Count,
                FilteredCount = grouped.Count
            };
        }


        public async Task<FG_ReceivePalletItemEditDto> GetItemById(Guid ItemId)
        {
            _logger.LogInformation("Fetching finished good item by ID: {ItemId}", ItemId);

            var item = await _dbContext.GIV_FG_ReceivePalletItems
                .Include(i => i.GIV_FG_ReceivePallet)
                .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.Id == ItemId && !i.IsDeleted);

            if (item == null)
            {
                _logger.LogWarning("Finished good item not found: {ItemId}", ItemId);
                return null;
            }

            _logger.LogInformation("Finished good item found: {ItemCode}", item.ItemCode);

            return new FG_ReceivePalletItemEditDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                BatchNo = item.BatchNo,
                ProdDate = item.ProdDate,
                Remarks = item.Remarks,
                IsReleased = item.IsReleased
            };
        }

        public async Task<ApiResponseDto<string>> UpdateItemAsync(FG_ReceivePalletItemEditDto dto)
        {
            _logger.LogInformation("Updating finished good item: {ItemId}", dto.Id);

            try
            {
                var item = await _dbContext.GIV_FG_ReceivePalletItems
                    .FirstOrDefaultAsync(i => i.Id == dto.Id && !i.IsDeleted);

                if (item == null)
                {
                    _logger.LogWarning("Finished good item not found: {ItemId}", dto.Id);
                    return ApiResponseDto<string>.ErrorResult("Item not found.");
                }

                item.ItemCode = dto.ItemCode;
                item.BatchNo = dto.BatchNo;
                item.ProdDate = dto.ProdDate == null ? null : DateTime.SpecifyKind(dto.ProdDate.Value, DateTimeKind.Utc);
                item.Remarks = dto.Remarks;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Finished good item updated: {ItemId}", dto.Id);
                return ApiResponseDto<string>.SuccessResult("OK", "Item updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update finished good item: {ItemId}", dto.Id);
                return ApiResponseDto<string>.ErrorResult("Failed to update item.", new List<string> { ex.Message });
            }
        }

        public async Task<FinishedGoodEditDto?> GetEditFinishedGoodDtoAsync(Guid id)
        {
            _logger.LogInformation("Fetching finished good for editing: {Id}", id);

            var entity = await _dbContext.GIV_FinishedGoods
                .AsNoTracking()
                .FirstOrDefaultAsync(fg => fg.Id == id && !fg.IsDeleted);

            return entity == null ? null : new FinishedGoodEditDto
            {
                Id = entity.Id,
                SKU = entity.SKU,
                Description = entity.Description
            };
        }

        public async Task<ApiResponseDto<string>> UpdateFinishedGoodAsync(FinishedGoodEditDto dto, string username)
        {
            _logger.LogInformation("Updating finished good: {Id} by user {User}", dto.Id, username);

            var entity = await _dbContext.GIV_FinishedGoods
                .FirstOrDefaultAsync(fg => fg.Id == dto.Id && !fg.IsDeleted);

            if (entity == null)
                return ApiResponseDto<string>.ErrorResult("Finished Good not found.");

            entity.SKU = dto.SKU;
            entity.Description = dto.Description;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = username;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Finished good updated: {Id}", dto.Id);
            return ApiResponseDto<string>.SuccessResult("Finished Good updated successfully.");
        }

        public async Task<FG_ReceiveEditDto?> GetEditReceiveDtoAsync(Guid id)
        {
            _logger.LogInformation("Fetching FG receive for editing: {Id}", id);

            var entity = await _dbContext.GIV_FG_Receives
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            return entity == null ? null : new FG_ReceiveEditDto
            {
                Id = entity.Id,
                BatchNo = entity.BatchNo ?? "",
                ReceivedDate = DateTime.SpecifyKind(entity.ReceivedDate, DateTimeKind.Utc),
                ReceivedBy = entity.ReceivedBy ?? "",
                Remarks = entity.Remarks
            };
        }

        public async Task<ApiResponseDto<string>> UpdateReceiveAsync(FG_ReceiveEditDto dto, string username)
        {
            _logger.LogInformation("Updating FG receive: {Id} by {Username}", dto.Id, username);

            var entity = await _dbContext.GIV_FG_Receives
                .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted);

            if (entity == null)
                return ApiResponseDto<string>.ErrorResult("Receive not found.");

            entity.BatchNo = dto.BatchNo;
            entity.ReceivedDate = DateTime.SpecifyKind(dto.ReceivedDate, DateTimeKind.Utc);
            entity.ReceivedBy = dto.ReceivedBy;
            entity.Remarks = dto.Remarks;
            entity.ModifiedAt = DateTime.UtcNow;
            entity.ModifiedBy = username;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("FG receive updated: {Id}", dto.Id);
            return ApiResponseDto<string>.SuccessResult("Receive updated successfully.");
        }

        public async Task<(byte[] fileContent, string fileName)> GenerateFinishedGoodExcelAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating monthly FG Excel report from {Start} to {End}", startDate, endDate);
            return await GenerateExcelInternal(startDate, endDate, "monthly");
        }

        public async Task<(byte[] fileContent, string fileName)> GenerateFinishedGoodWeeklyExcelAsync(DateTime cutoffDate)
        {
            var startDate = cutoffDate.AddDays(-6);
            var endDate = cutoffDate;
            _logger.LogInformation("Generating weekly FG Excel report from {Start} to {End}", startDate, endDate);
            return await GenerateExcelInternal(startDate, endDate, "weekly");
        }

        // Logging added to Excel generation

        private async Task<(byte[] fileContent, string fileName)> GenerateExcelInternal(DateTime start, DateTime end, string mode)
        {
            _logger.LogInformation("Generating FG Excel from {Start} to {End} in {Mode} mode", start, end, mode);

            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

            var receives = await _dbContext.GIV_FG_Receives
                .Include(r => r.FinishedGood)
                .Include(r => r.FG_ReceivePallets)
                    .ThenInclude(p => p.FG_ReceivePalletItems)
                .Include(r => r.FG_ReceivePallets)
                    .ThenInclude(p => p.Location)
                .Where(r => r.ReceivedDate >= start && r.ReceivedDate <= end && !r.IsDeleted)
                .ToListAsync();

            _logger.LogInformation("Found {ReceiveCount} FG receives for Excel export", receives.Count);

            var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("FinishedGoodReport");

            // Header row
            ws.Cells[1, 1].Value = "Loc";
            ws.Cells[1, 2].Value = "Receipt date";
            ws.Cells[1, 3].Value = "SKU";
            ws.Cells[1, 4].Value = "Description";
            ws.Cells[1, 5].Value = "Batch";
            ws.Cells[1, 6].Value = "QTY";
            ws.Cells[1, 7].Value = "Pack Size";
            ws.Cells[1, 8].Value = "NoItems";
            ws.Cells[1, 9].Value = "NO OF PLT";
            ws.Cells[1, 10].Value = "Plant";

            using (var range = ws.Cells[1, 1, 1, 11])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.Yellow);
            }

            int row = 2;

            foreach (var receive in receives)
            {
                var palletGroupCounts = receive.FG_ReceivePallets
                    .Where(p => p.Location != null)
                    .GroupBy(p => new { receive.BatchNo, LocationCode = p.Location!.Barcode })
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var pallet in receive.FG_ReceivePallets)
                {
                    var items = pallet.FG_ReceivePalletItems;
                    var locationCode = pallet.Location?.Barcode;
                    var noItems = items.Count;
                    var qty = pallet.PackSize * noItems;
                    var batchKey = new { receive.BatchNo, LocationCode = locationCode };

                    ws.Cells[row, 1].Value = locationCode;
                    ws.Cells[row, 2].Value = receive.ReceivedDate.ToString("dd-MMM-yy");
                    ws.Cells[row, 3].Value = receive.FinishedGood.SKU;
                    ws.Cells[row, 4].Value = receive.FinishedGood.Description;
                    ws.Cells[row, 5].Value = receive.BatchNo;
                    ws.Cells[row, 6].Value = qty;
                    ws.Cells[row, 7].Value = pallet.PackSize;
                    ws.Cells[row, 8].Value = noItems;
                    ws.Cells[row, 9].Value = palletGroupCounts.ContainsKey(batchKey) ? palletGroupCounts[batchKey] : 1;
                    ws.Cells[row, 10].Value = "BXHU";

                    row++;
                }
            }

            // Summarize total pallets by Plant
            var plantPalletSummary = receives
                .SelectMany(r => r.FG_ReceivePallets.Select(p => new
                {
                    Plant = "BXHU", // Replace with actual logic if dynamic
                    Count = 1
                }))
                .GroupBy(x => x.Plant)
                .Select(g => new { Plant = g.Key, TotalPLT = g.Sum(x => x.Count) })
                .ToList();

            int summaryStartCol = 13;
            int summaryStartRow = 1;

            // Header
            ws.Cells[summaryStartRow, summaryStartCol].Value = "Plant";
            ws.Cells[summaryStartRow, summaryStartCol + 1].Value = "Total PLT";
            using (var range = ws.Cells[summaryStartRow, summaryStartCol, summaryStartRow, summaryStartCol + 1])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }

            int summaryRow = summaryStartRow + 1;
            int grandTotal = 0;

            foreach (var entry in plantPalletSummary)
            {
                ws.Cells[summaryRow, summaryStartCol].Value = entry.Plant;
                ws.Cells[summaryRow, summaryStartCol + 1].Value = entry.TotalPLT;
                grandTotal += entry.TotalPLT;
                summaryRow++;
            }

            // Grand total row
            ws.Cells[summaryRow, summaryStartCol].Value = "Total";
            ws.Cells[summaryRow, summaryStartCol + 1].Value = grandTotal;
            using (var totalRange = ws.Cells[summaryRow, summaryStartCol, summaryRow, summaryStartCol + 1])
            {
                totalRange.Style.Font.Bold = true;
                totalRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                totalRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
            }

            ws.Cells.AutoFitColumns();

            var stream = new MemoryStream();
            package.SaveAs(stream);
            var filename = $"FinishedGoodReport_{mode}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            _logger.LogInformation("Excel report generated: {Filename}, Rows: {RowCount}", filename, row - 2);
            return (stream.ToArray(), filename);
        }

        public async Task<ApiResponseDto<Guid>> CreateFinishedGoodAsync(FinishedGoodCreateWebDto dto)
        {
            _logger.LogInformation("Creating finished good with SKU: {SKU}", dto.SKU);

            try
            {
                var now = DateTime.UtcNow.ToLocalTime();
                var exists = await _dbContext.GIV_FinishedGoods
                    .AnyAsync(x => x.SKU == dto.SKU && !x.IsDeleted && x.WarehouseId == _currentUserService.CurrentWarehouseId);

                if (exists)
                {
                    _logger.LogWarning("Duplicate SKU detected: {SKU}", dto.SKU);
                    return ApiResponseDto<Guid>.ErrorResult("Finished Good with the same SKU already exists.");
                }

                var entity = new GIV_FinishedGood
                {
                    Id = Guid.NewGuid(),
                    SKU = dto.SKU,
                    Description = dto.Description,
                    CreatedAt = now,
                    CreatedBy = _currentUserService.GetCurrentUsername,
                    WarehouseId = _currentUserService.CurrentWarehouseId,
                    IsDeleted = false
                };

                _dbContext.GIV_FinishedGoods.Add(entity);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Finished good created: {Id}", entity.Id);
                return ApiResponseDto<Guid>.SuccessResult(entity.Id, "Finished Good created successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating finished good for SKU: {SKU}", dto.SKU);
                return ApiResponseDto<Guid>.ErrorResult("An error occurred while creating the Finished Good.");
            }
        }
        public async Task<PaginatedResult<FG_ReceivePalletDetailsDto>> GetPaginatedPalletsByReceiveIdAsync(Guid receiveId, int start, int length)
        {
            _logger.LogInformation("Fetching pallets for ReceiveId: {ReceiveId} with Start={Start}, Length={Length}", receiveId, start, length);

            var query = _dbContext.GIV_FG_ReceivePallets
                .Where(p => p.ReceiveId == receiveId && !p.IsDeleted)
                .Include(p => p.FG_ReceivePalletItems)
                .Include(p => p.Location)
                .AsQueryable();

            var totalCount = await query.CountAsync();
            var pallets = await query.OrderByDescending(p => p.CreatedAt).Skip(start).Take(length).ToListAsync();

            _logger.LogInformation("Fetched {Count} pallets from total {TotalCount}", pallets.Count, totalCount);

            var dto = pallets.Select(p => new FG_ReceivePalletDetailsDto
            {
                Id = p.Id,
                PalletCode = p.PalletCode,
                HandledBy = p.HandledBy,
                StoredBy = p.StoredBy,
                PackSize = p.PackSize,
                LocationName = p.Location?.Barcode,
                FG_ReceivePalletItems = p.FG_ReceivePalletItems.Select(i => new FG_ReceivePalletItemDetailsDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks,
                    IsReleased = i.IsReleased
                }).ToList(),
                Quantity = p.FG_ReceivePalletItems.Count,
                QuantityBalance = p.FG_ReceivePalletItems.Count(i => !i.IsReleased)
            }).ToList();

            return new PaginatedResult<FG_ReceivePalletDetailsDto>
            {
                Items = dto,
                TotalCount = totalCount,
                FilteredCount = totalCount
            };
        }
        public async Task<(FG_ReceivePalletDetailsDto Pallet, List<LocationDto> Locations)> GetReceivePalletForEditAsync(Guid palletId)
        {
            _logger.LogInformation("Fetching pallet for edit: {PalletId}", palletId);

            var entity = await _dbContext.GIV_FG_ReceivePallets
                .AsNoTracking()
                .Include(p => p.Receive)
                .Include(p => p.Location)
                .Include(p => p.FG_ReceivePalletItems)
                .Include(p => p.FG_ReceivePalletPhotos)
                .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            var palletDto = _mapper.Map<FG_ReceivePalletDetailsDto>(entity);

            var warehouseId = entity?.WarehouseId ?? Guid.Empty;
            var locationList = await _dbContext.Locations
                .Where(l => l.WarehouseId == warehouseId && !l.IsDeleted)
                .OrderBy(l => l.Barcode)
                .Select(l => new LocationDto
                {
                    Id = l.Id,
                    Barcode = l.Barcode!
                }).ToListAsync();

            _logger.LogInformation("Fetched pallet and {Count} location(s) for pallet ID: {PalletId}", locationList.Count, palletId);
            return (palletDto, locationList);
        }
        public async Task<ApiResponseDto<string>> UpdatePalletAsync(FG_ReceivePalletEditDto dto)
        {
            _logger.LogInformation("Updating pallet with ID: {PalletId}", dto.Id);

            var pallet = await _dbContext.GIV_FG_ReceivePallets
                .FirstOrDefaultAsync(p => p.Id == dto.Id && !p.IsDeleted);

            if (pallet == null)
            {
                _logger.LogWarning("Pallet not found: {PalletId}", dto.Id);
                return ApiResponseDto<string>.ErrorResult($"Pallet {dto.Id} not found.");
            }

            pallet.PalletCode = dto.PalletCode?.Trim();
            pallet.HandledBy = dto.HandledBy;
            pallet.StoredBy = dto.StoredBy;
            pallet.PackSize = dto.PackSize;
            pallet.IsReleased = dto.IsReleased;
            pallet.ModifiedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Pallet updated successfully: {PalletId}", dto.Id);
            return ApiResponseDto<string>.SuccessResult(dto.Id.ToString(), "Pallet updated successfully.");
        }
        public async Task<ApiResponseDto<string>> UpdatePalletLocationAsync(Guid palletId, Guid? locationId, string username)
        {
            _logger.LogInformation("Updating pallet location for pallet {PalletId} to location {LocationId} by {Username}", palletId, locationId, username);

            var fgPallet = await _dbContext.GIV_FG_ReceivePallets
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            if (fgPallet == null)
            {
                _logger.LogWarning("Pallet not found: {PalletId}", palletId);
                return ApiResponseDto<string>.ErrorResult($"Finished Good Pallet with ID '{palletId}' not found.", new List<string> { "Invalid pallet ID." });
            }

            var oldLocationId = fgPallet.LocationId;

            var newLocation = await _dbContext.Locations.FirstOrDefaultAsync(l => l.Id == locationId && !l.IsDeleted);
            if (newLocation == null)
            {
                _logger.LogWarning("New location not found: {LocationId}", locationId);
                return ApiResponseDto<string>.ErrorResult($"Location with ID '{locationId}' not found.", new List<string> { "Invalid location ID." });
            }

            var isAvailable = await _locationService.CheckLocationAvailabilityByBarcodeAsync(newLocation.Barcode);
            if (!isAvailable)
            {
                _logger.LogWarning("Location '{Code}' is full", newLocation.Code);
                return ApiResponseDto<string>.ErrorResult($"Location '{newLocation.Code}' is full.", new List<string> { "Maximum capacity reached for this location." });
            }

            fgPallet.LocationId = newLocation.Id;
            fgPallet.StoredBy = username;
            fgPallet.ModifiedBy = username;
            fgPallet.ModifiedAt = DateTime.UtcNow;
            newLocation.IsEmpty = false;

            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Pallet {PalletId} location updated to {LocationId}", palletId, locationId);

            var isNowFull = !await _locationService.CheckLocationAvailabilityByBarcodeAsync(newLocation.Barcode);
            if (isNowFull)
            {
                await _locationService.MarkLocationAsOccupiedAsync(newLocation.Id);
                _logger.LogInformation("Marked location {LocationId} as occupied", newLocation.Id);
            }

            if (oldLocationId.HasValue && oldLocationId.Value != newLocation.Id)
            {
                await _locationService.MarkLocationAsEmptyAsync(oldLocationId.Value);
                _logger.LogInformation("Marked old location {OldLocationId} as empty", oldLocationId);
            }

            return ApiResponseDto<string>.SuccessResult("Pallet location updated successfully.", "Success");
        }
        public async Task<bool> UpdatePalletGroupFieldAsync(Guid palletId, string fieldName, bool value, string userName)
        {
            _logger.LogInformation("Updating group field {FieldName} to {Value} for pallet {PalletId}", fieldName, value, palletId);

            try
            {
                var pallet = await _dbContext.GIV_FG_ReceivePallets
                    .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

                if (pallet == null)
                {
                    _logger.LogWarning("Pallet not found: {PalletId}", palletId);
                    return false;
                }

                // Update the appropriate field based on fieldName
                //switch (fieldName.ToLower())
                //{
                //    case "group3":
                //        pallet.Group3 = value;
                //        break;
                //    case "group6":
                //        pallet.Group6 = value;
                //        break;
                //    case "group8":
                //        pallet.Group8 = value;
                //        break;
                //    case "group9":
                //        pallet.Group9 = value;
                //        break;
                //    case "ndg":
                //        pallet.NDG = value;
                //        break;
                //    case "scentaurus":
                //        pallet.Scentaurus = value;
                //        break;
                //    default:
                //        _logger.LogWarning("Invalid field name: {FieldName}", fieldName);
                //        return false;
                //}

                pallet.ModifiedAt = DateTime.Now;
                pallet.ModifiedBy = userName;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully updated group field {FieldName} for pallet {PalletId}", fieldName, palletId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group field {FieldName} for pallet {PalletId}", fieldName, palletId);
                return false;
            }
        }
        public async Task<FinishedGoodGrandTotalsDto> GetFinishedGoodsGrandTotalsAsync()
        {
            _logger.LogInformation("Calculating finished goods grand totals");

            try
            {
                // Get all non-deleted finished goods with their receives, pallets, and items
                var query = _dbContext.GIV_FinishedGoods
                    .Where(fg => !fg.IsDeleted)
                    .SelectMany(fg => fg.FG_Receive)
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.FG_ReceivePallets)
                    .Where(p => !p.IsDeleted);

                // Calculate total balance pallets (pallets that are not released)
                var totalBalancePallet = await query
                    .Where(p => !p.IsReleased)
                    .CountAsync();

                // Calculate total balance qty (items from pallets that are not released)
                var totalBalanceQty = await query
                    .Where(p => !p.IsReleased)
                    .SelectMany(p => p.FG_ReceivePalletItems)
                    .Where(i => !i.IsDeleted)
                    .CountAsync();

                _logger.LogInformation("Grand totals calculated - Balance Qty: {TotalBalanceQty}, Balance Pallets: {TotalBalancePallet}",
                    totalBalanceQty, totalBalancePallet);

                return new FinishedGoodGrandTotalsDto
                {
                    TotalBalanceQty = totalBalanceQty,
                    TotalBalancePallet = totalBalancePallet
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating finished goods grand totals");
                throw;
            }
        }
        public async Task<ApiResponseDto<string>> UpdateFinishedGoodGroupFieldAsync(Guid finishedGoodId, string fieldName, bool value, string userName)
        {
            _logger.LogInformation("Updating group field {FieldName} to {Value} for finished good {FinishedGoodId}", fieldName, value, finishedGoodId);

            try
            {
                var finishedGood = await _dbContext.GIV_FinishedGoods
                    .FirstOrDefaultAsync(fg => fg.Id == finishedGoodId && !fg.IsDeleted);

                if (finishedGood == null)
                {
                    _logger.LogWarning("Finished good not found: {FinishedGoodId}", finishedGoodId);
                    return ApiResponseDto<string>.ErrorResult("Finished good not found.");
                }

                // Update the appropriate field based on fieldName
                switch (fieldName.ToLower())
                {
                    case "group3":
                        finishedGood.Group3 = value;
                        break;
                    case "group4_1":
                        finishedGood.Group4_1 = value;
                        break;
                    case "group6":
                        finishedGood.Group6 = value;
                        break;
                    case "group8":
                        finishedGood.Group8 = value;
                        break;
                    case "group9":
                        finishedGood.Group9 = value;
                        break;
                    case "ndg":
                        finishedGood.NDG = value;
                        break;
                    case "scentaurus":
                        finishedGood.Scentaurus = value;
                        break;
                    default:
                        _logger.LogWarning("Invalid field name: {FieldName}", fieldName);
                        return ApiResponseDto<string>.ErrorResult($"Invalid field name: {fieldName}");
                }

                finishedGood.ModifiedAt = DateTime.UtcNow;
                finishedGood.ModifiedBy = userName;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully updated group field {FieldName} for finished good {FinishedGoodId}", fieldName, finishedGoodId);
                return ApiResponseDto<string>.SuccessResult("Group field updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group field {FieldName} for finished good {FinishedGoodId}", fieldName, finishedGoodId);
                return ApiResponseDto<string>.ErrorResult("An error occurred while updating the group field.", new List<string> { ex.Message });
            }
        }
        public async Task<PaginatedResult<FG_ReleaseTableRowDto>> GetPaginatedReleasesByFinishedGoodIdAsync(
    Guid finishedGoodId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            var query = _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePallet)
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePalletItem)
                .Where(r => r.GIV_FinishedGoodId == finishedGoodId && !r.IsDeleted);

            var totalCount = await query.CountAsync();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lower = searchTerm.ToLower();
                query = query.Where(r =>
                    r.ReleasedBy.ToLower().Contains(lower) ||
                    (r.Remarks != null && r.Remarks.ToLower().Contains(lower)) ||
                    (r.ActualReleasedBy != null && r.ActualReleasedBy.ToLower().Contains(lower)) ||
                    r.GIV_FG_ReleaseDetails.Any(d =>
                        (d.GIV_FG_ReceivePallet != null && d.GIV_FG_ReceivePallet.PalletCode.ToLower().Contains(lower)) ||
                        (d.GIV_FG_ReceivePalletItem != null && d.GIV_FG_ReceivePalletItem.ItemCode.ToLower().Contains(lower)))
                );
            }

            var filteredCount = await query.CountAsync();

            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate),
                1 => sortAscending ? query.OrderBy(r => r.ReleasedBy) : query.OrderByDescending(r => r.ReleasedBy),
                _ => query.OrderByDescending(r => r.ReleaseDate)
            };

            var items = await query.Skip(start).Take(length).ToListAsync();

            var userIds = items.Select(x => x.ReleasedBy).Distinct().ToList();
            var userMap = await _dbContext.Users
                .Where(u => userIds.Contains(u.Id.ToString()))
                .ToDictionaryAsync(u => u.Id.ToString(), u => u.FullName);

            var result = new List<FG_ReleaseTableRowDto>();

            foreach (var r in items)
            {
                var details = r.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
                var pallets = details.Where(d => d.IsEntirePallet).ToList();
                var itemsOnly = details.Where(d => !d.IsEntirePallet).ToList();
                var released = details.Where(d => d.ActualReleaseDate.HasValue).ToList();

                result.Add(new FG_ReleaseTableRowDto
                {
                    Id = r.Id,
                    ReleaseDate = r.ReleaseDate,
                    ReleasedBy = r.ReleasedBy,
                    ReleasedByFullName = userMap.GetValueOrDefault(r.ReleasedBy),
                    Remarks = r.Remarks,
                    ActualReleaseDate = r.ActualReleaseDate,
                    ActualReleasedBy = r.ActualReleasedBy,

                    TotalPallets = pallets.Count,
                    TotalItems = itemsOnly.Count,
                    EntirePalletCount = pallets.Count,
                    IndividualItemCount = itemsOnly.Count,
                    ReleasedPallets = released.Count(x => x.IsEntirePallet),
                    ReleasedItems = released.Count(x => !x.IsEntirePallet),

                    IsCompleted = r.ActualReleaseDate.HasValue,
                    IsPartiallyReleased = released.Any() && !r.ActualReleaseDate.HasValue,

                    HasEditAccess = true // modify based on permissions if needed
                });
            }

            return new PaginatedResult<FG_ReleaseTableRowDto>
            {
                Items = result,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }
        public async Task<List<GroupedPalletCountDto>> GetGroupedPalletCount(DateTime cutoffDate)
        {
            try
            {
                //get all received pallet  from monday to sunday (sunday is cutoffdate)
                _logger.LogDebug("FinishedGood - GetGroupedPalletCount");

                DateTime startDate = cutoffDate.AddDays(-6).Date;
                var rmPallets = await _dbContext.GIV_FG_ReceivePallets
                        .Where(rp => !rp.IsDeleted && !rp.IsReleased)
                        .Where(rp => !rp.Receive.IsDeleted)
                        .Where(rp => rp.Receive.ReceivedDate >= startDate && rp.Receive.ReceivedDate <= cutoffDate)
                        .Where(rp => !rp.Receive.FinishedGood.IsDeleted)
                            .Select(rp => new
                            {
                                NDG = rp.Receive.FinishedGood.NDG,
                                Group9 = rp.Receive.FinishedGood.Group9,
                                Group3 = rp.Receive.FinishedGood.Group3,
                                Group6 = rp.Receive.FinishedGood.Group6,
                                Group8 = rp.Receive.FinishedGood.Group8,
                                Group4_1 = rp.Receive.FinishedGood.Group4_1,
                                Scentaurus = rp.Receive.FinishedGood.Scentaurus,
                                Id = rp.Id
                            }).ToListAsync();
                /*
                 grouped to:
                    1.) NDG and Group9
                    2.) Group3, 4.1, 6, 8
                    3.) Scentaurus
                each pallet can only belong to one group
                 */
                var dtos = new List<GroupedPalletCountDto>
                {
                    new GroupedPalletCountDto{ group="NDG/9", palletCount=0},
                    new GroupedPalletCountDto{ group="3/4/6/8", palletCount=0},
                    new GroupedPalletCountDto{ group="Scentaurus", palletCount=0}
                };

                foreach (var rmPallet in rmPallets)
                {
                    if (rmPallet.NDG || rmPallet.Group9)
                    {
                        dtos.First(x => x.group == "NDG/9").palletCount++;
                        continue;
                    }
                    if (rmPallet.Group3 || rmPallet.Group4_1 || rmPallet.Group6 || rmPallet.Group8)
                    {
                        dtos.First(x => x.group == "3/4/6/8").palletCount++;
                        continue;
                    }
                    if (rmPallet.Scentaurus)
                    {
                        dtos.First(x => x.group == "Scentaurus").palletCount++;
                        continue;
                    }
                }
                return dtos;
            }
            catch (Exception)
            {
                _logger.LogError("Error FinishedGood - GetGroupedPalletCount");
                throw;
            }

        }

        public async Task<FG_ReleaseDetailsViewDto?> GetReleaseDetailsByIdAsync(Guid releaseId)
        {
            var release = await _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FinishedGood)
                .Include(r => r.GIV_FG_ReleaseDetails)
                .FirstOrDefaultAsync(r => r.Id == releaseId && !r.IsDeleted);

            if (release == null) return null;

            var details = release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
            var entire = details.Where(d => d.IsEntirePallet).ToList();
            var individual = details.Where(d => !d.IsEntirePallet).ToList();
            var released = details.Where(d => d.ActualReleaseDate.HasValue).ToList();

            return new FG_ReleaseDetailsViewDto
            {
                Id = release.Id,
                ReleaseDate = release.ReleaseDate,
                ReleasedBy = release.ReleasedBy,
                ReleasedByFullName = await _dbContext.Users
                    .Where(u => u.Id.ToString() == release.ReleasedBy)
                    .Select(u => u.FullName)
                    .FirstOrDefaultAsync(),
                Remarks = release.Remarks,
                ActualReleaseDate = release.ActualReleaseDate,
                ActualReleasedBy = release.ActualReleasedBy,

                TotalPallets = entire.Count,
                TotalItems = individual.Count,
                EntirePalletCount = entire.Count,
                IndividualItemCount = individual.Count,
                ReleasedPallets = released.Count(x => x.IsEntirePallet),
                ReleasedItems = released.Count(x => !x.IsEntirePallet),

                IsCompleted = release.ActualReleaseDate.HasValue,
                StatusText = release.ActualReleaseDate.HasValue ? "Completed" :
                             released.Any() ? "Partially Released" :
                             release.ReleaseDate.Date <= DateTime.UtcNow.Date ? "Due for Release" : "Scheduled",

                FinishedGoodId = release.GIV_FinishedGoodId, // can rename to FinishedGoodId in your DTO
                SKU = release.GIV_FinishedGood.SKU ?? "-",
                Description = release.GIV_FinishedGood.Description ?? "-"
            };
        }


        public async Task<PaginatedResult<FG_ReleaseDetailsDto>> GetPaginatedReleaseDetailsAsync(
    Guid releaseId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            var query = _dbContext.GIV_FG_ReleaseDetails
                .AsNoTracking()
                .Include(d => d.GIV_FG_Receive)
                .Include(d => d.GIV_FG_ReceivePallet).ThenInclude(p => p.Location)
                .Include(d => d.GIV_FG_ReceivePalletItem)
                .Where(d => d.GIV_FG_ReleaseId == releaseId && !d.IsDeleted);

            var totalCount = await query.CountAsync();
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var l = searchTerm.ToLower();
                query = query.Where(d =>
                    d.GIV_FG_ReceivePallet.PalletCode.ToLower().Contains(l) ||
                    (d.GIV_FG_ReceivePalletItem != null && d.GIV_FG_ReceivePalletItem.ItemCode.ToLower().Contains(l)) ||
                    d.GIV_FG_Receive.BatchNo.ToLower().Contains(l));
            }

            var filteredCount = await query.CountAsync();

            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(d => d.IsEntirePallet) : query.OrderByDescending(d => d.IsEntirePallet),
                1 => sortAscending ? query.OrderBy(d => d.GIV_FG_ReceivePallet.PalletCode) : query.OrderByDescending(d => d.GIV_FG_ReceivePallet.PalletCode),
                2 => sortAscending ? query.OrderBy(d => d.GIV_FG_ReceivePalletItem.ItemCode) : query.OrderByDescending(d => d.GIV_FG_ReceivePalletItem.ItemCode),
                3 => sortAscending ? query.OrderBy(d => d.GIV_FG_Receive.BatchNo) : query.OrderByDescending(d => d.GIV_FG_Receive.BatchNo),
                4 => sortAscending ? query.OrderBy(d => d.ActualReleaseDate ?? DateTime.MaxValue) : query.OrderByDescending(d => d.ActualReleaseDate ?? DateTime.MinValue),
                _ => query.OrderByDescending(d => d.Id)
            };

            var page = await query.Skip(start).Take(length).ToListAsync();

            var entirePalletIds = page
                .Where(d => d.IsEntirePallet)         
                .Select(d => d.GIV_FG_ReceivePalletId)
                .Distinct()
                .ToList();

            var palletItemCodes = entirePalletIds.Any()
                ? await _dbContext.GIV_FG_ReceivePalletItems
                    .Where(i => entirePalletIds.Contains(i.GIV_FG_ReceivePalletId) && !i.IsDeleted)
                    .GroupBy(i => i.GIV_FG_ReceivePalletId)
                    .ToDictionaryAsync(g => g.Key, g => g.Select(i => i.ItemCode).ToList())
                : new Dictionary<Guid, List<string>>();

            var dtoList = page.Select(d =>
            {
                palletItemCodes.TryGetValue(d.GIV_FG_ReceivePalletId, out var codes);
                codes ??= new List<string>();              

                return new FG_ReleaseDetailsDto
                {
                    Id = d.Id,
                    ReleaseId = d.GIV_FG_ReleaseId,
                    ReceiveId = d.GIV_FG_ReceiveId,
                    PalletId = d.GIV_FG_ReceivePalletId,
                    ItemId = d.IsEntirePallet ? null : d.GIV_FG_ReceivePalletItemId,
                    IsEntirePallet = d.IsEntirePallet,
                    ActualReleaseDate = d.ActualReleaseDate,
                    ActualReleasedBy = d.ActualReleasedBy,

                    PalletCode = d.GIV_FG_ReceivePallet?.PalletCode ?? "-",
                    ItemCode = d.GIV_FG_ReceivePalletItem?.ItemCode,
                    AllItemCodes = d.IsEntirePallet ? codes : new List<string>(),

                    BatchNo = d.GIV_FG_Receive?.BatchNo ?? "-",
                    LocationCode = d.GIV_FG_ReceivePallet?.Location?.Barcode,
                    ReceivedDate = d.GIV_FG_Receive?.ReceivedDate ?? DateTime.MinValue,
                    PackSize = d.GIV_FG_ReceivePallet?.PackSize ?? 0
                };
            }).ToList();

            return new PaginatedResult<FG_ReleaseDetailsDto>
            {
                Items = dtoList,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        #region JobRelease - Read Operations

        public async Task<PaginatedResult<JobReleaseTableRowDto>> GetPaginatedJobReleasesAsync(
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            _logger.LogInformation("Fetching paginated job releases (start={Start}, length={Length}, search={SearchTerm})",
                start, length, searchTerm);

            // Build the base query that groups releases by JobId
            var jobQuery = _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FinishedGood)
                .Include(r => r.GIV_FG_ReleaseDetails) 
                .Where(r => r.JobId.HasValue && !r.IsDeleted)
                .GroupBy(r => r.JobId)
                .Select(g => new
                {
                    JobId = g.Key,
                    PlannedReleaseDate = g.Min(r => r.ReleaseDate),
                    CreatedBy = g.First().CreatedBy,
                    FinishedGoodCount = g.Select(r => r.GIV_FinishedGoodId).Distinct().Count(),
                    AllReleases = g.ToList(),
                    TotalReleases = g.Count(),
                    CompletedReleases = g.Count(r => r.ActualReleaseDate.HasValue)
                });

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                jobQuery = jobQuery.Where(j =>
                    j.CreatedBy.Contains(searchTerm) ||
                    j.AllReleases.Any(r => r.GIV_FinishedGood.SKU.Contains(searchTerm)));
            }

            // Get total counts
            var totalCount = await jobQuery.CountAsync();
            var filteredCount = totalCount;

            // Apply sorting
            var sortedQuery = sortColumn switch
            {
                0 => sortAscending ? jobQuery.OrderBy(j => j.JobId) : jobQuery.OrderByDescending(j => j.JobId),
                1 => sortAscending ? jobQuery.OrderBy(j => j.PlannedReleaseDate) : jobQuery.OrderByDescending(j => j.PlannedReleaseDate),
                2 => sortAscending ? jobQuery.OrderBy(j => j.CreatedBy) : jobQuery.OrderByDescending(j => j.CreatedBy),
                3 => sortAscending ? jobQuery.OrderBy(j => j.FinishedGoodCount) : jobQuery.OrderByDescending(j => j.FinishedGoodCount),
                _ => jobQuery.OrderByDescending(j => j.PlannedReleaseDate)
            };

            // Get paginated results
            var jobs = await sortedQuery
                .Skip(start)
                .Take(length)
                .ToListAsync();

            // Get user information for the job creators
            var createdByUserIds = jobs.Select(j => j.CreatedBy).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => createdByUserIds.Contains(u.Id.ToString()) && !u.IsDeleted)
                .Select(u => new { UserId = u.Id.ToString(), u.Username, u.FullName })
                .ToListAsync();

            var userLookup = users.ToDictionary(u => u.UserId, u => new { u.Username, u.FullName });

            // Build result DTOs
            var jobReleaseDtos = new List<JobReleaseTableRowDto>();

            foreach (var job in jobs)
            {
                // Calculate aggregated totals for the job
                var totalPallets = 0;
                var totalItems = 0;

                foreach (var release in job.AllReleases)
                {
                    // Now this should work because we included GIV_FG_ReleaseDetails
                    var releaseDetails = release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
                    totalPallets += releaseDetails.Count(d => d.IsEntirePallet);
                    totalItems += releaseDetails.Count(d => !d.IsEntirePallet);
                }

                // Determine job status
                var jobStatus = GetJobStatus(job.AllReleases, job.PlannedReleaseDate);

                // Calculate completion percentage
                var completionPercentage = job.TotalReleases > 0
                    ? (decimal)job.CompletedReleases / job.TotalReleases * 100
                    : 0;

                // Get user info
                var createdByUser = userLookup.TryGetValue(job.CreatedBy, out var user) ? user : null;

                var dto = new JobReleaseTableRowDto
                {
                    JobId = job.JobId!.Value,
                    PlannedReleaseDate = job.PlannedReleaseDate,
                    CreatedBy = createdByUser?.Username ?? job.CreatedBy,
                    CreatedByFullName = createdByUser?.FullName,
                    FinishedGoodCount = job.FinishedGoodCount,
                    TotalPallets = totalPallets,
                    TotalItems = totalItems,
                    JobStatus = jobStatus,
                    CompletionPercentage = Math.Round(completionPercentage, 1)
                };

                jobReleaseDtos.Add(dto);
            }

            _logger.LogInformation("Returning {Count} job releases from total {TotalCount}", jobReleaseDtos.Count, totalCount);

            return new PaginatedResult<JobReleaseTableRowDto>
            {
                Items = jobReleaseDtos,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        public async Task<JobReleaseDetailsDto?> GetJobReleaseDetailsByJobIdAsync(Guid jobId)
        {
            _logger.LogInformation("Fetching job release details for JobId: {JobId}", jobId);

            var releases = await _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FinishedGood)
                .Include(r => r.GIV_FG_ReleaseDetails)
                .Where(r => r.JobId == jobId && !r.IsDeleted)
                .ToListAsync();

            if (!releases.Any())
            {
                _logger.LogWarning("No releases found for JobId: {JobId}", jobId);
                return null;
            }

            // Get user information for creators
            var createdByUserIds = releases.Select(r => r.CreatedBy).Distinct().ToList();
            var users = await _dbContext.Users
                .Where(u => createdByUserIds.Contains(u.Id.ToString()) && !u.IsDeleted)
                .Select(u => new { UserId = u.Id.ToString(), u.Username, u.FullName })
                .ToListAsync();

            var userLookup = users.ToDictionary(u => u.UserId, u => new { u.Username, u.FullName });

            // Calculate aggregated data
            var totalPallets = 0;
            var totalItems = 0;
            var completedReleases = 0;

            foreach (var release in releases)
            {
                var releaseDetails = release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
                totalPallets += releaseDetails.Count(d => d.IsEntirePallet);
                totalItems += releaseDetails.Count(d => !d.IsEntirePallet);

                if (release.ActualReleaseDate.HasValue)
                {
                    completedReleases++;
                }
            }

            // Get job metadata from first release (assuming all releases in job have same creation info)
            var firstRelease = releases.OrderBy(r => r.CreatedAt).First();
            var createdByUser = userLookup.TryGetValue(firstRelease.CreatedBy, out var user) ? user : null;

            // Determine status
            var statusInfo = GetJobStatusWithClass(releases, firstRelease.ReleaseDate);

            // Calculate completion percentage
            var completionPercentage = releases.Count > 0
                ? (decimal)completedReleases / releases.Count * 100
                : 0;

            var dto = new JobReleaseDetailsDto
            {
                JobId = jobId,
                CreatedDate = firstRelease.CreatedAt,
                CreatedBy = createdByUser?.Username ?? firstRelease.CreatedBy,
                CreatedByFullName = createdByUser?.FullName,
                StatusText = statusInfo.Status,
                StatusClass = statusInfo.StatusClass,
                FinishedGoodCount = releases.Select(r => r.GIV_FinishedGoodId).Distinct().Count(),
                TotalPallets = totalPallets,
                TotalItems = totalItems,
                CompletionPercentage = Math.Round(completionPercentage, 1),
                PlannedReleaseDate = firstRelease.ReleaseDate
            };

            _logger.LogInformation("Job release details retrieved for JobId: {JobId}", jobId);
            return dto;
        }

        public async Task<PaginatedResult<JobReleaseIndividualReleaseDto>> GetPaginatedJobReleaseIndividualReleasesAsync(
            Guid jobId,
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending)
        {
            _logger.LogInformation("Fetching individual releases for JobId: {JobId} (start={Start}, length={Length})",
                jobId, start, length);

            // Build base query
            var query = _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FinishedGood)
                .Include(r => r.GIV_FG_ReleaseDetails)
                .Where(r => r.JobId == jobId && !r.IsDeleted);

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    r.GIV_FinishedGood.SKU.Contains(searchTerm) ||
                    (r.GIV_FinishedGood.Description != null && r.GIV_FinishedGood.Description.Contains(searchTerm)));
            }

            // Get counts
            var totalCount = await query.CountAsync();
            var filteredCount = totalCount;

            // Apply sorting
            var sortedQuery = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(r => r.GIV_FinishedGood.SKU) : query.OrderByDescending(r => r.GIV_FinishedGood.SKU),
                1 => sortAscending ? query.OrderBy(r => r.GIV_FinishedGood.Description) : query.OrderByDescending(r => r.GIV_FinishedGood.Description),
                2 => sortAscending ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate),
                _ => query.OrderBy(r => r.GIV_FinishedGood.SKU)
            };

            // Get paginated results
            var releases = await sortedQuery
                .Skip(start)
                .Take(length)
                .ToListAsync();

            // Transform to DTOs
            var individualReleaseDtos = new List<JobReleaseIndividualReleaseDto>();

            foreach (var release in releases)
            {
                var details = release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
                var palletCount = details.Count(d => d.IsEntirePallet);
                var itemCount = details.Count(d => !d.IsEntirePallet);

                var status = release.ActualReleaseDate.HasValue ? "Completed" :
                            (details.Any(d => d.ActualReleaseDate.HasValue) ? "Partially Released" :
                            (release.ReleaseDate.Date <= DateTime.UtcNow.Date ? "Due for Release" : "Scheduled"));

                var dto = new JobReleaseIndividualReleaseDto
                {
                    ReleaseId = release.Id,
                    SKU = release.GIV_FinishedGood.SKU,
                    FinishedGoodDescription = release.GIV_FinishedGood.Description ?? "",
                    ReleaseDate = release.ReleaseDate,
                    Status = status,
                    StatusClass = GetReleaseStatusClass(status),
                    PalletCount = palletCount,
                    ItemCount = itemCount,
                    IsCompleted = release.ActualReleaseDate.HasValue
                };

                individualReleaseDtos.Add(dto);
            }

            return new PaginatedResult<JobReleaseIndividualReleaseDto>
            {
                Items = individualReleaseDtos,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        public async Task<(byte[] fileContent, string fileName)> ExportJobReleaseToExcelAsync(Guid jobId)
        {
            _logger.LogInformation("Exporting job release to Excel for JobId: {JobId}", jobId);

            var releases = await _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FinishedGood)
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePallet)
                        .ThenInclude(p => p.Location)
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePalletItem)
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_Receive)
                .Where(r => r.JobId == jobId && !r.IsDeleted)
                .ToListAsync();

            if (!releases.Any())
            {
                throw new InvalidOperationException($"No releases found for JobId: {jobId}");
            }

            // Prepare export data
            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");
            var exportData = new List<dynamic>();

            foreach (var release in releases)
            {
                var details = release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted).OrderBy(d => d.CreatedAt).ToList();

                foreach (var detail in details)
                {
                    var row = new
                    {
                        ReleaseDate = release.ReleaseDate,
                        SKU = release.GIV_FinishedGood.SKU,
                        MHU = detail.GIV_FG_ReceivePallet?.PalletCode ?? "", // Pallet Code as MHU
                        HU = detail.GIV_FG_ReceivePalletItem?.ItemCode ?? "", // Item Code as HU
                        Batch = detail.GIV_FG_Receive?.BatchNo ?? "",
                        Location = detail.GIV_FG_ReceivePallet?.Location?.Code ?? ""
                    };

                    exportData.Add(row);
                }
            }

            // Generate Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Job Release Details");

            // Set headers
            worksheet.Cells[1, 1].Value = "Release Date";
            worksheet.Cells[1, 2].Value = "SKU";
            worksheet.Cells[1, 3].Value = "MHU";
            worksheet.Cells[1, 4].Value = "HU";
            worksheet.Cells[1, 5].Value = "Batch";
            worksheet.Cells[1, 6].Value = "Location";

            // Style headers
            using (var range = worksheet.Cells[1, 1, 1, 6])
            {
                range.Style.Font.Bold = true;
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
            }

            // Add data
            for (int i = 0; i < exportData.Count; i++)
            {
                var row = exportData[i];
                var rowIndex = i + 2; // Start from row 2 (after header)

                worksheet.Cells[rowIndex, 1].Value = row.ReleaseDate.ToString("yyyy-MM-dd");
                worksheet.Cells[rowIndex, 2].Value = row.SKU;
                worksheet.Cells[rowIndex, 3].Value = row.MHU;
                worksheet.Cells[rowIndex, 4].Value = row.HU;
                worksheet.Cells[rowIndex, 5].Value = row.Batch;
                worksheet.Cells[rowIndex, 6].Value = row.Location;

                // Add borders to data rows
                using (var range = worksheet.Cells[rowIndex, 1, rowIndex, 6])
                {
                    range.Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);
                }
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            // Generate file name
            var fileName = $"FGJobRelease_{jobId.ToString().Substring(0, 8)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Excel export completed for JobId: {JobId}, {RowCount} rows exported", jobId, exportData.Count);

            return (package.GetAsByteArray(), fileName);
        }

        #endregion

        #region JobRelease - Inventory and Available Items

        public async Task<List<FinishedGoodForJobReleaseDto>> GetAvailableFinishedGoodsForJobReleaseAsync()
        {
            _logger.LogInformation("Fetching available finished goods for job release");

            var finishedGoods = await _dbContext.GIV_FinishedGoods
                .AsNoTracking()
                .Include(fg => fg.FG_Receive)
                    .ThenInclude(r => r.FG_ReceivePallets)
                        .ThenInclude(p => p.FG_ReceivePalletItems)
                .Where(fg => !fg.IsDeleted)
                .ToListAsync();

            var finishedGoodDtos = new List<FinishedGoodForJobReleaseDto>();

            foreach (var finishedGood in finishedGoods)
            {
                // Calculate balance quantities
                var allItems = finishedGood.FG_Receive
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.FG_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.FG_ReceivePalletItems)
                    .Where(i => !i.IsDeleted);

                var balanceQty = allItems.Count(i => !i.IsReleased);
                var balancePallets = finishedGood.FG_Receive
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.FG_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .Count(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased));

                var totalReceives = finishedGood.FG_Receive.Count(r => !r.IsDeleted);

                // Only include finished goods that have available inventory
                if (balanceQty > 0)
                {
                    finishedGoodDtos.Add(new FinishedGoodForJobReleaseDto
                    {
                        Id = finishedGood.Id,
                        SKU = finishedGood.SKU,
                        Description = finishedGood.Description,
                        BalanceQty = balanceQty,
                        BalancePallets = balancePallets,
                        TotalReceives = totalReceives
                    });
                }
            }

            _logger.LogInformation("Found {Count} finished goods with available inventory", finishedGoodDtos.Count);
            return finishedGoodDtos.OrderBy(fg => fg.SKU).ToList();
        }

        public async Task<List<JobReleaseInventoryDto>> GetFinishedGoodInventoryForJobReleaseAsync(List<Guid> finishedGoodIds)
        {
            _logger.LogInformation("Fetching inventory for {Count} finished goods", finishedGoodIds.Count);

            var finishedGoods = await _dbContext.GIV_FinishedGoods
                .AsNoTracking()
                .Include(fg => fg.FG_Receive.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.FG_ReceivePallets.Where(p => !p.IsDeleted))
                        .ThenInclude(p => p.FG_ReceivePalletItems.Where(i => !i.IsDeleted))
                .Where(fg => finishedGoodIds.Contains(fg.Id) && !fg.IsDeleted)
                .ToListAsync();

            var inventoryDtos = new List<JobReleaseInventoryDto>();

            foreach (var finishedGood in finishedGoods)
            {
                var finishedGoodDto = new JobReleaseInventoryDto
                {
                    Id = finishedGood.Id,
                    SKU = finishedGood.SKU,
                    Description = finishedGood.Description
                };

                // Calculate totals
                var allItems = finishedGood.FG_Receive
                    .SelectMany(r => r.FG_ReceivePallets)
                    .SelectMany(p => p.FG_ReceivePalletItems);

                finishedGoodDto.TotalBalanceQty = allItems.Count(i => !i.IsReleased);
                finishedGoodDto.TotalBalancePallet = finishedGood.FG_Receive
                    .SelectMany(r => r.FG_ReceivePallets)
                    .Count(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased));

                // Process receives with available inventory
                foreach (var receive in finishedGood.FG_Receive.Where(r => r.FG_ReceivePallets.Any(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased))))
                {
                    var receiveDto = new JobReleaseReceiveDto
                    {
                        Id = receive.Id,
                        BatchNo = receive.BatchNo,
                        ReceivedDate = receive.ReceivedDate,
                        ReceivedBy = receive.ReceivedBy
                    };

                    // Process pallets with available items
                    foreach (var pallet in receive.FG_ReceivePallets.Where(p => p.FG_ReceivePalletItems.Any(i => !i.IsReleased)).OrderBy(p => p.PalletCode))
                    {
                        var palletDto = new JobReleasePalletDto
                        {
                            Id = pallet.Id,
                            PalletCode = pallet.PalletCode,
                            IsReleased = pallet.IsReleased,
                            HandledBy = pallet.HandledBy
                        };

                        // Process items
                        foreach (var item in pallet.FG_ReceivePalletItems.OrderBy(i => i.ItemCode))
                        {
                            palletDto.Items.Add(new JobReleaseItemDto
                            {
                                Id = item.Id,
                                ItemCode = item.ItemCode,
                                IsReleased = item.IsReleased
                            });
                        }

                        receiveDto.Pallets.Add(palletDto);
                    }

                    finishedGoodDto.Receives.Add(receiveDto);
                }

                inventoryDtos.Add(finishedGoodDto);
            }

            _logger.LogInformation("Prepared inventory data for {Count} finished goods", inventoryDtos.Count);
            return inventoryDtos.OrderBy(fg => fg.SKU).ToList();
        }

        #endregion

        #region JobRelease - Conflict Management

        public async Task<FinishedGoodConflictResponse> GetFinishedGoodReleaseConflictsAsync(Guid finishedGoodId)
        {
            _logger.LogInformation("Checking release conflicts for FinishedGoodId: {FinishedGoodId}", finishedGoodId);

            var conflicts = new List<FinishedGoodConflictItem>();

            // Check for existing planned releases for this finished good
            var existingReleases = await _dbContext.GIV_FG_Releases
                .AsNoTracking()
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePallet)
                .Include(r => r.GIV_FG_ReleaseDetails)
                    .ThenInclude(d => d.GIV_FG_ReceivePalletItem)
                .Where(r => r.GIV_FinishedGoodId == finishedGoodId && !r.IsDeleted && !r.ActualReleaseDate.HasValue)
                .ToListAsync();

            foreach (var release in existingReleases)
            {
                foreach (var detail in release.GIV_FG_ReleaseDetails.Where(d => !d.IsDeleted))
                {
                    if (detail.IsEntirePallet && detail.GIV_FG_ReceivePallet != null)
                    {
                        conflicts.Add(new FinishedGoodConflictItem
                        {
                            PalletId = detail.GIV_FG_ReceivePalletId,
                            PalletCode = detail.GIV_FG_ReceivePallet.PalletCode,
                            ExistingReleaseDate = release.ReleaseDate,
                            ExistingJobId = release.JobId ?? release.Id,
                            ConflictType = "Pallet"
                        });
                    }
                    else if (!detail.IsEntirePallet && detail.GIV_FG_ReceivePalletItem != null)
                    {
                        conflicts.Add(new FinishedGoodConflictItem
                        {
                            PalletId = detail.GIV_FG_ReceivePalletId,
                            PalletCode = detail.GIV_FG_ReceivePallet?.PalletCode ?? "",
                            ItemId = detail.GIV_FG_ReceivePalletItemId,
                            ItemCode = detail.GIV_FG_ReceivePalletItem.ItemCode,
                            ExistingReleaseDate = release.ReleaseDate,
                            ExistingJobId = release.JobId ?? release.Id,
                            ConflictType = "Item"
                        });
                    }
                }
            }

            var response = new FinishedGoodConflictResponse
            {
                HasConflicts = conflicts.Any(),
                Conflicts = conflicts,
                Message = conflicts.Any() ? $"Found {conflicts.Count} existing release conflicts" : "No conflicts found"
            };

            _logger.LogInformation("Conflict check completed for FinishedGoodId: {FinishedGoodId}, Conflicts: {ConflictCount}",
                finishedGoodId, conflicts.Count);

            return response;
        }

        public async Task<Dictionary<Guid, FinishedGoodConflictResponse>> GetBatchFinishedGoodReleaseConflictsAsync(List<Guid> finishedGoodIds)
        {
            _logger.LogInformation("Checking release conflicts for {Count} finished goods", finishedGoodIds.Count);

            var result = new Dictionary<Guid, FinishedGoodConflictResponse>();

            foreach (var finishedGoodId in finishedGoodIds)
            {
                var conflicts = await GetFinishedGoodReleaseConflictsAsync(finishedGoodId);
                result[finishedGoodId] = conflicts;
            }

            _logger.LogInformation("Batch conflict check completed for {Count} finished goods", finishedGoodIds.Count);
            return result;
        }

        #endregion

        #region Helper Methods

        private string GetJobStatus(List<GIV_FG_Release> releases, DateTime plannedReleaseDate)
        {
            var completedCount = releases.Count(r => r.ActualReleaseDate.HasValue);
            var totalCount = releases.Count;
            var hasOverdue = releases.Any(r => !r.ActualReleaseDate.HasValue && r.ReleaseDate.Date < DateTime.UtcNow.Date);

            if (completedCount == totalCount)
                return "Completed";

            if (hasOverdue)
                return "Overdue";

            if (completedCount > 0)
                return "In Progress";

            if (plannedReleaseDate.Date <= DateTime.UtcNow.Date)
                return "Due for Release";

            return "Scheduled";
        }

        private (string Status, string StatusClass) GetJobStatusWithClass(List<GIV_FG_Release> releases, DateTime plannedReleaseDate)
        {
            var status = GetJobStatus(releases, plannedReleaseDate);
            var statusClass = status.ToLower() switch
            {
                "completed" => "status-completed",
                "overdue" => "status-overdue",
                "in progress" => "status-in-progress",
                "due for release" => "status-due",
                "scheduled" => "status-scheduled",
                _ => "status-scheduled"
            };

            return (status, statusClass);
        }

        private string GetReleaseStatusClass(string status)
        {
            return status.ToLower() switch
            {
                "completed" => "status-completed",
                "partially released" => "status-in-progress",
                "due for release" => "status-due",
                "scheduled" => "status-scheduled",
                _ => "status-scheduled"
            };
        }

        #endregion

    }
}
