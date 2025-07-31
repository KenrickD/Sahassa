using Amazon.Runtime.Internal;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto;
using WMS.Domain.DTOs.GIV_Invoicing;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RawMaterial.Import;
using WMS.Domain.DTOs.GIV_RawMaterial.Web;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet.Web;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto.Web;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.DTOs.RawMaterial;
using WMS.Domain.DTOs.Users;
using WMS.Domain.DTOs.Warehouses;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using static WMS.Domain.Enumerations.Enumerations;

namespace WMS.Application.Services
{
    public class RawMaterialService : IRawMaterialService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly AwsS3Config _awsS3Config;
        private readonly IAwsS3Service _awsS3Service;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILocationService _locationService;
        private readonly ILogger<RawMaterialService> _logger;
        public RawMaterialService(
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<RawMaterialService> logger,
            IConfiguration configuration,
            ICurrentUserService currentUserService,
            IOptions<AwsS3Config> awsOptions,
            IAwsS3Service s3Service,
            ILocationService locationService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _logger = logger;
            _configuration = configuration;
            _currentUserService = currentUserService;
            _awsS3Config = awsOptions.Value;
            _awsS3Service = s3Service;
            _locationService = locationService;
        }
        #region RawMaterial API
        public async Task<ApiResponseDto<Guid>> CreateRawMaterialAsync(
    RawMaterialCreateDto dto,
    List<RM_ReceivePalletPhotoUploadDto> photos,
    string username,
    Guid warehouseId)
        {
            using var scope = _logger.BeginScope("CreateRawMaterial - MaterialNo: {MaterialNo}, WarehouseId: {WarehouseId}, Username: {Username}", dto.MaterialNo, warehouseId, username);

            try
            {
                var now = DateTime.UtcNow.ToLocalTime();
                _logger.LogInformation("Starting raw material creation process.");

                var warehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouseId && !w.IsDeleted);
                if (warehouse == null)
                {
                    _logger.LogWarning("Warehouse not found: {WarehouseId}", warehouseId);
                    return ApiResponseDto<Guid>.ErrorResult($"Warehouse with ID '{warehouseId}' not found.", new List<string> { "Invalid warehouse ID." });
                }

                _logger.LogInformation("Warehouse found: {WarehouseCode}", warehouse.Code);

                var client = await _dbContext.Clients.FirstOrDefaultAsync(c => c.Id == _currentUserService.CurrentClientId && !c.IsDeleted);
                if (client == null)
                {
                    _logger.LogWarning("Client not found for user: {Username}", _currentUserService.GetCurrentUsername);
                    return ApiResponseDto<Guid>.ErrorResult($"User '{_currentUserService.GetCurrentUsername}' Client ID is not set.", new List<string> { "Invalid Client ID." });
                }

                _logger.LogInformation("Client found: {ClientCode}", client.Code);

                var invalidPhotos = photos.Where(p => string.IsNullOrWhiteSpace(p.PalletCode)).ToList();
                if (invalidPhotos.Any())
                {
                    _logger.LogWarning("Invalid photos found: {Count}", invalidPhotos.Count);
                    return ApiResponseDto<Guid>.ErrorResult("One or more photo entries have empty or missing PalletCode.", invalidPhotos.Select((_, i) => $"Photo index {i} is missing a valid PalletCode.").ToList());
                }

                var packageTypeCodeType = await _dbContext.GeneralCodeTypes.FirstOrDefaultAsync(t => t.Name == AppConsts.GeneralCodeType.PRODUCT_TYPE && !t.IsDeleted);
                if (packageTypeCodeType == null)
                {
                    _logger.LogWarning("GeneralCodeType 'Product Type' not found.");
                    return ApiResponseDto<Guid>.ErrorResult("General code type 'Product Type' not found.", new List<string> { "System setup missing: 'Product Type' code group for package type." });
                }

                var packageTypeLookup = await _dbContext.GeneralCodes
                    .Where(c => c.GeneralCodeTypeId == packageTypeCodeType.Id && !c.IsDeleted)
                    .ToDictionaryAsync(c => c.Name.Trim(), c => c.Id);

                var containerIdsToCheck = dto.Receives
                    .Where(r => (TransportType)r.TypeID == TransportType.Container && r.ContainerId.HasValue)
                    .Select(r => r.ContainerId!.Value)
                    .Distinct()
                    .ToList();

                if (containerIdsToCheck.Any())
                {
                    _logger.LogInformation("Validating {Count} container ID(s).", containerIdsToCheck.Count);
                    var existingContainerIds = await _dbContext.GIV_Containers
                        .Where(c => containerIdsToCheck.Contains(c.Id) && !c.IsDeleted)
                        .Select(c => c.Id)
                        .ToListAsync();

                    var missing = containerIdsToCheck.Except(existingContainerIds).ToList();
                    if (missing.Any())
                    {
                        _logger.LogWarning("Missing container ID(s): {Missing}", string.Join(", ", missing));
                        return ApiResponseDto<Guid>.ErrorResult($"Container(s) not found: {string.Join(", ", missing)}", new List<string> { "Some container IDs are invalid." });
                    }
                }

                var existingRawMaterial = await _dbContext.GIV_RawMaterials.FirstOrDefaultAsync(rm => rm.MaterialNo.ToLower() == dto.MaterialNo.ToLower() && !rm.IsDeleted);

                bool isNew = existingRawMaterial == null;

                GIV_RawMaterial rawMaterial = existingRawMaterial ?? new GIV_RawMaterial
                {
                    Id = Guid.NewGuid(),
                    MaterialNo = dto.MaterialNo,
                    Description = dto.Description,
                    Group3 = dto.Group3 ?? false,
                    Group4_1 = dto.Group4_1 ?? false,
                    Group6 = dto.Group6 ?? false,
                    Group8 = dto.Group8 ?? false,
                    Group9 = dto.Group9 ?? false,
                    NDG = dto.NDG ?? false,
                    Scentaurus = dto.Scentaurus ?? false,
                    CreatedBy = username,
                    CreatedAt = now,
                    IsDeleted = false,
                    WarehouseId = warehouseId
                };

                if (isNew)
                {
                    _logger.LogInformation("Creating new raw material: {MaterialNo}", rawMaterial.MaterialNo);
                    await _dbContext.GIV_RawMaterials.AddAsync(rawMaterial);
                }
                else
                {
                    _logger.LogInformation("Using existing raw material: {MaterialNo}", rawMaterial.MaterialNo);
                }

                var palletCodes = dto.Receives.SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !string.IsNullOrWhiteSpace(p.PalletCode))
                    .Select(p => p.PalletCode.Trim())
                    .Distinct()
                    .ToList();

                if (palletCodes.Any())
                {
                    _logger.LogInformation("Checking for duplicate pallet codes.");
                    var existingPallets = await _dbContext.GIV_RM_ReceivePallets
                        .Where(p => palletCodes.Contains(p.PalletCode!) && !p.IsDeleted)
                        .Select(p => p.PalletCode!)
                        .ToListAsync();

                    if (existingPallets.Any())
                    {
                        _logger.LogWarning("Duplicate pallet codes detected: {PalletCodes}", string.Join(", ", existingPallets));
                        return ApiResponseDto<Guid>.ErrorResult($"Duplicate PalletCode(s): {string.Join(", ", existingPallets)}", new List<string> { "Some PalletCodes already exist." });
                    }
                }

                var itemCodes = dto.Receives
                    .SelectMany(r => r.RM_ReceivePallets)
                    .SelectMany(p => p.RM_ReceivePalletItems)
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemCode))
                    .Select(i => i.ItemCode.Trim())
                    .Distinct()
                    .ToList();

                if (itemCodes.Any())
                {
                    _logger.LogInformation("Checking for duplicate item codes.");
                    var existingItems = await _dbContext.GIV_RM_ReceivePalletItems
                        .Where(i => itemCodes.Contains(i.ItemCode) && !i.IsDeleted)
                        .Select(i => i.ItemCode)
                        .ToListAsync();

                    if (existingItems.Any())
                    {
                        _logger.LogWarning("Duplicate item codes detected: {ItemCodes}", string.Join(", ", existingItems));
                        return ApiResponseDto<Guid>.ErrorResult($"Duplicate ItemCode(s): {string.Join(", ", existingItems)}", new List<string> { "Some ItemCodes already exist." });
                    }
                }

                _logger.LogInformation("Creating receive, pallet, item, and photo entities.");

                // Separate container and lorry receives
                var containerReceives = dto.Receives
                    .Where(r => (TransportType)r.TypeID == TransportType.Container && r.ContainerId.HasValue)
                    .ToList();

                var lorryReceives = dto.Receives
                    .Where(r => (TransportType)r.TypeID == TransportType.Lorry || !r.ContainerId.HasValue)
                    .ToList();

                var receiveTuples = new List<(RM_ReceiveCreateDto receiveDto, GIV_RM_Receive receive)>();

                // Process container receives - group by ContainerId
                if (containerReceives.Any())
                {
                    _logger.LogInformation("Processing {Count} container receives.", containerReceives.Count);

                    var containerGroups = containerReceives
                        .GroupBy(r => r.ContainerId)
                        .ToList();

                    foreach (var containerGroup in containerGroups)
                    {
                        // Generate a GroupId if there's more than one receive in this container group
                        Guid? groupId = containerGroup.Count() > 1 ? Guid.NewGuid() : null;

                        _logger.LogInformation("Container {ContainerId}: Creating {Count} receives with GroupId {GroupId}.",
                            containerGroup.Key, containerGroup.Count(), groupId?.ToString() ?? "null");

                        foreach (var receiveDto in containerGroup)
                        {
                            Guid? packageTypeId = null;
                            if (!string.IsNullOrWhiteSpace(receiveDto.PackageType) && packageTypeLookup.TryGetValue(receiveDto.PackageType.Trim(), out var id))
                            {
                                packageTypeId = id;
                            }

                            var receive = new GIV_RM_Receive
                            {
                                Id = Guid.NewGuid(),
                                GroupId = groupId,  // Set the GroupId for container receives in the same group
                                RawMaterialId = rawMaterial.Id,
                                TypeID = (TransportType)receiveDto.TypeID,
                                ContainerId = receiveDto.ContainerId,
                                BatchNo = receiveDto.BatchNo,
                                ReceivedDate = receiveDto.ReceivedDate,
                                ReceivedBy = receiveDto.ReceivedBy,
                                Remarks = receiveDto.Remarks,
                                CreatedBy = username,
                                CreatedAt = now,
                                IsDeleted = false,
                                WarehouseId = warehouseId,
                                PO = receiveDto.PO,
                                PackageTypeId = packageTypeId
                            };

                            receiveTuples.Add((receiveDto, receive));
                        }
                    }
                }

                // Process lorry receives - each as a separate entity
                if (lorryReceives.Any())
                {
                    _logger.LogInformation("Processing {Count} lorry receives.", lorryReceives.Count);

                    foreach (var receiveDto in lorryReceives)
                    {
                        Guid? packageTypeId = null;
                        if (!string.IsNullOrWhiteSpace(receiveDto.PackageType) && packageTypeLookup.TryGetValue(receiveDto.PackageType.Trim(), out var id))
                        {
                            packageTypeId = id;
                        }

                        var receive = new GIV_RM_Receive
                        {
                            Id = Guid.NewGuid(),
                            GroupId = null,  // No GroupId for lorry receives - each is treated separately
                            RawMaterialId = rawMaterial.Id,
                            TypeID = (TransportType)receiveDto.TypeID,
                            ContainerId = receiveDto.ContainerId,
                            BatchNo = receiveDto.BatchNo,
                            ReceivedDate = receiveDto.ReceivedDate,
                            ReceivedBy = receiveDto.ReceivedBy,
                            Remarks = receiveDto.Remarks,
                            CreatedBy = username,
                            CreatedAt = now,
                            IsDeleted = false,
                            WarehouseId = warehouseId,
                            PO = receiveDto.PO,
                            PackageTypeId = packageTypeId
                        };

                        receiveTuples.Add((receiveDto, receive));
                    }
                }

                var palletTuples = receiveTuples.SelectMany(r =>
                    r.receiveDto.RM_ReceivePallets.Select(palletDto =>
                    {
                        var pallet = new GIV_RM_ReceivePallet
                        {
                            Id = Guid.NewGuid(),
                            GIV_RM_ReceiveId = r.receive.Id,
                            PalletCode = palletDto.PalletCode,
                            HandledBy = palletDto.HandledBy,
                            PackSize = palletDto.PackSize,
                            CreatedBy = username,
                            CreatedAt = now,
                            IsDeleted = false,
                            WarehouseId = warehouseId
                        };
                        return (palletDto, pallet, r.receive);
                    })).ToList();

                var items = palletTuples.SelectMany(p =>
                    p.palletDto.RM_ReceivePalletItems.Select(itemDto => new GIV_RM_ReceivePalletItem
                    {
                        Id = Guid.NewGuid(),
                        GIV_RM_ReceivePalletId = p.pallet.Id,
                        ItemCode = itemDto.ItemCode.Trim(),
                        BatchNo = p.receive.BatchNo,
                        ProdDate = itemDto.ProdDate,
                        Remarks = itemDto.Remarks,
                        CreatedBy = username,
                        CreatedAt = now,
                        IsDeleted = false,
                        WarehouseId = warehouseId
                    })).ToList();

                var photoEntities = new List<GIV_RM_ReceivePalletPhoto>();
                foreach (var p in palletTuples)
                {
                    var matchedPhotos = photos.Where(ph => ph.PalletCode == p.pallet.PalletCode);

                    foreach (var photoDto in matchedPhotos)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(photoDto.PhotoFile.FileName)}";
                        var key = $"{_awsS3Config.FolderEnvironment}/{warehouse.Code}/{client.Code}/RM/{p.receive.Id}/{p.pallet.PalletCode}/{uniqueFileName}";
                        var fileType = _awsS3Service.DetermineFileType(photoDto.PhotoFile);
                        var s3Key = await _awsS3Service.UploadFileAsync(photoDto.PhotoFile, key);

                        photoEntities.Add(new GIV_RM_ReceivePalletPhoto
                        {
                            Id = Guid.NewGuid(),
                            GIV_RM_ReceivePalletId = p.pallet.Id,
                            FileName = photoDto.PhotoFile.FileName,
                            PhotoFile = s3Key,
                            ContentType = photoDto.PhotoFile.ContentType,
                            FileSizeBytes = photoDto.PhotoFile.Length,
                            FileType = fileType,
                            CreatedBy = username,
                            CreatedAt = now,
                            IsDeleted = false,
                            WarehouseId = warehouseId
                        });

                        _logger.LogDebug("Uploaded photo for pallet {PalletCode} to S3 key: {S3Key}", p.pallet.PalletCode, s3Key);
                    }
                }

                _logger.LogInformation("Saving all entities to database...");
                await _dbContext.GIV_RM_Receives.AddRangeAsync(receiveTuples.Select(r => r.receive));
                await _dbContext.GIV_RM_ReceivePallets.AddRangeAsync(palletTuples.Select(p => p.pallet));
                await _dbContext.GIV_RM_ReceivePalletItems.AddRangeAsync(items);
                await _dbContext.GIV_RM_ReceivePalletPhotos.AddRangeAsync(photoEntities);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Raw material creation completed successfully.");

                return ApiResponseDto<Guid>.SuccessResult(
                    rawMaterial.Id,
                    isNew ? "Raw material created successfully." : "Raw material already existed. New receives added.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating raw material.");
                return ApiResponseDto<Guid>.ErrorResult("An unexpected error occurred.", new List<string> { ex.Message });
            }
        }
        #endregion
        #region rawMaterial Web
        public async Task<ApiResponseDto<Guid>> CreateRawMaterialFromWebAsync(
    RawMaterialCreateWebDto dto,
    List<RM_ReceivePalletPhotoWebUploadDto> photos,
    string username,
    Guid warehouseId)
        {
            using var scope = _logger.BeginScope(
                "CreateRawMaterialFromWeb - MaterialNo: {MaterialNo}, WarehouseId: {WarehouseId}, Username: {Username}",
                dto.MaterialNo, warehouseId, username);

            try
            {
                var nowUtc = DateTime.UtcNow;
                _logger.LogInformation("Starting raw material creation (web version) at {Timestamp} UTC.", nowUtc);

                var warehouse = await _dbContext.Warehouses
                    .FirstOrDefaultAsync(w => w.Id == warehouseId && !w.IsDeleted);
                if (warehouse == null)
                    return ApiResponseDto<Guid>.ErrorResult($"Warehouse with ID '{warehouseId}' not found.");

                var client = await _dbContext.Clients
                    .FirstOrDefaultAsync(c => c.Id == _currentUserService.CurrentClientId && !c.IsDeleted);
                if (client == null)
                    return ApiResponseDto<Guid>.ErrorResult("Client not found for current user.");

                // Validate photo pallet codes
                var invalidPhotos = photos.Where(p => string.IsNullOrWhiteSpace(p.PalletCode)).ToList();
                if (invalidPhotos.Any())
                    return ApiResponseDto<Guid>.ErrorResult("One or more photo entries are missing a PalletCode.");

                // Check duplicate PalletCodes
                var palletCodes = dto.Receives
                    .SelectMany(r => r.Pallets)
                    .Where(p => !string.IsNullOrWhiteSpace(p.PalletCode))
                    .Select(p => p.PalletCode.Trim())
                    .Distinct()
                    .ToList();

                if (palletCodes.Any())
                {
                    var existing = await _dbContext.GIV_RM_ReceivePallets
                        .Where(p => palletCodes.Contains(p.PalletCode!) && !p.IsDeleted)
                        .Select(p => p.PalletCode!)
                        .ToListAsync();

                    if (existing.Any())
                        return ApiResponseDto<Guid>.ErrorResult($"Duplicate PalletCode(s): {string.Join(", ", existing)}");
                }

                // Check duplicate ItemCodes
                var itemCodes = dto.Receives
                    .SelectMany(r => r.Pallets)
                    .SelectMany(p => p.Items)
                    .Where(i => !string.IsNullOrWhiteSpace(i.ItemCode))
                    .Select(i => i.ItemCode.Trim())
                    .Distinct()
                    .ToList();

                if (itemCodes.Any())
                {
                    var existingItems = await _dbContext.GIV_RM_ReceivePalletItems
                        .Where(i => itemCodes.Contains(i.ItemCode) && !i.IsDeleted)
                        .Select(i => i.ItemCode)
                        .ToListAsync();

                    if (existingItems.Any())
                        return ApiResponseDto<Guid>.ErrorResult($"Duplicate ItemCode(s): {string.Join(", ", existingItems)}");
                }

                // Create or reuse RawMaterial entity
                var existingRawMaterial = await _dbContext.GIV_RawMaterials
                    .FirstOrDefaultAsync(rm => rm.MaterialNo.ToLower() == dto.MaterialNo.ToLower() && !rm.IsDeleted);

                bool isNew = existingRawMaterial == null;
                var rawMaterial = existingRawMaterial ?? new GIV_RawMaterial
                {
                    Id = Guid.NewGuid(),
                    MaterialNo = dto.MaterialNo,
                    Description = dto.Description,
                    CreatedAt = nowUtc,
                    CreatedBy = username,
                    WarehouseId = warehouseId,
                    IsDeleted = false
                };

                if (isNew)
                    await _dbContext.GIV_RawMaterials.AddAsync(rawMaterial);

                var receives = new List<GIV_RM_Receive>();
                var pallets = new List<GIV_RM_ReceivePallet>();
                var items = new List<GIV_RM_ReceivePalletItem>();
                var photosToSave = new List<GIV_RM_ReceivePalletPhoto>();

                foreach (var receiveDto in dto.Receives)
                {
                    var receive = new GIV_RM_Receive
                    {
                        Id = Guid.NewGuid(),
                        RawMaterialId = rawMaterial.Id,
                        TypeID = receiveDto.TypeID,
                        ContainerId = receiveDto.ContainerId,
                        BatchNo = receiveDto.BatchNo,
                        PO = receiveDto.PO,
                        PackageTypeId = receiveDto.PackageTypeId,
                        CreatedAt = nowUtc,
                        CreatedBy = username,
                        ReceivedDate = DateTime.SpecifyKind(receiveDto.ReceivedDate, DateTimeKind.Utc),
                        ReceivedBy = receiveDto.ReceivedBy,
                        Remarks = receiveDto.Remarks,
                        WarehouseId = warehouseId,
                        IsDeleted = false
                    };
                    receives.Add(receive);

                    foreach (var palletDto in receiveDto.Pallets)
                    {
                        var pallet = new GIV_RM_ReceivePallet
                        {
                            Id = Guid.NewGuid(),
                            GIV_RM_ReceiveId = receive.Id,
                            PalletCode = palletDto.PalletCode,
                            HandledBy = palletDto.HandledBy,
                            LocationId = palletDto.LocationId,
                            StoredBy = palletDto.StoredBy,
                            PackSize = palletDto.PackSize,
                            CreatedAt = nowUtc,
                            CreatedBy = username,
                            WarehouseId = warehouseId,
                            IsDeleted = false
                        };
                        pallets.Add(pallet);

                        foreach (var itemDto in palletDto.Items)
                        {
                            items.Add(new GIV_RM_ReceivePalletItem
                            {
                                Id = Guid.NewGuid(),
                                GIV_RM_ReceivePalletId = pallet.Id,
                                ItemCode = itemDto.ItemCode,
                                BatchNo = itemDto.BatchNo,
                                ProdDate = itemDto.ProdDate.HasValue
            ? DateTime.SpecifyKind(itemDto.ProdDate.Value, DateTimeKind.Utc)
            : (DateTime?)null,
                                Remarks = itemDto.Remarks,
                                CreatedAt = nowUtc,
                                CreatedBy = username,
                                WarehouseId = warehouseId,
                                IsDeleted = false
                            });
                        }

                        // Handle photos
                        var matched = photos.Where(ph => ph.PalletCode == pallet.PalletCode);
                        foreach (var photoDto in matched)
                        {
                            var file = photoDto.PhotoFile;
                            var extension = Path.GetExtension(file.FileName);
                            var key = $"{_awsS3Config.FolderEnvironment}/{warehouse.Code}/{client.Code}/RM/{receive.Id}/{pallet.PalletCode}/{Guid.NewGuid()}{extension}";
                            var uploadedKey = await _awsS3Service.UploadFileAsync(file, key);

                            photosToSave.Add(new GIV_RM_ReceivePalletPhoto
                            {
                                Id = Guid.NewGuid(),
                                GIV_RM_ReceivePalletId = pallet.Id,
                                FileName = file.FileName,
                                PhotoFile = uploadedKey,
                                ContentType = file.ContentType,
                                FileSizeBytes = file.Length,
                                FileType = _awsS3Service.DetermineFileType(file),
                                CreatedAt = nowUtc,
                                CreatedBy = username,
                                WarehouseId = warehouseId,
                                IsDeleted = false
                            });
                        }
                    }
                }

                // Persist all
                await _dbContext.GIV_RM_Receives.AddRangeAsync(receives);
                await _dbContext.GIV_RM_ReceivePallets.AddRangeAsync(pallets);
                await _dbContext.GIV_RM_ReceivePalletItems.AddRangeAsync(items);
                await _dbContext.GIV_RM_ReceivePalletPhotos.AddRangeAsync(photosToSave);
                await _dbContext.SaveChangesAsync();

                return ApiResponseDto<Guid>.SuccessResult(
                    rawMaterial.Id,
                    isNew
                        ? "Raw material created successfully."
                        : "Raw material existed; new receives added.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateRawMaterialFromWebAsync");
                return ApiResponseDto<Guid>.ErrorResult(
                    "An unexpected error occurred.",
                    new List<string> { ex.Message });
            }
        }
        #region Load RawMaterial Page
        public async Task<List<GIV_RawMaterial>> GetAllRawMaterialsAsync()
        {
            _logger.LogInformation("Fetching all raw materials with related receives, pallets, and items.");
            var result = await _dbContext.GIV_RawMaterials
                .Include(rm => rm.RM_Receive)
                    .ThenInclude(r => r.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                .Where(rm => !rm.IsDeleted)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} raw materials.", result.Count);
            return result;
        }

        public async Task<List<RawMaterialDetailsDto>> GetRawMaterialsAsync()
        {
            _logger.LogInformation("Mapping all raw materials to DTO.");
            var entities = await GetAllRawMaterialsAsync();
            var dtos = _mapper.Map<List<RawMaterialDetailsDto>>(entities);
            _logger.LogInformation("Mapped {Count} raw materials to DTO.", dtos.Count);
            return dtos;
        }

        public async Task<RawMaterialDetailsDto> GetRawMaterialDetailsByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching raw material details for ID: {Id}", id);
            var rawMaterial = await _dbContext.GIV_RawMaterials
                .Include(rm => rm.RM_Receive)
                    .ThenInclude(c => c.Container)
                .Include(rm => rm.RM_Receive)
                    .ThenInclude(r => r.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                .FirstOrDefaultAsync(rm => rm.Id == id);

            if (rawMaterial == null)
            {
                _logger.LogWarning("Raw material not found for ID: {Id}", id);
            }

            var dto = _mapper.Map<RawMaterialDetailsDto>(rawMaterial);
            _logger.LogInformation("Returning raw material details for ID: {Id}", id);
            return dto;
        }

        public async Task<List<RM_ReceivePalletDetailsDto>> GetPalletsByIdAsync(Guid ReceiveId)
        {
            _logger.LogInformation("Fetching pallets for ReceiveId: {ReceiveId}", ReceiveId);
            var pallets = await _dbContext.GIV_RM_ReceivePallets
                .Where(p => p.GIV_RM_ReceiveId == ReceiveId && !p.IsDeleted)
                .Include(p => p.RM_ReceivePalletItems)
                .Include(p => p.Location)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} pallets.", pallets.Count);
            var dto = pallets.Select(p => new RM_ReceivePalletDetailsDto
            {
                Id = p.Id,
                PalletCode = p.PalletCode,
                HandledBy = p.HandledBy,
                StoredBy = p.StoredBy,
                PackSize = p.PackSize,
                LocationName = p.Location?.Name,
                RM_ReceivePalletItems = p.RM_ReceivePalletItems.Select(i => new RM_ReceivePalletItemDetailsDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks
                }).ToList()
            }).ToList();

            return dto;
        }

        public async Task<(bool success, string message, List<string> urls)> GetPalletPhotoPathByIdAsync(Guid palletId)
        {
            _logger.LogInformation("Fetching pallet photos for PalletId: {PalletId}", palletId);
            var pallet = await _dbContext.GIV_RM_ReceivePallets
                .Include(p => p.RM_ReceivePalletPhotos)
                .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            if (pallet == null || !pallet.RM_ReceivePalletPhotos.Any())
            {
                _logger.LogWarning("No photos found for pallet: {PalletId}", palletId);
                return (false, "No photo found for this pallet", new());
            }

            var s3Keys = pallet.RM_ReceivePalletPhotos
                .Where(p => !string.IsNullOrWhiteSpace(p.PhotoFile))
                .Select(p => p.PhotoFile)
                .ToList();

            if (!s3Keys.Any())
            {
                _logger.LogWarning("No valid S3 photo keys found for pallet: {PalletId}", palletId);
                return (false, "No valid photo keys found.", new());
            }

            var presignedUrlsDict = await _awsS3Service.GenerateMultiplePresignedUrlsAsync(s3Keys, FileType.Photo);
            var urls = s3Keys.Select(k => presignedUrlsDict[k]).ToList();

            _logger.LogInformation("Returning {Count} photo URLs for pallet: {PalletId}", urls.Count, palletId);
            return (true, "Photos found", urls);
        }

        public async Task<List<RawMaterialItemDto>> GetItemsByReceive(Guid receiveId, bool isGroupedView = false, Guid? groupId = null)
        {
            _logger.LogInformation("Fetching items by ReceiveId: {ReceiveId}, IsGrouped: {IsGrouped}, GroupId: {GroupId}",
                receiveId, isGroupedView, groupId);

            IQueryable<GIV_RM_ReceivePalletItem> query;

            if (isGroupedView && groupId.HasValue)
            {
                // Get items from all receives in the group
                query = _dbContext.GIV_RM_Receives
                    .Where(r => r.GroupId == groupId && !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.RM_ReceivePalletItems)
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p.Location);
            }
            else
            {
                // Get items from single receive
                query = _dbContext.GIV_RM_ReceivePalletItems
                    .Include(i => i.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p.Location)
                    .Where(i => i.GIV_RM_ReceivePallet.GIV_RM_ReceiveId == receiveId && !i.IsDeleted);
            }

            var items = await query
                .Select(i => new RawMaterialItemDto
                {
                    HU = i.ItemCode,
                    BatchNo = i.BatchNo,
                    MHU = i.GIV_RM_ReceivePallet.PalletCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks,
                    IsReleased = i.IsReleased,
                    Location = i.GIV_RM_ReceivePallet.Location != null
                        ? i.GIV_RM_ReceivePallet.Location.Barcode
                        : null
                }).ToListAsync();

            _logger.LogInformation("Fetched {Count} items", items.Count);
            return items;
        }

        public async Task<List<RawMaterialGroupedItemDto>> GetGroupedItemsByReceive(Guid receiveId, bool isGroupedView = false, Guid? groupId = null)
        {
            _logger.LogInformation("Fetching grouped items for ReceiveId: {ReceiveId}, IsGrouped: {IsGrouped}, GroupId: {GroupId}",
                receiveId, isGroupedView, groupId);

            var items = await GetItemsByReceive(receiveId, isGroupedView, groupId);

            var grouped = items
                .GroupBy(i => i.BatchNo)
                .Select(g => new RawMaterialGroupedItemDto
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

            _logger.LogInformation("Grouped into {Count} batch(es).", grouped.Count);
            return grouped;
        }

        public async Task<PaginatedResult<RawMaterialItemDto>> GetPaginatedItemsByReceive(
    Guid receiveId, int start, int length, bool isGroupedView = false, Guid? groupId = null)
        {
            _logger.LogInformation("Fetching paginated items for ReceiveId: {ReceiveId} | Start: {Start}, Length: {Length}, IsGrouped: {IsGrouped}",
                receiveId, start, length, isGroupedView);

            IQueryable<GIV_RM_ReceivePalletItem> query;

            if (isGroupedView && groupId.HasValue)
            {
                query = _dbContext.GIV_RM_Receives
                    .Where(r => r.GroupId == groupId && !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.RM_ReceivePalletItems)
                    .Where(i => !i.IsDeleted)
                    .Include(i => i.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p.Location);
            }
            else
            {
                query = _dbContext.GIV_RM_ReceivePalletItems
                    .Include(i => i.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p.Location)
                    .Where(i => i.GIV_RM_ReceivePallet.GIV_RM_ReceiveId == receiveId && !i.IsDeleted);
            }

            var total = await query.CountAsync();
            var items = await query
                .Skip(start)
                .Take(length)
                .Select(i => new RawMaterialItemDto
                {
                    HU = i.ItemCode,
                    BatchNo = i.BatchNo ?? "N/A",
                    MHU = i.GIV_RM_ReceivePallet.PalletCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks,
                    IsReleased = i.IsReleased,
                    Location = i.GIV_RM_ReceivePallet.Location != null
                        ? i.GIV_RM_ReceivePallet.Location.Barcode
                        : null
                }).ToListAsync();

            _logger.LogInformation("Returning {Count} of {Total} items.", items.Count, total);
            return new PaginatedResult<RawMaterialItemDto>
            {
                Items = items,
                TotalCount = total,
                FilteredCount = total
            };
        }

        public async Task<PaginatedResult<RawMaterialGroupedItemDto>> GetPaginatedGroupedItemsByReceive(
    Guid receiveId, int start, int length, bool isGroupedView = false, Guid? groupId = null)
        {
            _logger.LogInformation("Fetching paginated grouped items for ReceiveId: {ReceiveId} | Start: {Start}, Length: {Length}, IsGrouped: {IsGrouped}",
                receiveId, start, length, isGroupedView);

            var items = await GetItemsByReceive(receiveId, isGroupedView, groupId);

            var grouped = items
                .GroupBy(i => i.BatchNo)
                .Select(g => new RawMaterialGroupedItemDto
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
            _logger.LogInformation("Returning {Count} of {Total} grouped items.", paged.Count, grouped.Count);

            return new PaginatedResult<RawMaterialGroupedItemDto>
            {
                Items = paged,
                TotalCount = grouped.Count,
                FilteredCount = grouped.Count
            };
        }



        public async Task<RawMaterialReleaseDto> GetRawMaterialReleaseDetailsAsync(Guid rawmaterialId)
        {
            _logger.LogInformation("Fetching raw material release details for ID: {RawMaterialId}", rawmaterialId);

            var rawMaterial = await _dbContext.GIV_RawMaterials
                .Include(r => r.RM_Receive)
                    .ThenInclude(rcv => rcv.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                .FirstOrDefaultAsync(r => r.Id == rawmaterialId);

            _logger.LogInformation("Fetched raw material for release mapping.");

            return new RawMaterialReleaseDto
            {
                RawMaterialId = rawMaterial.Id,
                MaterialNo = rawMaterial.MaterialNo,
                Description = rawMaterial.Description,
                Receives = rawMaterial.RM_Receive.Select(rcv => new RM_ReceiveReleaseDto
                {
                    Id = rcv.Id,
                    BatchNo = rcv.BatchNo,
                    ReceivedBy = rcv.ReceivedBy,
                    ReceivedDate = rcv.ReceivedDate,
                    Pallets = rcv.RM_ReceivePallets.Select(p => new RM_PalletReleaseDto
                    {
                        Id = p.Id,
                        PalletCode = p.PalletCode,
                        IsReleased = p.IsReleased,
                        HandledBy = p.HandledBy,
                        Items = p.RM_ReceivePalletItems.Select(i => new RM_PalletItemReleaseDto
                        {
                            Id = i.Id,
                            ItemCode = i.ItemCode,
                            IsReleased = i.IsReleased
                        }).ToList()
                    }).ToList()
                }).ToList()
            };
        }
        #endregion
        public async Task<ServiceWebResult> ReleaseRawMaterialAsync(RawMaterialReleaseSubmitDto rawMaterialReleaseDto, string userId)
        {
            _logger.LogInformation("Starting raw material release for user {UserId} and material {RawMaterialId}", userId, rawMaterialReleaseDto.RawMaterialId);
            try
            {
                var isToday = DateTime.UtcNow.Date;
                var allReleases = new List<GIV_RM_Release>();

                // Track which pallets are being explicitly released as whole units
                var explicitlyReleasedPallets = rawMaterialReleaseDto.PalletReleases?
                    .Select(pr => pr.PalletId)
                    .ToHashSet() ?? new HashSet<Guid>();

                // Group releases by date
                var releasesByDate = new Dictionary<DateTime, (List<Guid> PalletIds, List<Guid> ItemIds)>();

                // Process pallet releases and group by date
                if (rawMaterialReleaseDto.PalletReleases != null)
                {
                    foreach (var palletRelease in rawMaterialReleaseDto.PalletReleases)
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
                if (rawMaterialReleaseDto.ItemIds != null && rawMaterialReleaseDto.ItemReleaseDates != null)
                {
                    foreach (var itemId in rawMaterialReleaseDto.ItemIds)
                    {
                        // Get the item to check its pallet
                        var item = await _dbContext.GIV_RM_ReceivePalletItems
                            .Where(i => i.Id == itemId && !i.IsReleased)
                            .Select(i => new { i.Id, i.GIV_RM_ReceivePalletId })
                            .FirstOrDefaultAsync();

                        if (item == null) continue; // Skip if not found or already released

                        // Skip items whose pallets are being explicitly released
                        if (explicitlyReleasedPallets.Contains(item.GIV_RM_ReceivePalletId))
                            continue;

                        // Get release date for this item
                        if (!rawMaterialReleaseDto.ItemReleaseDates.TryGetValue(itemId.ToString(), out string releaseDateStr) ||
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
                    var releaseRecord = new GIV_RM_Release
                    {
                        GIV_RawMaterialId = rawMaterialReleaseDto.RawMaterialId,
                        ReleaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc),
                        ReleasedBy = userId,
                        WarehouseId = Guid.Empty, // Set this later
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = userId,
                        GIV_RM_ReleaseDetails = new List<GIV_RM_ReleaseDetails>()
                    };

                    // Process pallets for this date
                    if (palletIds.Any())
                    {
                        var pallets = await _dbContext.GIV_RM_ReceivePallets
                            .Include(p => p.RM_ReceivePalletItems)
                            .Include(p => p.GIV_RM_Receive)
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
                                var entirePalletDetail = new GIV_RM_ReleaseDetails
                                {
                                    GIV_RM_ReceiveId = pallet.GIV_RM_ReceiveId,
                                    GIV_RM_ReceivePalletId = pallet.Id,
                                    GIV_RM_ReceivePalletItemId = pallet.RM_ReceivePalletItems.FirstOrDefault()?.Id ?? Guid.Empty,
                                    IsEntirePallet = true, // Explicitly releasing the entire pallet
                                    WarehouseId = pallet.WarehouseId,
                                    CreatedBy = userId,
                                    CreatedAt = DateTime.UtcNow
                                };

                                releaseRecord.GIV_RM_ReleaseDetails.Add(entirePalletDetail);

                                //// Mark pallet and items as released if the date is today or earlier
                                //if (releaseDate <= isToday)
                                //{
                                //    pallet.IsReleased = true;
                                //    foreach (var item in pallet.RM_ReceivePalletItems)
                                //    {
                                //        item.IsReleased = true;
                                //    }
                                //}
                            }
                        }
                    }

                    // Process individual items for this date
                    if (itemIds.Any())
                    {
                        var items = await _dbContext.GIV_RM_ReceivePalletItems
                            .Include(i => i.GIV_RM_ReceivePallet)
                            .Where(i => itemIds.Contains(i.Id) && !i.IsReleased)
                            .ToListAsync();

                        if (items.Any())
                        {
                            // Set warehouse ID if not already set
                            if (releaseRecord.WarehouseId == Guid.Empty)
                            {
                                releaseRecord.WarehouseId = items.First().GIV_RM_ReceivePallet.WarehouseId;
                            }

                            // Process each item
                            foreach (var item in items)
                            {
                                // IMPORTANT: Always set IsEntirePallet = false for individual item releases,
                                // even if it completes a pallet
                                var itemDetail = new GIV_RM_ReleaseDetails
                                {
                                    GIV_RM_ReceiveId = item.GIV_RM_ReceivePallet.GIV_RM_ReceiveId,
                                    GIV_RM_ReceivePalletId = item.GIV_RM_ReceivePalletId,
                                    GIV_RM_ReceivePalletItemId = item.Id,
                                    IsEntirePallet = false, // Always false for individual items
                                    WarehouseId = item.GIV_RM_ReceivePallet.WarehouseId,
                                    CreatedBy = userId,
                                    CreatedAt = DateTime.UtcNow
                                };

                                releaseRecord.GIV_RM_ReleaseDetails.Add(itemDetail);

                                // Mark item as released if the date is today or earlier
                                //if (releaseDate <= isToday)
                                //{
                                //    item.IsReleased = true;

                                //    // Check if all items in the pallet are now released
                                //    var pallet = item.GIV_RM_ReceivePallet;
                                //    if (pallet != null)
                                //    {
                                //        // Load all items for the pallet if not already loaded
                                //        if (!_dbContext.Entry(pallet).Collection(p => p.RM_ReceivePalletItems).IsLoaded)
                                //        {
                                //            await _dbContext.Entry(pallet).Collection(p => p.RM_ReceivePalletItems).LoadAsync();
                                //        }

                                //        // Check if all items are now released
                                //        if (pallet.RM_ReceivePalletItems.All(i => i.IsReleased))
                                //        {
                                //            pallet.IsReleased = true;
                                //        }
                                //    }
                                //}
                            }
                        }
                    }

                    // Add the release record to the list
                    if (releaseRecord.GIV_RM_ReleaseDetails.Any())
                    {
                        allReleases.Add(releaseRecord);
                    }
                }

                // Save all changes
                _dbContext.GIV_RM_Releases.AddRange(allReleases);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Release saved successfully for user {UserId}.", userId);
                return new ServiceWebResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while releasing raw material for user {UserId}", userId);
                return new ServiceWebResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        public async System.Threading.Tasks.Task ProcessRawMaterialReleases( DateTime today, CancellationToken stoppingToken)
        {
            _logger.LogInformation("Processing raw material releases scheduled for today or earlier: {Today}", today);
            DateTime todayUtc = today.Kind == DateTimeKind.Unspecified
        ? DateTime.SpecifyKind(today, DateTimeKind.Utc)
        : today.ToUniversalTime();
            // Get all releases scheduled for today or earlier that haven't been processed yet
            var pendingReleases = await _dbContext.GIV_RM_Releases
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p!.RM_ReceivePalletItems)
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePalletItem)
                .Where(r => r.ReleaseDate.Date <= today)
                .ToListAsync(stoppingToken);

            int palletCount = 0;
            int itemCount = 0;

            foreach (var release in pendingReleases)
            {
                // Process entire pallet releases
                var palletReleaseDetails = release.GIV_RM_ReleaseDetails
                    .Where(d => d.IsEntirePallet && d.GIV_RM_ReceivePallet != null && !d.GIV_RM_ReceivePallet.IsReleased)
                    .ToList();

                foreach (var detail in palletReleaseDetails)
                {
                    var pallet = detail.GIV_RM_ReceivePallet;
                    if (pallet != null)
                    {
                        pallet.IsReleased = true;
                        palletCount++;

                        foreach (var item in pallet.RM_ReceivePalletItems)
                        {
                            item.IsReleased = true;
                            itemCount++;
                        }
                    }
                }

                // Process individual item releases
                var itemReleaseDetails = release.GIV_RM_ReleaseDetails
                    .Where(d => !d.IsEntirePallet && d.GIV_RM_ReceivePalletItem != null && !d.GIV_RM_ReceivePalletItem.IsReleased)
                    .ToList();

                foreach (var detail in itemReleaseDetails)
                {
                    var item = detail.GIV_RM_ReceivePalletItem;
                    if (item != null)
                    {
                        item.IsReleased = true;
                        itemCount++;

                        // Check if all items in this pallet are now released
                        var pallet = detail.GIV_RM_ReceivePallet;
                        if (pallet != null)
                        {
                            // Load all items for this pallet if not already loaded
                            if (!_dbContext.Entry(pallet).Collection(p => p.RM_ReceivePalletItems).IsLoaded)
                            {
                                await _dbContext.Entry(pallet).Collection(p => p.RM_ReceivePalletItems).LoadAsync(stoppingToken);
                            }

                            if (pallet.RM_ReceivePalletItems.All(i => i.IsReleased))
                            {
                                pallet.IsReleased = true;
                                palletCount++;
                            }
                        }
                    }
                }
            }

            if (palletCount > 0 || itemCount > 0)
            {
                await _dbContext.SaveChangesAsync(stoppingToken);
                _logger.LogInformation("Raw Material scheduled releases processed: {PalletCount} pallets and {ItemCount} items marked as released",
                    palletCount, itemCount);
            }
            else
            {
                _logger.LogInformation("No pending Raw Material releases to process");
            }
        }
        public async Task<PaginatedResult<RawMaterialDetailsDto>> GetPaginatedRawMaterials(
    string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated raw materials: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, skip, take, sortColumn, sortAscending);

            try
            {
                var query = _dbContext.GIV_RawMaterials
                    .Include(rm => rm.RM_Receive)
                        .ThenInclude(r => r.RM_ReceivePallets)
                            .ThenInclude(p => p.RM_ReceivePalletItems)
                    .Where(rm => !rm.IsDeleted);
                query = query.Where(rm =>
                    rm.RM_Receive
                        .SelectMany(r => r.RM_ReceivePallets)
                        .SelectMany(p => p.RM_ReceivePalletItems)
                        .Any(i => !i.IsReleased) || // Has unreleased items (balance qty > 0)
                    rm.RM_Receive
                        .SelectMany(r => r.RM_ReceivePallets)
                        .Any(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased)) // Has pallets with unreleased items (balance pallets > 0)
                );
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(rm =>
                        rm.MaterialNo.ToLower().Contains(searchTerm) ||
                        (rm.Description != null && rm.Description.ToLower().Contains(searchTerm))
                    );
                }

                var totalCount = await query.CountAsync();

                query = sortColumn switch
                {
                    0 => sortAscending ? query.OrderBy(rm => rm.MaterialNo) : query.OrderByDescending(rm => rm.MaterialNo),
                    1 => sortAscending ? query.OrderBy(rm => rm.Description) : query.OrderByDescending(rm => rm.Description),
                    // Sort by Total Balance Quantity (count of unreleased items)
                    3 => sortAscending
                        ? query.OrderBy(rm => rm.RM_Receive
                            .SelectMany(r => r.RM_ReceivePallets)
                            .SelectMany(p => p.RM_ReceivePalletItems)
                            .Count(i => !i.IsReleased))
                        : query.OrderByDescending(rm => rm.RM_Receive
                            .SelectMany(r => r.RM_ReceivePallets)
                            .SelectMany(p => p.RM_ReceivePalletItems)
                            .Count(i => !i.IsReleased)),

                    // Sort by Total Balance Pallets (count of pallets with unreleased items)
                    4 => sortAscending
                        ? query.OrderBy(rm => rm.RM_Receive
                            .SelectMany(r => r.RM_ReceivePallets)
                            .Where(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased))
                            .Count())
                        : query.OrderByDescending(rm => rm.RM_Receive
                            .SelectMany(r => r.RM_ReceivePallets)
                            .Where(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased))
                            .Count()),
                    _ => query.OrderBy(rm => rm.MaterialNo)
                };

                var paged = await query.Skip(skip).Take(take).ToListAsync();
                var dtoList = _mapper.Map<List<RawMaterialDetailsDto>>(paged);

                _logger.LogInformation("Retrieved {Count} raw materials (skip={Skip}, take={Take}) from total of {TotalCount}",
                    dtoList.Count, skip, take, totalCount);

                return new PaginatedResult<RawMaterialDetailsDto>
                {
                    Items = dtoList,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated raw materials");
                throw;
            }
        }
        public async Task<PaginatedResult<RawMaterialBatchDto>> GetPaginatedRawMaterialsByBatch(
    string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated raw materials by batch: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
        searchTerm, skip, take, sortColumn, sortAscending);
            try
            {
                var query = _dbContext.GIV_RM_Receives
                    .Include(r => r.RawMaterial) 
                    .Include(r => r.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                    .Where(r => !r.IsDeleted);
                query = query.Where(r =>
                    r.RM_ReceivePallets
                        .SelectMany(p => p.RM_ReceivePalletItems)
                        .Any(i => !i.IsReleased)
                );
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(r =>
                        (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                        r.RawMaterial.MaterialNo.ToLower().Contains(searchTerm) ||
                        (r.RawMaterial.Description != null && r.RawMaterial.Description.ToLower().Contains(searchTerm)) ||
                        r.RM_ReceivePallets.Any(p =>
                            (p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                            p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm))
                        )
                    );
                }

                var allMatches = await query.ToListAsync();

                // Expand into individual items
                var allItems = new List<RawMaterialBatchDto>();
                foreach (var receive in allMatches)
                {
                    foreach (var pallet in receive.RM_ReceivePallets)
                    {
                        foreach (var item in pallet.RM_ReceivePalletItems.Where(i => !i.IsReleased))
                        {
                            allItems.Add(new RawMaterialBatchDto
                            {
                                Id = item.Id,
                                BatchNo = receive.BatchNo ?? "N/A",
                                PalletCode = pallet.PalletCode ?? "N/A",
                                ItemCode = item.ItemCode,
                                MaterialNo = receive.RawMaterial.MaterialNo,
                                Description = receive.RawMaterial.Description ?? "N/A",
                                ReceivedDate = receive.ReceivedDate,
                                RawMaterialId = receive.RawMaterialId,

                                // Add the group fields from RawMaterial
                                Group3 = receive.RawMaterial.Group3,
                                Group4_1 = receive.RawMaterial.Group4_1,
                                Group6 = receive.RawMaterial.Group6,
                                Group8 = receive.RawMaterial.Group8,
                                Group9 = receive.RawMaterial.Group9,
                                NDG = receive.RawMaterial.NDG,
                                Scentaurus = receive.RawMaterial.Scentaurus,

                                HasEditAccess = false // Will be set in controller
                            });
                        }
                    }
                }

                // Get total count before sorting and paging
                var totalCount = allItems.Count;

                // Apply sorting
                var sortedItems = sortColumn switch
                {
                    0 => sortAscending ? allItems.OrderBy(x => x.BatchNo).ToList() : allItems.OrderByDescending(x => x.BatchNo).ToList(),
                    1 => sortAscending ? allItems.OrderBy(x => x.PalletCode).ToList() : allItems.OrderByDescending(x => x.PalletCode).ToList(),
                    2 => sortAscending ? allItems.OrderBy(x => x.ItemCode).ToList() : allItems.OrderByDescending(x => x.ItemCode).ToList(),
                    3 => sortAscending ? allItems.OrderBy(x => x.MaterialNo).ToList() : allItems.OrderByDescending(x => x.MaterialNo).ToList(),
                    4 => sortAscending ? allItems.OrderBy(x => x.ReceivedDate).ToList() : allItems.OrderByDescending(x => x.ReceivedDate).ToList(),
                    _ => allItems.OrderByDescending(x => x.ReceivedDate).ToList()
                };

                // Now apply paging to the expanded and sorted list
                var pagedItems = sortedItems.Skip(skip).Take(take).ToList();

                _logger.LogInformation("Retrieved {Count} raw materials by batch (skip={Skip}, take={Take}) from total of {TotalCount}",
                    pagedItems.Count, skip, take, totalCount);

                return new PaginatedResult<RawMaterialBatchDto>
                {
                    Items = pagedItems,
                    TotalCount = totalCount,
                    FilteredCount = totalCount // This should match totalCount since we've already filtered
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated raw materials by batch");
                throw;
            }
        }
        public async Task<PaginatedResult<RM_ReceiveTableRowDto>> GetPaginatedReceivesByRawMaterialIdAsync(
    Guid rawMaterialId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending,
    bool showGrouped)
        {
            _logger.LogInformation("Fetching receives for RawMaterialId: {RawMaterialId} with pagination Start={Start}, Length={Length}",
                rawMaterialId, start, length);

            // Start with base query - get all receives for this raw material
            var query = _dbContext.GIV_RM_Receives
                .AsNoTracking()
                .Include(r => r.RM_ReceivePallets)
                    .ThenInclude(p => p.RM_ReceivePalletItems)
                .Include(r => r.Container)
                .Where(r => r.RawMaterialId == rawMaterialId && !r.IsDeleted);

            // Get total count
            var totalCount = await query.CountAsync();

            // Get all receives first - we'll apply complex filtering in memory
            var allReceives = await query.ToListAsync();
            var filteredReceives = allReceives;

            // Apply filtering in memory
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // Extract date filters if present
                if (TryExtractDateFilters(searchTerm, out string cleanSearchTerm, out DateTime? startDate, out DateTime? endDate))
                {
                    // Apply date filters
                    if (startDate.HasValue)
                    {
                        filteredReceives = filteredReceives.Where(r => r.ReceivedDate >= startDate.Value).ToList();
                    }

                    if (endDate.HasValue)
                    {
                        var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                        filteredReceives = filteredReceives.Where(r => r.ReceivedDate <= endOfDay).ToList();
                    }

                    // Apply text search if there's a clean term after removing date filters
                    if (!string.IsNullOrWhiteSpace(cleanSearchTerm))
                    {
                        searchTerm = cleanSearchTerm;
                    }
                    else
                    {
                        searchTerm = null; // We've handled the date part, no need for text search
                    }
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    // Apply text search to the in-memory list
                    searchTerm = searchTerm.ToLower();

                    // Try parsing search term as number for more flexible numeric matching
                    if (double.TryParse(searchTerm, out double searchNumberDouble))
                    {
                        int searchNumberInt = (int)searchNumberDouble;
                        int maxDifference = Math.Max(1, searchNumberInt / 10); // 10% tolerance for approximate matching

                        filteredReceives = filteredReceives.Where(r =>
                            // Search in Batch Number
                            (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                            // Search in Received By
                            (r.ReceivedBy != null && r.ReceivedBy.ToLower().Contains(searchTerm)) ||
                            // Search in Pallet Code
                            r.RM_ReceivePallets.Any(p => p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                            // Search in Item Code
                            r.RM_ReceivePallets.Any(p => p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm))) ||
                            // Search in Received Date
                            IsDateMatch(r.ReceivedDate, searchTerm) ||
                            // Search in Pack Size or Quantity with approximate matching
                            r.RM_ReceivePallets.Any(p => p.PackSize == searchNumberInt) ||
                            r.RM_ReceivePallets.Count == searchNumberInt ||
                            r.RM_ReceivePallets.Sum(p => p.RM_ReceivePalletItems.Count) == searchNumberInt ||
                            r.RM_ReceivePallets.Count(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased)) == searchNumberInt ||
                            r.RM_ReceivePallets.SelectMany(p => p.RM_ReceivePalletItems).Count(i => !i.IsReleased) == searchNumberInt ||
                            // Approximate match (±10% for larger numbers)
                            (searchNumberInt > 10 && r.RM_ReceivePallets.Any(p => Math.Abs(p.PackSize - searchNumberInt) <= maxDifference)) ||
                            (searchNumberInt > 10 && Math.Abs(r.RM_ReceivePallets.Count - searchNumberInt) <= maxDifference) ||
                            (searchNumberInt > 10 && Math.Abs(r.RM_ReceivePallets.Sum(p => p.RM_ReceivePalletItems.Count) - searchNumberInt) <= maxDifference)
                        ).ToList();
                    }
                    else
                    {
                        // Non-numeric search
                        filteredReceives = filteredReceives.Where(r =>
                            // Search in Batch Number
                            (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                            // Search in Received By
                            (r.ReceivedBy != null && r.ReceivedBy.ToLower().Contains(searchTerm)) ||
                            // Search in Pallet Code
                            r.RM_ReceivePallets.Any(p => p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                            // Search in Item Code
                            r.RM_ReceivePallets.Any(p => p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm)))
                        ).ToList();
                    }
                }
            }

            // Get filtered count before grouping
            var filteredCount = filteredReceives.Count;

            // Dictionary to store group info for each receive
            var groupInfoByReceiveId = new Dictionary<Guid, (bool IsGrouped, Guid? GroupId)>();

            // Dictionary to store receives grouped by their GroupId
            // Use string key to handle null values properly - "null" for ungrouped, Guid.ToString() for grouped
            var groupedReceives = new Dictionary<string, List<GIV_RM_Receive>>();

            // First pass: Get group info for all receives
            foreach (var receive in filteredReceives)
            {
                var (isGrouped, groupId) = await GetReceiveGroupInfoAsync(receive.Id);
                groupInfoByReceiveId[receive.Id] = (isGrouped, groupId);

                // Use string key to avoid null reference issues
                string groupKey = groupId?.ToString() ?? "null";

                // Add to appropriate group collection
                if (!groupedReceives.ContainsKey(groupKey))
                {
                    groupedReceives[groupKey] = new List<GIV_RM_Receive>();
                }
                groupedReceives[groupKey].Add(receive);
            }

            // Now create the result items based on grouping
            var resultItems = new List<RM_ReceiveTableRowDto>();

            // Add grouped receives first (one entry per group)
            foreach (var group in groupedReceives)
            {
                // Skip "null" key (ungrouped receives) for now
                if (group.Key == "null")
                {
                    continue;
                }

                var receives = group.Value;
                var firstReceive = receives.First();
                var groupId = Guid.Parse(group.Key); // Convert string key back to Guid

                // Aggregate data from all receives in this group
                var allPallets = receives.SelectMany(r => r.RM_ReceivePallets ?? new List<GIV_RM_ReceivePallet>()).ToList();
                var allItems = allPallets.SelectMany(p => p.RM_ReceivePalletItems ?? new List<GIV_RM_ReceivePalletItem>()).ToList();

                resultItems.Add(new RM_ReceiveTableRowDto
                {
                    Id = firstReceive.Id,
                    ReceivedDate = firstReceive.ReceivedDate,
                    BatchNo = firstReceive.BatchNo,
                    ContainerUrl = firstReceive.Container?.ContainerURL,
                    TotalPallets = allPallets.Count,
                    Quantity = allItems.Count,
                    BalanceQuantity = allItems.Count(i => !i.IsReleased),
                    BalancePallets = allPallets.Count(p => p.RM_ReceivePalletItems != null && p.RM_ReceivePalletItems.Any(i => !i.IsReleased)),
                    PackSize = allPallets.Sum(p => p.PackSize),
                    ShowGrouped = showGrouped,
                    PalletCodes = string.Join(", ", allPallets.Select(p => p.PalletCode).Where(c => !string.IsNullOrEmpty(c))),
                    ItemCodes = string.Join(", ", allItems.Select(i => i.ItemCode).Where(c => !string.IsNullOrEmpty(c))),
                    // Group properties
                    IsGrouped = true,
                    GroupId = groupId,
                    ReceivesInGroup = receives.Count
                });
            }

            // Add individual ungrouped receives
            if (groupedReceives.ContainsKey("null"))
            {
                foreach (var receive in groupedReceives["null"])
                {
                    var allPallets = receive.RM_ReceivePallets ?? new List<GIV_RM_ReceivePallet>();
                    var allItems = allPallets.SelectMany(p => p.RM_ReceivePalletItems ?? new List<GIV_RM_ReceivePalletItem>()).ToList();

                    resultItems.Add(new RM_ReceiveTableRowDto
                    {
                        Id = receive.Id,
                        ReceivedDate = receive.ReceivedDate,
                        BatchNo = receive.BatchNo,
                        ContainerUrl = receive.Container?.ContainerURL,
                        TotalPallets = allPallets.Count,
                        Quantity = allItems.Count,
                        BalanceQuantity = allItems.Count(i => !i.IsReleased),
                        BalancePallets = allPallets.Count(p => p.RM_ReceivePalletItems != null && p.RM_ReceivePalletItems.Any(i => !i.IsReleased)),
                        PackSize = allPallets.Sum(p => p.PackSize),
                        ShowGrouped = showGrouped,
                        PalletCodes = string.Join(", ", allPallets.Select(p => p.PalletCode).Where(c => !string.IsNullOrEmpty(c))),
                        ItemCodes = string.Join(", ", allItems.Select(i => i.ItemCode).Where(c => !string.IsNullOrEmpty(c))),
                        // Not grouped
                        IsGrouped = false,
                        GroupId = null,
                        ReceivesInGroup = 1
                    });
                }
            }

            _logger.LogInformation("Created {Count} rows ({GroupedCount} groups, {UngroupedCount} individual)",
                resultItems.Count,
                resultItems.Count(r => r.IsGrouped),
                resultItems.Count(r => !r.IsGrouped));

            // Apply sorting
            IEnumerable<RM_ReceiveTableRowDto> sortedItems;
            switch (sortColumn)
            {
                case 0: // ReceivedDate
                    sortedItems = sortAscending
                        ? resultItems.OrderBy(r => r.ReceivedDate)
                        : resultItems.OrderByDescending(r => r.ReceivedDate);
                    break;
                case 1: // PackSize
                    sortedItems = sortAscending
                        ? resultItems.OrderBy(r => r.PackSize)
                        : resultItems.OrderByDescending(r => r.PackSize);
                    break;
                case 2: // Quantity
                    sortedItems = sortAscending
                        ? resultItems.OrderBy(r => r.Quantity)
                        : resultItems.OrderByDescending(r => r.Quantity);
                    break;
                case 3: // TotalPallets
                    sortedItems = sortAscending
                        ? resultItems.OrderBy(r => r.TotalPallets)
                        : resultItems.OrderByDescending(r => r.TotalPallets);
                    break;
                default:
                    sortedItems = resultItems.OrderByDescending(r => r.ReceivedDate);
                    break;
            }

            // Apply pagination
            var paginatedItems = sortedItems.Skip(start).Take(length).ToList();

            return new PaginatedResult<RM_ReceiveTableRowDto>
            {
                TotalCount = totalCount,
                FilteredCount = filteredCount,
                Items = paginatedItems
            };
        }

        #region Helper method to extract date filters from search term
        private bool TryExtractDateFilters(string searchTerm, out string cleanSearchTerm, out DateTime? startDate, out DateTime? endDate)
        {
            // Initialize outputs
            cleanSearchTerm = searchTerm;
            startDate = null;
            endDate = null;

            // Check for date filter pattern
            // Format: "__ds:2023-07-17 __de:2023-07-18 actualSearchTerm"
            bool hasDateFilter = false;

            // Extract start date if present
            string startPattern = "__ds:";
            int startIndex = searchTerm.IndexOf(startPattern);
            if (startIndex >= 0)
            {
                int endOfStart = searchTerm.IndexOf(' ', startIndex + startPattern.Length);
                if (endOfStart < 0) endOfStart = searchTerm.Length;

                string startDateStr = searchTerm.Substring(startIndex + startPattern.Length, endOfStart - (startIndex + startPattern.Length));
                if (DateTime.TryParse(startDateStr, out DateTime parsedStartDate))
                {
                    startDate = parsedStartDate;
                    hasDateFilter = true;

                    // Remove this part from the search term
                    cleanSearchTerm = cleanSearchTerm.Replace(startPattern + startDateStr, "").Trim();
                }
            }

            // Extract end date if present
            string endPattern = "__de:";
            int endIndex = searchTerm.IndexOf(endPattern);
            if (endIndex >= 0)
            {
                int endOfEnd = searchTerm.IndexOf(' ', endIndex + endPattern.Length);
                if (endOfEnd < 0) endOfEnd = searchTerm.Length;

                string endDateStr = searchTerm.Substring(endIndex + endPattern.Length, endOfEnd - (endIndex + endPattern.Length));
                if (DateTime.TryParse(endDateStr, out DateTime parsedEndDate))
                {
                    endDate = parsedEndDate;
                    hasDateFilter = true;

                    // Remove this part from the search term
                    cleanSearchTerm = cleanSearchTerm.Replace(endPattern + endDateStr, "").Trim();
                }
            }

            return hasDateFilter;
        }

        private void ApplyDateRangeFilter(ref IQueryable<GIV_RM_Receive> query, string[] dates)
        {
            // Parse start date if provided
            if (dates.Length > 0 && !string.IsNullOrEmpty(dates[0]))
            {
                if (DateTime.TryParse(dates[0], out DateTime parsedStartDate))
                {
                    // Convert to UTC for PostgreSQL
                    var startDate = DateTime.SpecifyKind(parsedStartDate, DateTimeKind.Utc);
                    _logger.LogDebug("Parsed start date (UTC): {StartDate}", startDate);
                    // Apply start date filter
                    query = query.Where(r => r.ReceivedDate >= startDate);
                }
            }

            // Parse end date if provided
            if (dates.Length > 1 && !string.IsNullOrEmpty(dates[1]))
            {
                if (DateTime.TryParse(dates[1], out DateTime parsedEndDate))
                {
                    // Convert to UTC for PostgreSQL - end of day
                    var endDate = DateTime.SpecifyKind(parsedEndDate.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
                    _logger.LogDebug("Parsed end date (UTC): {EndDate}", endDate);
                    // Apply end date filter
                    query = query.Where(r => r.ReceivedDate <= endDate);
                }
            }
        }

        private void ApplyTextSearch(ref IQueryable<GIV_RM_Receive> query, string searchTerm)
        {
            searchTerm = searchTerm.ToLower();

            // Try parsing search term as number for more flexible numeric matching
            if (double.TryParse(searchTerm, out double searchNumberDouble))
            {
                int searchNumberInt = (int)searchNumberDouble;

                // Check if it's a year (4 digits between 1900-2100)
                bool isYear = searchTerm.Length == 4 && searchNumberInt >= 1900 && searchNumberInt <= 2100;
                _logger.LogDebug("Search term '{SearchTerm}' identified as year: {IsYear}, Value: {Year}",
                    searchTerm, isYear, isYear ? searchNumberInt : 0);

                // Check if search term is a month (1-12)
                bool isMonth = searchTerm.Length <= 2 && searchNumberInt >= 1 && searchNumberInt <= 12;

                // Check if search term is a day (1-31)
                bool isDay = searchTerm.Length <= 2 && searchNumberInt >= 1 && searchNumberInt <= 31;

                // Try parsing as complete date
                bool isCompleteDate = DateTime.TryParse(searchTerm, out DateTime searchDate);
                _logger.LogDebug("Search term '{SearchTerm}' identified as complete date: {IsCompleteDate}, Value: {Date}",
                    searchTerm, isCompleteDate, isCompleteDate ? searchDate.ToString("yyyy-MM-dd") : "N/A");

                // Build the query with date and numeric search
                query = query.Where(r =>
                    // Search in Batch Number
                    (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                    // Search in Received By
                    (r.ReceivedBy != null && r.ReceivedBy.ToLower().Contains(searchTerm)) ||
                    // Search in Pallet Code
                    r.RM_ReceivePallets.Any(p => p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                    // Search in Item Code
                    r.RM_ReceivePallets.Any(p => p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm))) ||
                    // Search in Received Date
                    (
                        // Use IsDateMatch for more flexible date matching
                        IsDateMatch(r.ReceivedDate, searchTerm)
                    ) ||
                    // Search in Pack Size or Quantity with approximate matching
                    (
                        // Exact match
                        r.RM_ReceivePallets.Any(p => p.PackSize == searchNumberInt) ||
                        r.RM_ReceivePallets.Count == searchNumberInt ||
                        r.RM_ReceivePallets.Sum(p => p.RM_ReceivePalletItems.Count) == searchNumberInt ||
                        r.RM_ReceivePallets.Count(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased)) == searchNumberInt ||
                        r.RM_ReceivePallets.SelectMany(p => p.RM_ReceivePalletItems).Count(i => !i.IsReleased) == searchNumberInt ||

                        // Approximate match (±10% for larger numbers)
                        (searchNumberInt > 10 && r.RM_ReceivePallets.Any(p =>
                            Math.Abs(p.PackSize - searchNumberInt) <= Math.Max(1, searchNumberInt / 10))) ||
                        (searchNumberInt > 10 && Math.Abs(r.RM_ReceivePallets.Count - searchNumberInt) <= Math.Max(1, searchNumberInt / 10)) ||
                        (searchNumberInt > 10 && Math.Abs(r.RM_ReceivePallets.Sum(p => p.RM_ReceivePalletItems.Count) - searchNumberInt) <= Math.Max(1, searchNumberInt / 10))
                    )
                );
            }
            else
            {
                // Non-numeric search
                query = query.Where(r =>
                    // Search in Batch Number
                    (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                    // Search in Received By
                    (r.ReceivedBy != null && r.ReceivedBy.ToLower().Contains(searchTerm)) ||
                    // Search in Pallet Code
                    r.RM_ReceivePallets.Any(p => p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                    // Search in Item Code
                    r.RM_ReceivePallets.Any(p => p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm)))
                );
            }
        }

        // Helper method for date matching
        private bool IsDateMatch(DateTime recordDate, string searchTerm)
        {
            // Try parsing as a complete date first
            if (DateTime.TryParse(searchTerm, out DateTime searchDate))
            {
                return recordDate.Date == searchDate.Date;
            }

            // Try parsing as a year (4 digits)
            if (searchTerm.Length == 4 && int.TryParse(searchTerm, out int year) && year >= 1900 && year <= 2100)
            {
                return recordDate.Year == year;
            }

            // Try parsing as a month (1-12)
            if (searchTerm.Length <= 2 && int.TryParse(searchTerm, out int month) && month >= 1 && month <= 12)
            {
                return recordDate.Month == month;
            }

            // Try parsing as a day (1-31)
            if (searchTerm.Length <= 2 && int.TryParse(searchTerm, out int day) && day >= 1 && day <= 31)
            {
                return recordDate.Day == day;
            }

            // Check if the search term appears in the formatted date string (multiple formats for flexibility)
            string isoDate = recordDate.ToString("yyyy-MM-dd");
            string shortDate = recordDate.ToString("d");
            string longDate = recordDate.ToString("D");
            string monthYear = recordDate.ToString("MMM yyyy");

            return isoDate.Contains(searchTerm) ||
                   shortDate.Contains(searchTerm) ||
                   longDate.ToLower().Contains(searchTerm.ToLower()) ||
                   monthYear.ToLower().Contains(searchTerm.ToLower());
        }
        #endregion
        public async Task<PaginatedResult<RM_ReceivePalletDetailsDto>> GetPaginatedPalletsByReceiveIdAsync(
    Guid receiveId,
    int start,
    int length,
    string searchTerm = null,
    bool isGroupedView = false,
    Guid? groupId = null)
        {
            _logger.LogInformation("Fetching pallets for ReceiveId: {ReceiveId} with Start={Start}, Length={Length}, SearchTerm={SearchTerm}, IsGrouped={IsGrouped}, GroupId={GroupId}",
                receiveId, start, length, searchTerm, isGroupedView, groupId);

            IQueryable<GIV_RM_ReceivePallet> query;

            // If this is a grouped view and we have a group ID, get pallets from all receives in the group
            if (isGroupedView && groupId.HasValue)
            {
                _logger.LogInformation("Using group-based pallet retrieval for GroupId={GroupId}", groupId.Value);

                // Get pallets from all receives in this group
                query = _dbContext.GIV_RM_Receives
                    .Where(r => r.GroupId == groupId && !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .Include(p => p.RM_ReceivePalletItems)
                    .Include(p => p.Location);
            }
            else
            {
                // Get pallets from just this receive
                query = _dbContext.GIV_RM_ReceivePallets
                    .Where(p => p.GIV_RM_ReceiveId == receiveId && !p.IsDeleted)
                    .Include(p => p.RM_ReceivePalletItems)
                    .Include(p => p.Location);
            }

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                // Apply searching to include pallet codes and item codes
                query = query.Where(p =>
                    // Search in PalletCode (MHU)
                    (p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                    // Search in ItemCode (HU)
                    p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm)) ||
                    // Search in Location name
                    (p.Location != null &&
                        ((p.Location.Name != null && p.Location.Name.ToLower().Contains(searchTerm)) ||
                        (p.Location.Barcode != null && p.Location.Barcode.ToLower().Contains(searchTerm)))) ||
                    // Search in Remarks
                    p.RM_ReceivePalletItems.Any(i => i.Remarks != null && i.Remarks.ToLower().Contains(searchTerm))
                );
            }

            var totalCount = await query.CountAsync();
            var filteredCount = totalCount; // If no search applied, filtered count equals total count

            // If search was applied, get the filtered count
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                filteredCount = await query.CountAsync();
            }

            var pallets = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip(start)
                .Take(length)
                .ToListAsync();

            _logger.LogInformation("Fetched {Count} pallets from total {TotalCount}",
                pallets.Count, totalCount);

            var dto = pallets.Select(p => new RM_ReceivePalletDetailsDto
            {
                Id = p.Id,
                PalletCode = p.PalletCode,
                HandledBy = p.HandledBy,
                StoredBy = p.StoredBy,
                PackSize = p.PackSize,
                LocationName = p.Location?.Barcode,

                RM_ReceivePalletItems = p.RM_ReceivePalletItems.Select(i => new RM_ReceivePalletItemDetailsDto
                {
                    Id = i.Id,
                    ItemCode = i.ItemCode,
                    ProdDate = i.ProdDate,
                    Remarks = i.Remarks,
                    IsReleased = i.IsReleased
                }).ToList(),
                Quantity = p.RM_ReceivePalletItems.Count,
                QuantityBalance = p.RM_ReceivePalletItems.Count(i => !i.IsReleased)
            }).ToList();

            return new PaginatedResult<RM_ReceivePalletDetailsDto>
            {
                Items = dto,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        public async Task<RM_ReceivePalletItemDto?> GetItemById(Guid ItemId)
        {
            _logger.LogInformation("Fetching item by ID: {ItemId}", ItemId);

            var item = await _dbContext.GIV_RM_ReceivePalletItems
                .Include(i => i.GIV_RM_ReceivePallet)
                .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(i => i.Id == ItemId && !i.IsDeleted);

            if (item == null)
            {
                _logger.LogWarning("Item not found: {ItemId}", ItemId);
                return null;
            }

            _logger.LogInformation("Item found: {ItemCode}", item.ItemCode);
            return new RM_ReceivePalletItemDto
            {
                Id = item.Id,
                ItemCode = item.ItemCode,
                BatchNo = item.BatchNo,
                ProdDate = item.ProdDate,
                Remarks = item.Remarks,
                IsReleased = item.IsReleased
            };
        }
        //edit
        public async Task<ApiResponseDto<string>> UpdateItemAsync(RM_ReceivePalletItemDto dto)
        {
            _logger.LogInformation("Updating item with ID: {ItemId}", dto.Id);

            try
            {
                var item = await _dbContext.GIV_RM_ReceivePalletItems
                    .FirstOrDefaultAsync(i => i.Id == dto.Id && !i.IsDeleted);

                if (item == null)
                {
                    _logger.LogWarning("Item not found: {ItemId}", dto.Id);
                    return ApiResponseDto<string>.ErrorResult("Item not found.");
                }

                item.ItemCode = dto.ItemCode;
                item.BatchNo = dto.BatchNo;
                item.ProdDate = dto.ProdDate.HasValue
            ? DateTime.SpecifyKind(dto.ProdDate.Value, DateTimeKind.Utc)
            : (DateTime?)null;
                item.Remarks = dto.Remarks;

                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Item updated successfully: {ItemId}", dto.Id);
                return ApiResponseDto<string>.SuccessResult("OK", "Item updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update item: {ItemId}", dto.Id);
                return ApiResponseDto<string>.ErrorResult("Failed to update item.", new List<string> { ex.Message });
            }
        }

        public async Task<(RM_ReceivePalletDetailsDto Pallet, List<LocationDto> Locations)> GetReceivePalletForEditAsync(Guid palletId)
        {
            _logger.LogInformation("Fetching pallet for edit: {PalletId}", palletId);

            var entity = await _dbContext.GIV_RM_ReceivePallets
                .AsNoTracking()
                .Include(p => p.GIV_RM_Receive)
                .Include(p => p.Location)
                .Include(p => p.RM_ReceivePalletItems)
                .Include(p => p.RM_ReceivePalletPhotos)
                .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            var palletDto = _mapper.Map<RM_ReceivePalletDetailsDto>(entity);

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

        public async Task<ApiResponseDto<string>> UpdatePalletAsync(RM_ReceivePalletEditDto dto)
        {
            _logger.LogInformation("Updating pallet with ID: {PalletId}", dto.Id);

            var pallet = await _dbContext.GIV_RM_ReceivePallets
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

        public async Task<RawMaterialEditDto?> GetRawMaterialForEditAsync(Guid id)
        {
            _logger.LogInformation("Fetching raw material for edit: {RawMaterialId}", id);

            var rawMaterial = await _dbContext.GIV_RawMaterials
                .Where(rm => rm.Id == id && !rm.IsDeleted)
                .FirstOrDefaultAsync();

            if (rawMaterial == null)
            {
                _logger.LogWarning("Raw material not found: {RawMaterialId}", id);
                return null;
            }

            _logger.LogInformation("Raw material fetched: {RawMaterialId}", id);
            return new RawMaterialEditDto
            {
                Id = rawMaterial.Id,
                MaterialNo = rawMaterial.MaterialNo,
                Description = rawMaterial.Description
            };
        }

        public async Task<ApiResponseDto<string>> UpdateRawMaterialAsync(RawMaterialEditDto dto, string username)
        {
            _logger.LogInformation("Updating raw material: {RawMaterialId} by user: {Username}", dto.Id, username);

            var rawMaterial = await _dbContext.GIV_RawMaterials
                .FirstOrDefaultAsync(rm => rm.Id == dto.Id && !rm.IsDeleted);

            if (rawMaterial == null)
            {
                _logger.LogWarning("Raw material not found: {RawMaterialId}", dto.Id);
                return ApiResponseDto<string>.ErrorResult("Raw material not found.");
            }

            rawMaterial.MaterialNo = dto.MaterialNo.Trim();
            rawMaterial.Description = dto.Description?.Trim();
            rawMaterial.ModifiedBy = username;
            rawMaterial.ModifiedAt = DateTime.UtcNow.ToLocalTime();

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Raw material updated successfully: {RawMaterialId}", dto.Id);
            return ApiResponseDto<string>.SuccessResult("Raw material updated.");
        }

        public async Task<RM_ReceiveEditDto?> GetEditDtoAsync(Guid id)
        {
            _logger.LogInformation("Fetching receive for edit: {ReceiveId}", id);

            var entity = await _dbContext.GIV_RM_Receives
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

            if (entity == null)
            {
                _logger.LogWarning("Receive not found: {ReceiveId}", id);
                return null;
            }

            _logger.LogInformation("Receive fetched for edit: {ReceiveId}", id);
            return new RM_ReceiveEditDto
            {
                Id = entity.Id,
                BatchNo = entity.BatchNo,
                ReceivedDate = entity.ReceivedDate,
                ReceivedBy = entity.ReceivedBy,
                Remarks = entity.Remarks
            };
        }

        public async Task<ApiResponseDto<string>> UpdateReceiveAsync(RM_ReceiveEditDto dto, string username)
        {
            _logger.LogInformation("Updating receive: {ReceiveId} by user: {Username}", dto.Id, username);

            var entity = await _dbContext.GIV_RM_Receives
                .FirstOrDefaultAsync(r => r.Id == dto.Id && !r.IsDeleted);

            if (entity == null)
            {
                _logger.LogWarning("Receive not found: {ReceiveId}", dto.Id);
                return ApiResponseDto<string>.ErrorResult("Receive not found.");
            }

            entity.BatchNo = dto.BatchNo;
            entity.ReceivedDate = DateTime.SpecifyKind(dto.ReceivedDate, DateTimeKind.Utc);
            entity.ReceivedBy = dto.ReceivedBy;
            entity.Remarks = dto.Remarks;
            entity.ModifiedAt = DateTime.UtcNow.ToLocalTime();
            entity.ModifiedBy = username;

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Receive updated successfully: {ReceiveId}", dto.Id);
            return ApiResponseDto<string>.SuccessResult("Receive updated successfully.");
        }
        #region Import Raw Materials data
        public async Task<RawMaterialImportResult> ImportRawMaterialsAsync(IFormFile file, Guid warehouseId)
        {
            _logger.LogInformation("Importing raw materials from file: {FileName}", file.FileName);
            var result = new RawMaterialImportResult();
            var now = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            var username = "import-user";

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var validationResult = await ValidateRawMaterialImportAsync(file);
                _logger.LogInformation("Validation complete. TotalRows={TotalRows}, ValidItems={ValidCount}, Errors={ErrorCount}", validationResult.TotalRows, validationResult.ValidItems.Count, validationResult.Errors.Count);

                result.TotalRows = validationResult.TotalRows;
                result.Errors.AddRange(validationResult.Errors);
                result.Warnings.AddRange(validationResult.Warnings);

                if (!validationResult.IsValid)
                {
                    result.Success = false;
                    return result;
                }

                foreach (var item in validationResult.ValidItems)
                {
                    try
                    {
                        _logger.LogDebug("Processing row {RowNumber} with MaterialNo={MaterialNo}", item.RowNumber, item.MaterialNo);

                        var importResultItem = new RawMaterialImportResultItem
                        {
                            RowNumber = item.RowNumber,
                            MaterialNo = item.MaterialNo,
                            Description = item.Description ?? string.Empty
                        };

                        var rawMaterial = await _dbContext.GIV_RawMaterials
                            .FirstOrDefaultAsync(r => r.MaterialNo == item.MaterialNo && !r.IsDeleted);

                        if (rawMaterial == null)
                        {
                            rawMaterial = new GIV_RawMaterial
                            {
                                Id = Guid.NewGuid(),
                                MaterialNo = item.MaterialNo,
                                Description = item.Description,
                                CreatedAt = now,
                                CreatedBy = username,
                                WarehouseId = warehouseId,
                                IsDeleted = false
                            };
                            await _dbContext.GIV_RawMaterials.AddAsync(rawMaterial);
                            importResultItem.IsRawMaterialInserted = true;
                        }
                        else if (item.IsUpdate)
                        {
                            rawMaterial.Description = item.Description;
                            rawMaterial.ModifiedAt = now;
                            rawMaterial.ModifiedBy = username;
                            importResultItem.IsRawMaterialUpdated = true;
                        }

                        if (!item.ReceivedDate.HasValue)
                        {
                            importResultItem.Status = "Success";
                            importResultItem.Message = importResultItem.IsRawMaterialInserted ? "RawMaterial inserted only" : "RawMaterial updated only";
                            result.Results.Add(importResultItem);
                            result.SuccessCount++;
                            continue;
                        }

                        var receive = new GIV_RM_Receive
                        {
                            Id = Guid.NewGuid(),
                            RawMaterialId = rawMaterial.Id,
                            BatchNo = item.BatchNo ?? string.Empty,
                            ReceivedDate = DateTime.SpecifyKind(item.ReceivedDate.Value, DateTimeKind.Utc),
                            ReceivedBy = item.ReceivedBy ?? username,
                            Remarks = item.ReceiveRemarks,
                            TypeID = item.TypeID ?? TransportType.Container,
                            CreatedAt = now,
                            CreatedBy = username,
                            WarehouseId = warehouseId,
                            IsDeleted = false
                        };
                        await _dbContext.GIV_RM_Receives.AddAsync(receive);
                        importResultItem.IsReceiveInserted = true;

                        if (string.IsNullOrWhiteSpace(item.PalletCode))
                        {
                            importResultItem.Status = "Success";
                            importResultItem.Message = "RawMaterial and Receive inserted (no pallet)";
                            result.Results.Add(importResultItem);
                            result.SuccessCount++;
                            continue;
                        }

                        var palletExists = await _dbContext.GIV_RM_ReceivePallets.AnyAsync(p => p.PalletCode == item.PalletCode && !p.IsDeleted);
                        if (palletExists)
                        {
                            throw new Exception($"Duplicate PalletCode '{item.PalletCode}' found in database.");
                        }

                        var pallet = new GIV_RM_ReceivePallet
                        {
                            Id = Guid.NewGuid(),
                            GIV_RM_ReceiveId = receive.Id,
                            PalletCode = item.PalletCode,
                            HandledBy = item.HandledBy ?? username,
                            LocationId = item.LocationId,
                            StoredBy = item.StoredBy,
                            PackSize = item.PackSize ?? 1,
                            CreatedAt = now,
                            CreatedBy = username,
                            WarehouseId = warehouseId,
                            IsDeleted = false
                        };
                        await _dbContext.GIV_RM_ReceivePallets.AddAsync(pallet);
                        importResultItem.IsPalletInserted = true;

                        if (!string.IsNullOrWhiteSpace(item.ItemCode))
                        {
                            var itemExists = await _dbContext.GIV_RM_ReceivePalletItems.AnyAsync(i => i.ItemCode == item.ItemCode && !i.IsDeleted);
                            if (itemExists)
                            {
                                throw new Exception($"Duplicate ItemCode '{item.ItemCode}' found in database.");
                            }

                            var palletItem = new GIV_RM_ReceivePalletItem
                            {
                                Id = Guid.NewGuid(),
                                GIV_RM_ReceivePalletId = pallet.Id,
                                ItemCode = item.ItemCode,
                                BatchNo = item.ItemBatchNo,
                                ProdDate = item.ProdDate.HasValue ? DateTime.SpecifyKind(item.ProdDate.Value, DateTimeKind.Utc) : now,
                                Remarks = item.ItemRemarks,
                                CreatedAt = now,
                                CreatedBy = username,
                                WarehouseId = warehouseId,
                                IsDeleted = false
                            };
                            await _dbContext.GIV_RM_ReceivePalletItems.AddAsync(palletItem);
                            importResultItem.IsItemInserted = true;
                        }

                        if (!string.IsNullOrWhiteSpace(item.PhotoFile))
                        {
                            var photo = new GIV_RM_ReceivePalletPhoto
                            {
                                Id = Guid.NewGuid(),
                                GIV_RM_ReceivePalletId = pallet.Id,
                                PhotoFile = item.PhotoFile,
                                CreatedAt = now,
                                CreatedBy = username,
                                WarehouseId = warehouseId,
                                IsDeleted = false
                            };
                            await _dbContext.GIV_RM_ReceivePalletPhotos.AddAsync(photo);
                            importResultItem.IsPhotoInserted = true;
                        }

                        var parts = new List<string>();
                        if (importResultItem.IsRawMaterialInserted) parts.Add("RawMaterial inserted");
                        if (importResultItem.IsRawMaterialUpdated) parts.Add("RawMaterial updated");
                        if (importResultItem.IsReceiveInserted) parts.Add("Receive inserted");
                        if (importResultItem.IsPalletInserted) parts.Add("Pallet inserted");
                        if (importResultItem.IsItemInserted) parts.Add("Item inserted");
                        if (importResultItem.IsPhotoInserted) parts.Add("Photo inserted");

                        importResultItem.Status = "Success";
                        importResultItem.Message = string.Join(", ", parts);

                        result.Results.Add(importResultItem);
                        result.SuccessCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process row {Row}", item.RowNumber);

                        result.ErrorCount++;
                        result.Errors.Add($"Row {item.RowNumber}: {ex.Message}");
                        result.Results.Add(new RawMaterialImportResultItem
                        {
                            RowNumber = item.RowNumber,
                            MaterialNo = item.MaterialNo,
                            Description = item.Description ?? string.Empty,
                            Status = "Error",
                            Message = ex.Message
                        });
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                result.ProcessedRows = result.SuccessCount + result.ErrorCount;
                result.Success = result.ErrorCount == 0;

                _logger.LogInformation("Import completed. Success: {SuccessCount}, Errors: {ErrorCount}", result.SuccessCount, result.ErrorCount);
                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error during import.");
                result.Success = false;
                result.Errors.Add("Unexpected error during import: " + ex.Message);
                return result;
            }
        }

        public async Task<RawMaterialImportValidationResult> ValidateRawMaterialImportAsync(IFormFile file)
        {
            var result = new RawMaterialImportValidationResult();
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            try
            {
                _logger.LogInformation("Validating import file: {FileName}", file.FileName);

                if (extension != ".xlsx")
                {
                    result.Errors.Add("Unsupported file format. Only .xlsx is supported.");
                    return result;
                }

                ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

                using var stream = file.OpenReadStream();
                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null || worksheet.Dimension == null)
                {
                    result.Errors.Add("No worksheet or content found.");
                    return result;
                }

                int rowCount = worksheet.Dimension.Rows;
                result.TotalRows = rowCount - 1;

                for (int row = 2; row <= rowCount; row++)
                {
                    var materialNo = worksheet.Cells[row, 1].Text.Trim();
                    var description = worksheet.Cells[row, 2].Text.Trim();
                    var batchNo = worksheet.Cells[row, 3].Text.Trim();
                    var receivedDate = worksheet.Cells[row, 4].Text.Trim();
                    var receivedBy = worksheet.Cells[row, 5].Text.Trim();
                    var remarks = worksheet.Cells[row, 6].Text.Trim();
                    var typeStr = worksheet.Cells[row, 7].Text.Trim();

                    var palletCode = worksheet.Cells[row, 8].Text.Trim();
                    var handledBy = worksheet.Cells[row, 9].Text.Trim();
                    var locationIdStr = worksheet.Cells[row, 10].Text.Trim();
                    var storedBy = worksheet.Cells[row, 11].Text.Trim();
                    var packSizeStr = worksheet.Cells[row, 12].Text.Trim();

                    var itemCode = worksheet.Cells[row, 13].Text.Trim();
                    var itemBatchNo = worksheet.Cells[row, 14].Text.Trim();
                    var prodDateStr = worksheet.Cells[row, 15].Text.Trim();
                    var dgStr = worksheet.Cells[row, 16].Text.Trim();
                    var itemRemarks = worksheet.Cells[row, 17].Text.Trim();
                    var photoFile = worksheet.Cells[row, 18].Text.Trim();

                    var validationItem = new RawMaterialImportValidationItem
                    {
                        RowNumber = row,
                        MaterialNo = materialNo,
                        Description = description,
                        BatchNo = batchNo,
                        ReceivedBy = receivedBy,
                        ReceiveRemarks = remarks,
                        PalletCode = palletCode,
                        HandledBy = handledBy,
                        StoredBy = storedBy,
                        ItemCode = itemCode,
                        ItemBatchNo = itemBatchNo,
                        ItemRemarks = itemRemarks,
                        PhotoFile = photoFile
                    };

                    if (DateTime.TryParse(receivedDate, out var parsedReceivedDate))
                        validationItem.ReceivedDate = parsedReceivedDate;
                    if (DateTime.TryParse(prodDateStr, out var parsedProdDate))
                        validationItem.ProdDate = parsedProdDate;
                    if (Enum.TryParse<TransportType>(typeStr, out var transportType))
                        validationItem.TypeID = transportType;
                    if (Guid.TryParse(locationIdStr, out var locationId))
                        validationItem.LocationId = locationId;
                    if (int.TryParse(packSizeStr, out var packSize))
                        validationItem.PackSize = packSize;
                    if (bool.TryParse(dgStr, out var dg))
                        validationItem.DG = dg;

                    var exists = await _dbContext.GIV_RawMaterials.AnyAsync(r => r.MaterialNo == materialNo && !r.IsDeleted);
                    validationItem.IsUpdate = exists;

                    if (string.IsNullOrWhiteSpace(materialNo))
                        validationItem.Errors.Add("MaterialNo is required");

                    validationItem.IsValid = !validationItem.Errors.Any();

                    if (validationItem.IsValid)
                        result.ValidItems.Add(validationItem);
                    else
                        result.Errors.AddRange(validationItem.Errors.Select(e => $"Row {row}: {e}"));
                }

                result.IsValid = !result.Errors.Any();
                _logger.LogInformation("Validation complete. Valid={IsValid}, TotalValid={ValidCount}, Errors={ErrorCount}", result.IsValid, result.ValidItems.Count, result.Errors.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Validation error for import file: {FileName}", file.FileName);
                result.Errors.Add("Validation failed: " + ex.Message);
                return result;
            }
        }
        #endregion
        public async Task<(byte[] fileContent, string fileName)> GenerateRawMaterialExcelAsync(DateTime startDate, DateTime endDate)
        {
            _logger.LogInformation("Generating raw material report from {Start} to {End} (monthly)", startDate, endDate);
            return await GenerateExcelInternal(startDate, endDate, "monthly");
        }

        public async Task<(byte[] fileContent, string fileName)> GenerateRawMaterialWeeklyExcelAsync(DateTime cutoffDate)
        {
            var startDate = cutoffDate.AddDays(-6);
            var endDate = cutoffDate;
            _logger.LogInformation("Generating raw material report for weekly cutoff ending {EndDate}", cutoffDate);
            return await GenerateExcelInternal(startDate, endDate, "weekly");
        }

        private async Task<(byte[] fileContent, string fileName)> GenerateExcelInternal(DateTime start, DateTime end, string mode)
        {
            _logger.LogInformation("Running internal Excel generation for mode={Mode}, range={Start} to {End}", mode, start, end);

            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");

            var receives = await _dbContext.GIV_RM_Receives
                .Include(r => r.RawMaterial)
                .Include(r => r.RM_ReceivePallets)
                    .ThenInclude(p => p.RM_ReceivePalletItems)
                .Include(r => r.RM_ReceivePallets)
                    .ThenInclude(p => p.Location)
                .Where(r => r.ReceivedDate >= start && r.ReceivedDate <= end && !r.IsDeleted)
                .ToListAsync();

            var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("RawMaterialReport");

            // Header row
            ws.Cells[1, 1].Value = "Loc";
            ws.Cells[1, 2].Value = "Receipt date";
            ws.Cells[1, 3].Value = "MaterialNo";
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
                var palletGroupCounts = receive.RM_ReceivePallets
                    .Where(p => p.Location != null)
                    .GroupBy(p => new { receive.BatchNo, LocationCode = p.Location!.Barcode })
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var pallet in receive.RM_ReceivePallets)
                {
                    var items = pallet.RM_ReceivePalletItems;
                    var locationCode = pallet.Location?.Barcode;
                    var noItems = items.Count;
                    var qty = pallet.PackSize * noItems;
                    var batchKey = new { receive.BatchNo, LocationCode = locationCode };

                    ws.Cells[row, 1].Value = locationCode;
                    ws.Cells[row, 2].Value = receive.ReceivedDate.ToString("dd-MMM-yy");
                    ws.Cells[row, 3].Value = receive.RawMaterial.MaterialNo;
                    ws.Cells[row, 4].Value = receive.RawMaterial.Description;
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
                .SelectMany(r => r.RM_ReceivePallets.Select(p => new
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
            var filename = $"RawMaterialReport_{mode}_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx";

            _logger.LogInformation("Excel report generated: {Filename}, Rows={RowCount}", filename, row);
            return (stream.ToArray(), filename);
        }

        public async Task<ApiResponseDto<string>> UpdatePalletLocationAsync(Guid palletId, Guid? locationId, string username)
        {
            _logger.LogInformation("Updating pallet location for pallet {PalletId} to location {LocationId} by {Username}", palletId, locationId, username);

            var rmPallet = await _dbContext.GIV_RM_ReceivePallets
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == palletId && !p.IsDeleted);

            if (rmPallet == null)
            {
                _logger.LogWarning("Pallet not found: {PalletId}", palletId);
                return ApiResponseDto<string>.ErrorResult($"Raw Material Pallet with ID '{palletId}' not found.", new List<string> { "Invalid pallet ID." });
            }

            var oldLocationId = rmPallet.LocationId;

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

            rmPallet.LocationId = newLocation.Id;
            rmPallet.StoredBy = username;
            rmPallet.ModifiedBy = username;
            rmPallet.ModifiedAt = DateTime.UtcNow;
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
                var pallet = await _dbContext.GIV_RM_ReceivePallets
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
        public async Task<List<string>> GetDistinctBatchNumbersAsync()
        {
            return await _dbContext.GIV_RM_Receives
                .Where(r => !r.IsDeleted && r.BatchNo != null && r.BatchNo != "")
                .Select(r => r.BatchNo)
                .Distinct()
                .OrderBy(b => b)
                .Select(b => b!)
                .ToListAsync();
        }

        public async Task<List<string>> GetDistinctMaterialNumbersAsync()
        {
            return await _dbContext.GIV_RawMaterials
                .Where(r => !r.IsDeleted)
                .Select(r => r.MaterialNo)
                .Distinct()
                .OrderBy(m => m)
                .ToListAsync();
        }
        public async Task<PaginatedResult<RawMaterialPalletDto>> GetPaginatedRawMaterialsByPallet(
    string searchTerm, int skip, int take, int sortColumn, bool sortAscending)
        {
            _logger.LogDebug("Getting paginated raw materials by pallet: SearchTerm={SearchTerm}, Skip={Skip}, Take={Take}, SortColumn={SortColumn}, SortAscending={SortAscending}",
        searchTerm, skip, take, sortColumn, sortAscending);
            try
            {
                // First, create a query to get all the data that matches the search criteria
                var query = _dbContext.GIV_RM_Receives
                    .Include(r => r.RawMaterial) 
                    .Include(r => r.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                    .Where(r => !r.IsDeleted);
                query = query.Where(r =>
                    r.RM_ReceivePallets.Any(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased))
                );
                // Apply search if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(r =>
                        (r.BatchNo != null && r.BatchNo.ToLower().Contains(searchTerm)) ||
                        r.RawMaterial.MaterialNo.ToLower().Contains(searchTerm) ||
                        (r.RawMaterial.Description != null && r.RawMaterial.Description.ToLower().Contains(searchTerm)) ||
                        r.RM_ReceivePallets.Any(p =>
                            (p.PalletCode != null && p.PalletCode.ToLower().Contains(searchTerm)) ||
                            p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchTerm))
                        )
                    );
                }

                // Get all matching records first
                var allMatches = await query.ToListAsync();

                // Expand into pallet-level items (group by pallet)
                var allPallets = new List<RawMaterialPalletDto>();
                foreach (var receive in allMatches)
                {
                    foreach (var pallet in receive.RM_ReceivePallets.Where(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased)))
                    {
                        var unreleasedItems = pallet.RM_ReceivePalletItems.Where(i => !i.IsReleased).ToList();
                        var itemCodes = unreleasedItems.Select(i => i.ItemCode).ToList();

                        allPallets.Add(new RawMaterialPalletDto
                        {
                            Id = pallet.Id,
                            BatchNo = receive.BatchNo ?? "N/A",
                            PalletCode = pallet.PalletCode ?? "N/A",
                            ItemCode = itemCodes.FirstOrDefault() ?? "N/A", // Show first item code in main column
                            MaterialNo = receive.RawMaterial.MaterialNo,
                            Description = receive.RawMaterial.Description ?? "N/A",
                            ReceivedDate = receive.ReceivedDate,
                            ItemCount = unreleasedItems.Count,
                            AllItemCodes = string.Join(", ", itemCodes), // Keep all codes for reference/tooltip
                            RawMaterialId = receive.RawMaterialId,

                            // Add the group fields from RawMaterial
                            Group3 = receive.RawMaterial.Group3,
                            Group4_1 = receive.RawMaterial.Group4_1,
                            Group6 = receive.RawMaterial.Group6,
                            Group8 = receive.RawMaterial.Group8,
                            Group9 = receive.RawMaterial.Group9,
                            NDG = receive.RawMaterial.NDG,
                            Scentaurus = receive.RawMaterial.Scentaurus,

                            HasEditAccess = false // Will be set in controller
                        });
                    }
                }

                // Get total count before sorting and paging
                var totalCount = allPallets.Count;

                // Apply sorting
                var sortedPallets = sortColumn switch
                {
                    0 => sortAscending ? allPallets.OrderBy(x => x.BatchNo).ToList() : allPallets.OrderByDescending(x => x.BatchNo).ToList(),
                    1 => sortAscending ? allPallets.OrderBy(x => x.PalletCode).ToList() : allPallets.OrderByDescending(x => x.PalletCode).ToList(),
                    2 => sortAscending ? allPallets.OrderBy(x => x.ItemCode).ToList() : allPallets.OrderByDescending(x => x.ItemCode).ToList(),
                    3 => sortAscending ? allPallets.OrderBy(x => x.MaterialNo).ToList() : allPallets.OrderByDescending(x => x.MaterialNo).ToList(),
                    4 => sortAscending ? allPallets.OrderBy(x => x.ReceivedDate).ToList() : allPallets.OrderByDescending(x => x.ReceivedDate).ToList(),
                    _ => allPallets.OrderByDescending(x => x.ReceivedDate).ToList()
                };

                // Apply paging to the sorted list
                var pagedPallets = sortedPallets.Skip(skip).Take(take).ToList();

                _logger.LogInformation("Retrieved {Count} raw materials by pallet (skip={Skip}, take={Take}) from total of {TotalCount}",
                    pagedPallets.Count, skip, take, totalCount);

                return new PaginatedResult<RawMaterialPalletDto>
                {
                    Items = pagedPallets,
                    TotalCount = totalCount,
                    FilteredCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paginated raw materials by pallet");
                throw;
            }
        }
        public async Task<List<string>> GetPalletCodesAsync()
        {
            _logger.LogDebug("Getting all pallet codes for autocomplete");
            try
            {
                var palletCodes = await _dbContext.GIV_RM_Receives
                    .Include(r => r.RM_ReceivePallets)
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !string.IsNullOrEmpty(p.PalletCode))
                    .Select(p => p.PalletCode!)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} unique pallet codes", palletCodes.Count);
                return palletCodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pallet codes");
                throw;
            }
        }
        public async Task<RawMaterialTotalsDto> GetRawMaterialTotals()
        {
            try
            {
                _logger.LogDebug("Calculating total raw material statistics");

                var totalBalancePallet = await _dbContext.GIV_RM_ReceivePallets
            .Include(p => p.GIV_RM_Receive)
                .ThenInclude(r => r.RawMaterial)
            .Where(p => !p.IsDeleted)
            .Where(p => !p.GIV_RM_Receive.IsDeleted)
            .Where(p => !p.GIV_RM_Receive.RawMaterial.IsDeleted)
            .Where(p => p.RM_ReceivePalletItems.Any(i => !i.IsDeleted && !i.IsReleased))
            .CountAsync();

                var totalBalanceQty = await _dbContext.GIV_RM_ReceivePalletItems
            .Include(i => i.GIV_RM_ReceivePallet)
                .ThenInclude(p => p.GIV_RM_Receive)
                    .ThenInclude(r => r.RawMaterial)
            .Where(i => !i.IsDeleted && !i.IsReleased)
            .Where(i => !i.GIV_RM_ReceivePallet.IsDeleted)
            .Where(i => !i.GIV_RM_ReceivePallet.GIV_RM_Receive.IsDeleted)
            .Where(i => !i.GIV_RM_ReceivePallet.GIV_RM_Receive.RawMaterial.IsDeleted)
            .CountAsync();

                _logger.LogInformation("Raw material totals calculated: TotalBalancePallet={TotalBalancePallet}, TotalBalanceQty={TotalBalanceQty}",
                    totalBalancePallet, totalBalanceQty);

                return new RawMaterialTotalsDto
                {
                    TotalBalancePallet = totalBalancePallet,
                    TotalBalanceQty = totalBalanceQty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating raw material totals");
                throw;
            }
        }
        public async Task<List<RawMaterialExportDto>> GetRawMaterialsForExport(string searchTerm)
        {
            try
            {
                _logger.LogDebug("Getting raw materials for export: SearchTerm={SearchTerm}", searchTerm);

                // Start with a query for raw materials
                var query = _dbContext.GIV_RawMaterials
                    .Include(rm => rm.RM_Receive)
                        .ThenInclude(r => r.RM_ReceivePallets)
                            .ThenInclude(p => p.RM_ReceivePalletItems)
                    .Where(rm => !rm.IsDeleted);

                // Apply search filter if provided
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(rm =>
                        rm.MaterialNo.ToLower().Contains(searchTerm) ||
                        (rm.Description != null && rm.Description.ToLower().Contains(searchTerm))
                    );
                }

                // Execute query to get raw materials with all related data
                var rawMaterials = await query.ToListAsync();

                // Transform the data into the export format
                var exportData = new List<RawMaterialExportDto>();

                foreach (var rm in rawMaterials)
                {
                    // Group by receive to get different receiving dates
                    var receiveGroups = rm.RM_Receive
                        .Where(r => !r.IsDeleted)
                        .GroupBy(r => r.ReceivedDate.Date);

                    foreach (var receiveGroup in receiveGroups)
                    {
                        var receivedDate = receiveGroup.Key;

                        // Get all valid pallets for this material and receive date
                        var validPallets = receiveGroup
                            .SelectMany(r => r.RM_ReceivePallets)
                            .Where(p => !p.IsDeleted)
                            .Where(p => p.RM_ReceivePalletItems.Any(i => !i.IsDeleted && !i.IsReleased))
                            .ToList();

                        if (validPallets.Any())
                        {
                            var palletCodes = string.Join(", ", validPallets
                                .Select(p => p.PalletCode)
                                .Where(pc => !string.IsNullOrEmpty(pc))
                                .OrderBy(pc => pc));

                            exportData.Add(new RawMaterialExportDto
                            {
                                MaterialNo = rm.MaterialNo,
                                Description = rm.Description,
                                ReceivedDate = receivedDate,
                                TotalPallets = validPallets.Count,
                                PalletCodes = palletCodes
                            });
                        }
                    }
                }

                _logger.LogInformation("Generated export data with {Count} rows", exportData.Count);
                return exportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving raw materials for export");
                throw;
            }
        }
        public async Task<bool> UpdateRawMaterialGroupFieldAsync(Guid rawMaterialId, string fieldName, bool value, string userName)
        {
            _logger.LogInformation("Updating group field {FieldName} to {Value} for raw material {RawMaterialId}", fieldName, value, rawMaterialId);

            try
            {
                var rawMaterial = await _dbContext.GIV_RawMaterials
                    .FirstOrDefaultAsync(rm => rm.Id == rawMaterialId && !rm.IsDeleted);

                if (rawMaterial == null)
                {
                    _logger.LogWarning("Raw material not found: {RawMaterialId}", rawMaterialId);
                    return false;
                }

                // Update the appropriate field based on fieldName
                switch (fieldName.ToLower())
                {
                    case "group3":
                        rawMaterial.Group3 = value;
                        break;
                    case "group4_1":
                        rawMaterial.Group4_1 = value;
                        break;
                    case "group6":
                        rawMaterial.Group6 = value;
                        break;
                    case "group8":
                        rawMaterial.Group8 = value;
                        break;
                    case "group9":
                        rawMaterial.Group9 = value;
                        break;
                    case "ndg":
                        rawMaterial.NDG = value;
                        break;
                    case "scentaurus":
                        rawMaterial.Scentaurus = value;
                        break;
                    default:
                        _logger.LogWarning("Invalid field name: {FieldName}", fieldName);
                        return false;
                }

                rawMaterial.ModifiedAt = DateTime.UtcNow;
                rawMaterial.ModifiedBy = userName;

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Successfully updated group field {FieldName} for raw material {RawMaterialId}", fieldName, rawMaterialId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group field {FieldName} for raw material {RawMaterialId}", fieldName, rawMaterialId);
                return false;
            }
        }

        public async Task<(bool IsGrouped, Guid? GroupId)> GetReceiveGroupInfoAsync(Guid receiveId)
        {
            var receive = await _dbContext.GIV_RM_Receives
                .FirstOrDefaultAsync(r => r.Id == receiveId && !r.IsDeleted);

            if (receive != null && receive.GroupId.HasValue)
            {
                return (true, receive.GroupId);
            }

            return (false, null);
        }
        #region release and release detail
        public async Task<PaginatedResult<RM_ReleaseTableRowDto>> GetPaginatedReleasesByRawMaterialIdAsync(
    Guid rawMaterialId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            _logger.LogInformation("Fetching releases for RawMaterialId: {RawMaterialId} with pagination Start={Start}, Length={Length}",
                rawMaterialId, start, length);

            // Start with base query - get all releases for this raw material with user joins
            var query = _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePallet)
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePalletItem)
                .Where(r => r.GIV_RawMaterialId == rawMaterialId && !r.IsDeleted);

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply search filter if provided - simplified for EF Core translation
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                // Try to parse as date for simple date comparisons
                DateTime searchDate = default;
                bool isDateSearch = false;
                if (DateTime.TryParse(searchTerm, out searchDate))
                {
                    // Ensure the DateTime is treated as UTC for PostgreSQL compatibility
                    searchDate = DateTime.SpecifyKind(searchDate, DateTimeKind.Utc);
                    isDateSearch = true;
                }

                int searchYear = 0;
                bool isYearSearch = searchTerm.Length == 4 && int.TryParse(searchTerm, out searchYear) && searchYear >= 1900 && searchYear <= 2100;

                query = query.Where(r =>
                    // Search in ReleasedBy username
                    r.ReleasedBy.ToLower().Contains(searchTerm) ||
                    // Search in remarks
                    (r.Remarks != null && r.Remarks.ToLower().Contains(searchTerm)) ||
                    // Search in actual released by (username)
                    (r.ActualReleasedBy != null && r.ActualReleasedBy.ToLower().Contains(searchTerm)) ||
                    // Simple date searches that EF Core can translate
                    (isDateSearch && r.ReleaseDate.Date == searchDate.Date) ||
                    (isDateSearch && r.ActualReleaseDate.HasValue && r.ActualReleaseDate.Value.Date == searchDate.Date) ||
                    (isYearSearch && r.ReleaseDate.Year == searchYear) ||
                    (isYearSearch && r.ActualReleaseDate.HasValue && r.ActualReleaseDate.Value.Year == searchYear) ||
                    // Search in pallet codes within release details
                    r.GIV_RM_ReleaseDetails.Any(d =>
                        d.GIV_RM_ReceivePallet != null &&
                        d.GIV_RM_ReceivePallet.PalletCode != null &&
                        d.GIV_RM_ReceivePallet.PalletCode.ToLower().Contains(searchTerm)
                    ) ||
                    // Search in item codes within release details
                    r.GIV_RM_ReleaseDetails.Any(d =>
                        d.GIV_RM_ReceivePalletItem != null &&
                        d.GIV_RM_ReceivePalletItem.ItemCode.ToLower().Contains(searchTerm)
                    )
                );
            }

            var filteredCount = await query.CountAsync();

            // Apply sorting
            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate),
                1 => sortAscending ? query.OrderBy(r => r.ReleasedBy) : query.OrderByDescending(r => r.ReleasedBy),
                2 => sortAscending ? query.OrderBy(r => r.ActualReleaseDate ?? DateTime.MaxValue) : query.OrderByDescending(r => r.ActualReleaseDate ?? DateTime.MinValue),
                _ => query.OrderByDescending(r => r.ReleaseDate)
            };

            // Get paginated results
            var releases = await query
                .Skip(start)
                .Take(length)
                .ToListAsync();

            // Get all ReleasedBy user IDs that we need to look up (only for ReleasedBy, not ActualReleasedBy)
            var releasedByUserIds = releases
                .Select(r => r.ReleasedBy)
                .Where(userId => !string.IsNullOrEmpty(userId))
                .Distinct()
                .ToList();

            // Get user information in one query (only for ReleasedBy users)
            var users = await _dbContext.Users
                .Where(u => releasedByUserIds.Contains(u.Id.ToString()) && !u.IsDeleted)
                .Select(u => new { UserId = u.Id.ToString(), u.Username, u.FullName })
                .ToListAsync();

            var userLookup = users.ToDictionary(u => u.UserId, u => new { u.Username, u.FullName });

            // Transform to DTOs
            var releaseDtos = new List<RM_ReleaseTableRowDto>();

            foreach (var release in releases)
            {
                var details = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();

                var entirePalletDetails = details.Where(d => d.IsEntirePallet).ToList();
                var individualItemDetails = details.Where(d => !d.IsEntirePallet).ToList();

                var releasedDetails = details.Where(d => d.ActualReleaseDate.HasValue).ToList();
                var releasedPalletDetails = releasedDetails.Where(d => d.IsEntirePallet).ToList();
                var releasedItemDetails = releasedDetails.Where(d => !d.IsEntirePallet).ToList();

                // Get username for ReleasedBy only
                var releasedByUser = userLookup.TryGetValue(release.ReleasedBy, out var releasedUser) ? releasedUser : null;

                var dto = new RM_ReleaseTableRowDto
                {
                    Id = release.Id,
                    ReleaseDate = release.ReleaseDate,
                    ReleasedBy = releasedByUser?.Username ?? release.ReleasedBy,
                    ReleasedByFullName = releasedByUser?.FullName,
                    Remarks = release.Remarks,
                    ActualReleaseDate = release.ActualReleaseDate,
                    ActualReleasedBy = release.ActualReleasedBy, // Use as-is since it's already a username

                    TotalPallets = entirePalletDetails.Count,
                    TotalItems = individualItemDetails.Count,
                    EntirePalletCount = entirePalletDetails.Count,
                    IndividualItemCount = individualItemDetails.Count,

                    IsCompleted = release.ActualReleaseDate.HasValue,
                    IsPartiallyReleased = releasedDetails.Any() && !release.ActualReleaseDate.HasValue,
                    ReleasedPallets = releasedPalletDetails.Count,
                    ReleasedItems = releasedItemDetails.Count
                };

                releaseDtos.Add(dto);
            }

            _logger.LogInformation("Returning {Count} releases from total {TotalCount}", releaseDtos.Count, totalCount);

            return new PaginatedResult<RM_ReleaseTableRowDto>
            {
                Items = releaseDtos,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }

        public async Task<RM_ReleaseDetailsViewDto?> GetReleaseDetailsByIdAsync(Guid releaseId)
        {
            _logger.LogInformation("Fetching release details for ReleaseId: {ReleaseId}", releaseId);

            var release = await _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RM_ReleaseDetails)
                .Include(r => r.GIV_RawMaterial)
                .FirstOrDefaultAsync(r => r.Id == releaseId && !r.IsDeleted);

            if (release == null)
            {
                _logger.LogWarning("Release not found: {ReleaseId}", releaseId);
                return null;
            }

            // Get user information for ReleasedBy
            var releasedByUser = await _dbContext.Users
                .Where(u => u.Id.ToString() == release.ReleasedBy && !u.IsDeleted)
                .Select(u => new { u.Username, u.FullName })
                .FirstOrDefaultAsync();

            var details = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
            var entirePalletDetails = details.Where(d => d.IsEntirePallet).ToList();
            var individualItemDetails = details.Where(d => !d.IsEntirePallet).ToList();
            var releasedDetails = details.Where(d => d.ActualReleaseDate.HasValue).ToList();

            var dto = new RM_ReleaseDetailsViewDto
            {
                Id = release.Id,
                ReleaseDate = release.ReleaseDate,
                ReleasedBy = releasedByUser?.Username ?? release.ReleasedBy,
                ReleasedByFullName = releasedByUser?.FullName,
                Remarks = release.Remarks,
                ActualReleaseDate = release.ActualReleaseDate,
                ActualReleasedBy = release.ActualReleasedBy,

                TotalPallets = entirePalletDetails.Count,
                TotalItems = individualItemDetails.Count,
                EntirePalletCount = entirePalletDetails.Count,
                IndividualItemCount = individualItemDetails.Count,
                ReleasedPallets = releasedDetails.Count(d => d.IsEntirePallet),
                ReleasedItems = releasedDetails.Count(d => !d.IsEntirePallet),

                IsCompleted = release.ActualReleaseDate.HasValue,
                StatusText = release.ActualReleaseDate.HasValue ? "Completed" :
                            (releasedDetails.Any() ? "Partially Released" :
                            (release.ReleaseDate.Date <= DateTime.UtcNow.Date ? "Due for Release" : "Scheduled")),

                RawMaterialId = release.GIV_RawMaterialId,
                MaterialNo = release.GIV_RawMaterial.MaterialNo,
                MaterialDescription = release.GIV_RawMaterial.Description ?? ""
            };

            _logger.LogInformation("Returning release details for ReleaseId: {ReleaseId}", releaseId);
            return dto;
        }

        public async Task<PaginatedResult<RM_ReleaseDetailsDto>> GetPaginatedReleaseDetailsAsync(
    Guid releaseId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending)
        {
            _logger.LogInformation("Fetching paginated release details for ReleaseId: {ReleaseId} with pagination Start={Start}, Length={Length}",
                releaseId, start, length);

            // Query release details with related data
            var query = _dbContext.GIV_RM_ReleaseDetails
                .AsNoTracking()
                .Include(d => d.GIV_RM_Receive) // Direct relationship
                .Include(d => d.GIV_RM_ReceivePallet)
                    .ThenInclude(p => p.Location)
                .Include(d => d.GIV_RM_ReceivePalletItem)
                .Where(d => d.GIV_RM_ReleaseId == releaseId && !d.IsDeleted);

            var totalCount = await query.CountAsync();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(d =>
                    (d.GIV_RM_ReceivePallet != null && d.GIV_RM_ReceivePallet.PalletCode != null && d.GIV_RM_ReceivePallet.PalletCode.ToLower().Contains(searchTerm)) ||
                    (d.GIV_RM_ReceivePalletItem != null && d.GIV_RM_ReceivePalletItem.ItemCode.ToLower().Contains(searchTerm)) ||
                    (d.GIV_RM_Receive != null && d.GIV_RM_Receive.BatchNo != null && d.GIV_RM_Receive.BatchNo.ToLower().Contains(searchTerm)) ||
                    (d.GIV_RM_ReceivePallet != null && d.GIV_RM_ReceivePallet.Location != null && d.GIV_RM_ReceivePallet.Location.Barcode != null && d.GIV_RM_ReceivePallet.Location.Barcode.ToLower().Contains(searchTerm)) ||
                    (d.ActualReleasedBy != null && d.ActualReleasedBy.ToLower().Contains(searchTerm))
                );
            }

            var filteredCount = await query.CountAsync();

            // Apply sorting
            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(d => d.IsEntirePallet).ThenBy(d => d.GIV_RM_ReceivePallet != null ? d.GIV_RM_ReceivePallet.PalletCode : "") : query.OrderByDescending(d => d.IsEntirePallet).ThenByDescending(d => d.GIV_RM_ReceivePallet != null ? d.GIV_RM_ReceivePallet.PalletCode : ""),
                1 => sortAscending ? query.OrderBy(d => d.GIV_RM_ReceivePallet != null ? d.GIV_RM_ReceivePallet.PalletCode : "") : query.OrderByDescending(d => d.GIV_RM_ReceivePallet != null ? d.GIV_RM_ReceivePallet.PalletCode : ""),
                2 => sortAscending ? query.OrderBy(d => d.GIV_RM_ReceivePalletItem != null ? d.GIV_RM_ReceivePalletItem.ItemCode : "") : query.OrderByDescending(d => d.GIV_RM_ReceivePalletItem != null ? d.GIV_RM_ReceivePalletItem.ItemCode : ""),
                3 => sortAscending ? query.OrderBy(d => d.GIV_RM_Receive != null ? d.GIV_RM_Receive.BatchNo : "") : query.OrderByDescending(d => d.GIV_RM_Receive != null ? d.GIV_RM_Receive.BatchNo : ""),
                4 => sortAscending ? query.OrderBy(d => d.ActualReleaseDate ?? DateTime.MaxValue) : query.OrderByDescending(d => d.ActualReleaseDate ?? DateTime.MinValue),
                _ => query.OrderBy(d => d.IsEntirePallet).ThenBy(d => d.GIV_RM_ReceivePallet != null ? d.GIV_RM_ReceivePallet.PalletCode : "")
            };

            // Get paginated results
            var releaseDetails = await query
                .Skip(start)
                .Take(length)
                .ToListAsync();

            // Get all pallet IDs for entire pallet releases to fetch their item codes
            var entirePalletIds = releaseDetails
                .Where(d => d.IsEntirePallet && d.GIV_RM_ReceivePalletId.HasValue)
                .Select(d => d.GIV_RM_ReceivePalletId!.Value)
                .Distinct()
                .ToList();

            // Fetch all item codes for entire pallets
            var palletItemCodes = new Dictionary<Guid, List<string>>();
            if (entirePalletIds.Any())
            {
                var itemCodesData = await _dbContext.GIV_RM_ReceivePalletItems
                    .Where(i => entirePalletIds.Contains(i.GIV_RM_ReceivePalletId) && !i.IsDeleted)
                    .Select(i => new { i.GIV_RM_ReceivePalletId, i.ItemCode })
                    .ToListAsync();

                palletItemCodes = itemCodesData
                    .GroupBy(x => x.GIV_RM_ReceivePalletId)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ItemCode).ToList());
            }

            // Transform to DTOs
            var detailDtos = new List<RM_ReleaseDetailsDto>();

            foreach (var d in releaseDetails)
            {
                var dto = new RM_ReleaseDetailsDto
                {
                    Id = d.Id,
                    ReleaseId = d.GIV_RM_ReleaseId,
                    ReceiveId = d.GIV_RM_ReceiveId,
                    PalletId = d.GIV_RM_ReceivePalletId ?? Guid.Empty, // Handle nullable PalletId
                    ItemId = d.IsEntirePallet ? null : d.GIV_RM_ReceivePalletItemId, // Only set ItemId for individual items
                    IsEntirePallet = d.IsEntirePallet,
                    ActualReleaseDate = d.ActualReleaseDate,
                    ActualReleasedBy = d.ActualReleasedBy,

                    PalletCode = d.GIV_RM_ReceivePallet?.PalletCode ?? "",
                    ItemCode = d.GIV_RM_ReceivePalletItem?.ItemCode,
                    AllItemCodes = d.IsEntirePallet && d.GIV_RM_ReceivePalletId.HasValue && palletItemCodes.ContainsKey(d.GIV_RM_ReceivePalletId.Value)
                        ? palletItemCodes[d.GIV_RM_ReceivePalletId.Value]
                        : new List<string>(),
                    BatchNo = d.GIV_RM_Receive?.BatchNo ?? "",
                    LocationCode = d.GIV_RM_ReceivePallet?.Location?.Barcode,
                    ReceivedDate = d.GIV_RM_Receive?.ReceivedDate ?? DateTime.MinValue,
                    PackSize = d.GIV_RM_ReceivePallet?.PackSize ?? 0,
                    HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAW_MATERIAL_DELETE)
                };

                detailDtos.Add(dto);
            }

            _logger.LogInformation("Returning {Count} release details from total {TotalCount}", detailDtos.Count, totalCount);

            return new PaginatedResult<RM_ReleaseDetailsDto>
            {
                Items = detailDtos,
                TotalCount = totalCount,
                FilteredCount = filteredCount
            };
        }
#endregion
        #region Job Releases View

        public async Task<PaginatedResult<JobReleaseTableRowDto>> GetPaginatedJobReleasesAsync(
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending)
        {
            _logger.LogInformation("Fetching job releases with pagination Start={Start}, Length={Length}", start, length);

            // Query releases that have JobId (job-based releases)
            var baseQuery = _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RawMaterial)
                .Include(r => r.GIV_RM_ReleaseDetails)
                .Where(r => r.JobId != null && !r.IsDeleted);

            // Group by JobId to get job-level data
            var jobQuery = baseQuery
                .GroupBy(r => r.JobId)
                .Select(g => new
                {
                    JobId = g.Key,
                    PlannedReleaseDate = g.Min(r => r.ReleaseDate), // Earliest release date in the job
                    CreatedBy = g.First().CreatedBy, // Assuming all releases in a job have same creator
                    CreatedAt = g.Min(r => r.CreatedAt), // Earliest creation time
                    MaterialCount = g.Count(), // Number of different materials
                    TotalReleases = g.Count(),
                    CompletedReleases = g.Count(r => r.ActualReleaseDate != null),
                    Materials = g.Select(r => r.GIV_RawMaterial.MaterialNo).ToList(),
                    AllReleases = g.ToList()
                });

            var totalCount = await jobQuery.CountAsync();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();

                // Parse date search
                DateTime searchDate = default;
                bool isDateSearch = DateTime.TryParse(searchTerm, out searchDate);
                if (isDateSearch)
                {
                    searchDate = DateTime.SpecifyKind(searchDate, DateTimeKind.Utc);
                }

                // Parse year search
                int searchYear = 0;
                bool isYearSearch = searchTerm.Length == 4 && int.TryParse(searchTerm, out searchYear) && searchYear >= 1900 && searchYear <= 2100;

                jobQuery = jobQuery.Where(j =>
                    // Search in JobId
                    j.JobId.ToString().ToLower().Contains(searchTerm) ||
                    // Search in CreatedBy
                    j.CreatedBy.ToLower().Contains(searchTerm) ||
                    // Search by date
                    (isDateSearch && j.PlannedReleaseDate.Date == searchDate.Date) ||
                    // Search by year
                    (isYearSearch && j.PlannedReleaseDate.Year == searchYear) ||
                    // Search in material numbers
                    j.Materials.Any(m => m.ToLower().Contains(searchTerm))
                );
            }

            var filteredCount = await jobQuery.CountAsync();

            // Apply sorting
            var sortedQuery = sortColumn switch
            {
                0 => sortAscending ? jobQuery.OrderBy(j => j.JobId) : jobQuery.OrderByDescending(j => j.JobId),
                1 => sortAscending ? jobQuery.OrderBy(j => j.PlannedReleaseDate) : jobQuery.OrderByDescending(j => j.PlannedReleaseDate),
                2 => sortAscending ? jobQuery.OrderBy(j => j.CreatedBy) : jobQuery.OrderByDescending(j => j.CreatedBy),
                3 => sortAscending ? jobQuery.OrderBy(j => j.MaterialCount) : jobQuery.OrderByDescending(j => j.MaterialCount),
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
                    var releaseDetails = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
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
                    MaterialCount = job.MaterialCount,
                    TotalPallets = totalPallets,
                    TotalItems = totalItems,
                    JobStatus = jobStatus,
                    CompletionPercentage = Math.Round(completionPercentage, 1),
                    HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAW_MATERIAL_DELETE)
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

            var releases = await _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RawMaterial)
                .Include(r => r.GIV_RM_ReleaseDetails)
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
                var releaseDetails = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
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

            // Determine job status
            var jobStatus = GetJobStatus(releases, releases.Min(r => r.ReleaseDate));
            var completionPercentage = releases.Count > 0
                ? (decimal)completedReleases / releases.Count * 100
                : 0;

            var dto = new JobReleaseDetailsDto
            {
                JobId = jobId,
                CreatedDate = firstRelease.CreatedAt,
                CreatedBy = createdByUser?.Username ?? firstRelease.CreatedBy,
                CreatedByFullName = createdByUser?.FullName,
                StatusText = jobStatus,
                StatusClass = GetJobStatusClass(jobStatus),
                MaterialCount = releases.Count,
                TotalPallets = totalPallets,
                TotalItems = totalItems,
                CompletionPercentage = Math.Round(completionPercentage, 1),
                PlannedReleaseDate = releases.Min(r => r.ReleaseDate)
            };

            _logger.LogInformation("Returning job release details for JobId: {JobId}", jobId);
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
            _logger.LogInformation("Fetching individual releases for JobId: {JobId} with pagination Start={Start}, Length={Length}",
                jobId, start, length);

            var query = _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RawMaterial)
                .Include(r => r.GIV_RM_ReleaseDetails)
                .Where(r => r.JobId == jobId && !r.IsDeleted);

            var totalCount = await query.CountAsync();

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(r =>
                    r.GIV_RawMaterial.MaterialNo.ToLower().Contains(searchTerm) ||
                    (r.GIV_RawMaterial.Description != null && r.GIV_RawMaterial.Description.ToLower().Contains(searchTerm)) ||
                    (r.Remarks != null && r.Remarks.ToLower().Contains(searchTerm))
                );
            }

            var filteredCount = await query.CountAsync();

            // Apply sorting
            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(r => r.GIV_RawMaterial.MaterialNo) : query.OrderByDescending(r => r.GIV_RawMaterial.MaterialNo),
                1 => sortAscending ? query.OrderBy(r => r.GIV_RawMaterial.Description) : query.OrderByDescending(r => r.GIV_RawMaterial.Description),
                2 => sortAscending ? query.OrderBy(r => r.ReleaseDate) : query.OrderByDescending(r => r.ReleaseDate),
                _ => query.OrderBy(r => r.GIV_RawMaterial.MaterialNo)
            };

            // Get paginated results
            var releases = await query
                .Skip(start)
                .Take(length)
                .ToListAsync();
            var now = DateTime.UtcNow;

            // Transform to DTOs
            var individualReleaseDtos = new List<JobReleaseIndividualReleaseDto>();

            foreach (var release in releases)
            {
                var details = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();
                var palletCount = details.Count(d => d.IsEntirePallet);
                var itemCount = details.Count(d => !d.IsEntirePallet);

                var status = release.ActualReleaseDate.HasValue ? "Completed" :
                            (details.Any(d => d.ActualReleaseDate.HasValue) ? "Partially Released" :
                            (IsPastNoonOnReleaseDate(release.ReleaseDate, now) ? "Due for Release" : "Scheduled"));

                var dto = new JobReleaseIndividualReleaseDto
                {
                    ReleaseId = release.Id,
                    MaterialNo = release.GIV_RawMaterial.MaterialNo,
                    MaterialDescription = release.GIV_RawMaterial.Description ?? "",
                    ReleaseDate = release.ReleaseDate,
                    Status = status,
                    StatusClass = GetReleaseStatusClass(status),
                    PalletCount = palletCount,
                    ItemCount = itemCount,
                    IsCompleted = release.ActualReleaseDate.HasValue,
                    HasDeleteAccess = _currentUserService.HasPermission(AppConsts.Permissions.RAW_MATERIAL_DELETE)
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
        #endregion
        #region Helper methods for job status determination
        private bool IsPastNoonOnReleaseDate(DateTime releaseDate, DateTime currentTime)
        {
            var noonOnReleaseDate = releaseDate.Date.AddHours(12);

            return currentTime >= noonOnReleaseDate;
        }
        private string GetJobStatus(List<GIV_RM_Release> releases, DateTime plannedReleaseDate)
        {
            var completedCount = releases.Count(r => r.ActualReleaseDate.HasValue);
            var totalCount = releases.Count;
            var now = DateTime.UtcNow;

            var hasOverdue = releases.Any(r =>
                !r.ActualReleaseDate.HasValue &&
                IsPastNoonOnReleaseDate(r.ReleaseDate, now)
            );

            var hasPartialReleases = releases.Any(r =>
                !r.ActualReleaseDate.HasValue && 
                r.GIV_RM_ReleaseDetails.Any(d =>
                    !d.IsDeleted &&
                    d.ActualReleaseDate.HasValue 
                )
            );

            if (completedCount == totalCount)
                return "Completed";

            if (hasOverdue)
                return "Overdue";

            if (completedCount > 0 || hasPartialReleases)
                return "In Progress";

            if (plannedReleaseDate.Date <= DateTime.UtcNow.Date)
                return "Due for Release";

            return "Scheduled";
        }

        private string GetJobStatusClass(string status)
        {
            return status.ToLower() switch
            {
                "completed" => "status-completed",
                "in progress" => "status-in-progress",
                "overdue" => "status-overdue",
                "due for release" => "status-due", // Add missing case
                "scheduled" => "status-scheduled",
                _ => "status-scheduled"
            };
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
        public async Task<(byte[] fileContent, string fileName)> ExportJobReleaseToExcelAsync(Guid jobId)
        {
            _logger.LogInformation("Starting Excel export for JobId: {JobId}", jobId);

            // Get all releases for this job with all necessary data
            var releases = await _dbContext.GIV_RM_Releases
                .AsNoTracking()
                .Include(r => r.GIV_RawMaterial)
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePallet)
                        .ThenInclude(p => p.Location)
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_ReceivePalletItem)
                .Include(r => r.GIV_RM_ReleaseDetails)
                    .ThenInclude(d => d.GIV_RM_Receive)
                .Where(r => r.JobId == jobId && !r.IsDeleted)
                .ToListAsync();

            if (!releases.Any())
            {
                _logger.LogWarning("No releases found for JobId: {JobId}", jobId);
                return (Array.Empty<byte>(), string.Empty);
            }

            // Prepare export data
            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");
            var exportData = new List<JobReleaseExcelRowDto>();

            foreach (var release in releases)
            {
                var releaseDetails = release.GIV_RM_ReleaseDetails.Where(d => !d.IsDeleted).ToList();

                foreach (var detail in releaseDetails)
                {
                    var row = new JobReleaseExcelRowDto
                    {
                        ReleaseDate = release.ReleaseDate,
                        MaterialNo = release.GIV_RawMaterial.MaterialNo,
                        MHU = detail.GIV_RM_ReceivePallet?.PalletCode ?? "", // Pallet Code as MHU
                        HU = detail.GIV_RM_ReceivePalletItem?.ItemCode ?? "", // Item Code as HU
                        Batch = detail.GIV_RM_Receive?.BatchNo ?? "",
                        Location = detail.GIV_RM_ReceivePallet?.Location?.Code ?? ""
                    };

                    exportData.Add(row);
                }
            }

            // Generate Excel file
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Job Release Details");

            // Set headers
            worksheet.Cells[1, 1].Value = "Release Date";
            worksheet.Cells[1, 2].Value = "Material No";
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
                worksheet.Cells[rowIndex, 2].Value = row.MaterialNo;
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
            var fileName = $"JobRelease_{jobId.ToString().Substring(0, 8)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation("Excel export completed for JobId: {JobId}, {RowCount} rows exported", jobId, exportData.Count);

            return (package.GetAsByteArray(), fileName);
        }
        #region Create Job Release load
        public async Task<List<MaterialForJobReleaseDto>> GetAvailableMaterialsForJobReleaseAsync()
        {
            _logger.LogInformation("Fetching available materials for job release");

            var materials = await _dbContext.GIV_RawMaterials
                .AsNoTracking()
                .Include(m => m.RM_Receive)
                    .ThenInclude(r => r.RM_ReceivePallets)
                        .ThenInclude(p => p.RM_ReceivePalletItems)
                .Where(m => !m.IsDeleted)
                .ToListAsync();

            var materialDtos = new List<MaterialForJobReleaseDto>();

            foreach (var material in materials)
            {
                // Calculate balance quantities
                var allItems = material.RM_Receive
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .SelectMany(p => p.RM_ReceivePalletItems)
                    .Where(i => !i.IsDeleted);

                var balanceQty = allItems.Count(i => !i.IsReleased);
                var balancePallets = material.RM_Receive
                    .Where(r => !r.IsDeleted)
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Where(p => !p.IsDeleted)
                    .Count(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased));

                var totalReceives = material.RM_Receive.Count(r => !r.IsDeleted);

                // Only include materials that have available inventory
                if (balanceQty > 0)
                {
                    materialDtos.Add(new MaterialForJobReleaseDto
                    {
                        Id = material.Id,
                        MaterialNo = material.MaterialNo,
                        Description = material.Description,
                        BalanceQty = balanceQty,
                        BalancePallets = balancePallets,
                        TotalReceives = totalReceives
                    });
                }
            }

            _logger.LogInformation("Found {Count} materials with available inventory", materialDtos.Count);
            return materialDtos.OrderBy(m => m.MaterialNo).ToList();
        }

        public async Task<List<JobReleaseInventoryDto>> GetMaterialInventoryForJobReleaseAsync(List<Guid> materialIds)
        {
            _logger.LogInformation("Fetching inventory for {Count} materials", materialIds.Count);

            var materials = await _dbContext.GIV_RawMaterials
                .AsNoTracking()
                .Include(m => m.RM_Receive.Where(r => !r.IsDeleted))
                    .ThenInclude(r => r.RM_ReceivePallets.Where(p => !p.IsDeleted))
                        .ThenInclude(p => p.RM_ReceivePalletItems.Where(i => !i.IsDeleted))
                .Where(m => materialIds.Contains(m.Id) && !m.IsDeleted)
                .ToListAsync();

            var inventoryDtos = new List<JobReleaseInventoryDto>();

            foreach (var material in materials)
            {
                var materialDto = new JobReleaseInventoryDto
                {
                    Id = material.Id,
                    MaterialNo = material.MaterialNo,
                    Description = material.Description
                };

                // Calculate totals
                var allItems = material.RM_Receive
                    .SelectMany(r => r.RM_ReceivePallets)
                    .SelectMany(p => p.RM_ReceivePalletItems);

                materialDto.TotalBalanceQty = allItems.Count(i => !i.IsReleased);
                materialDto.TotalBalancePallet = material.RM_Receive
                    .SelectMany(r => r.RM_ReceivePallets)
                    .Count(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased));

                // Process receives
                foreach (var receive in material.RM_Receive.OrderByDescending(r => r.ReceivedDate))
                {
                    // Only include receives that have available inventory
                    var hasAvailableItems = receive.RM_ReceivePallets
                        .Any(p => p.RM_ReceivePalletItems.Any(i => !i.IsReleased));

                    if (!hasAvailableItems) continue;

                    // Get batch number from the first available item instead of receive (Req by sooshin 25/07/2025)
                    var firstAvailableItem = receive.RM_ReceivePallets
                        .SelectMany(p => p.RM_ReceivePalletItems)
                        .Where(i => !i.IsReleased)
                        .FirstOrDefault();

                    var receiveDto = new JobReleaseReceiveDto
                    {
                        Id = receive.Id,
                        BatchNo = firstAvailableItem?.BatchNo,
                        ReceivedDate = receive.ReceivedDate,
                        ReceivedBy = receive.ReceivedBy
                    };

                    // Process pallets
                    foreach (var pallet in receive.RM_ReceivePallets.OrderBy(p => p.PalletCode))
                    {
                        var palletDto = new JobReleasePalletDto
                        {
                            Id = pallet.Id,
                            PalletCode = pallet.PalletCode,
                            IsReleased = pallet.IsReleased,
                            HandledBy = pallet.HandledBy
                        };

                        // Process items
                        foreach (var item in pallet.RM_ReceivePalletItems.OrderBy(i => i.ItemCode))
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

                    materialDto.Receives.Add(receiveDto);
                }

                inventoryDtos.Add(materialDto);
            }

            _logger.LogInformation("Prepared inventory data for {Count} materials", inventoryDtos.Count);
            return inventoryDtos.OrderBy(m => m.MaterialNo).ToList();
        }
        #endregion
        #region Create Job Release Submit
        public async Task<ServiceWebResult> CreateJobReleaseAsync(JobReleaseCreateDto dto, string userId)
        {
            _logger.LogInformation("Creating job release for user {UserId} with {MaterialCount} materials",
                userId, dto.Materials.Count);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var materialDto in dto.Materials)
                {
                    foreach (var receiveDto in materialDto.Receives)
                    {
                        // Deduplicate pallets within this receive
                        receiveDto.Pallets = receiveDto.Pallets
                            .GroupBy(p => p.PalletId)
                            .Select(g => g.First())
                            .ToList();

                        // Deduplicate items within this receive
                        receiveDto.Items = receiveDto.Items
                            .GroupBy(i => i.ItemId)
                            .Select(g => g.First())
                            .ToList();
                    }
                }
                // Generate a unique JobId for this release
                var jobId = Guid.NewGuid();
                var allReleases = new List<GIV_RM_Release>();

                // Pre-load all pallet and item data to avoid multiple queries
                var allPalletIds = dto.Materials
                    .SelectMany(m => m.Receives)
                    .SelectMany(r => r.Pallets)
                    .Select(p => p.PalletId)
                    .Distinct()
                    .ToList();

                var allItemIds = dto.Materials
                    .SelectMany(m => m.Receives)
                    .SelectMany(r => r.Items)
                    .Select(i => i.ItemId)
                    .Distinct()
                    .ToList();

                // Load pallets with their items in one query
                var palletLookup = await _dbContext.GIV_RM_ReceivePallets
                    .Include(p => p.RM_ReceivePalletItems)
                    .Where(p => allPalletIds.Contains(p.Id) && !p.IsDeleted)
                    .ToDictionaryAsync(p => p.Id, p => p);

                // Load individual items in one query
                var itemLookup = await _dbContext.GIV_RM_ReceivePalletItems
                    .Include(i => i.GIV_RM_ReceivePallet)
                    .Where(i => allItemIds.Contains(i.Id) && !i.IsDeleted)
                    .ToDictionaryAsync(i => i.Id, i => i);

                foreach (var materialDto in dto.Materials)
                {
                    foreach (var receiveDto in materialDto.Receives)
                    {
                        _logger.LogInformation("Processing receive {ReceiveId} with {PalletCount} pallets and {ItemCount} items",
                            receiveDto.ReceiveId, receiveDto.Pallets.Count, receiveDto.Items.Count);

                        // Create a release record for each receive
                        var release = new GIV_RM_Release
                        {
                            Id = Guid.NewGuid(),
                            JobId = jobId, // Link all releases with the same JobId
                            GIV_RawMaterialId = materialDto.MaterialId,
                            ReleaseDate = DateTime.SpecifyKind(receiveDto.ReleaseDate, DateTimeKind.Utc),
                            ReleasedBy = userId,
                            Remarks = dto.JobRemarks,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = userId,
                            IsDeleted = false,
                            WarehouseId = _currentUserService.CurrentWarehouseId
                        };

                        var releaseDetails = new List<GIV_RM_ReleaseDetails>();

                        // Process pallet releases (entire pallets)
                        foreach (var palletDto in receiveDto.Pallets)
                        {
                            _logger.LogInformation("Processing pallet {PalletId} - {PalletCode}",
                                palletDto.PalletId, palletDto.PalletCode);

                            if (!palletLookup.TryGetValue(palletDto.PalletId, out var pallet))
                            {
                                _logger.LogWarning("Pallet not found: {PalletId}", palletDto.PalletId);
                                continue;
                            }

                            _logger.LogInformation("Pallet {PalletCode} has {ItemCount} items - creating ONE release detail for entire pallet",
                                pallet.PalletCode, pallet.RM_ReceivePalletItems.Count(i => !i.IsDeleted));

                            // Create ONLY ONE release detail record for the entire pallet
                            // Use the first item as the representative item for the pallet release
                            var firstItem = pallet.RM_ReceivePalletItems.Where(i => !i.IsDeleted).FirstOrDefault();

                            if (firstItem != null)
                            {
                                releaseDetails.Add(new GIV_RM_ReleaseDetails
                                {
                                    Id = Guid.NewGuid(),
                                    GIV_RM_ReleaseId = release.Id,
                                    GIV_RM_ReceiveId = receiveDto.ReceiveId,
                                    GIV_RM_ReceivePalletId = pallet.Id,
                                    GIV_RM_ReceivePalletItemId = null,// changed to null to avoid itemid duplication 
                                    IsEntirePallet = true, // This indicates the entire pallet is being released
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId,
                                    IsDeleted = false,
                                    WarehouseId = _currentUserService.CurrentWarehouseId
                                });

                                _logger.LogInformation("Created ONE release detail for entire pallet {PalletCode}", pallet.PalletCode);
                            }
                            else
                            {
                                _logger.LogWarning("Pallet {PalletCode} has no valid items", pallet.PalletCode);
                            }
                        }

                        // Get selected pallet IDs for this receive to avoid double-processing
                        var selectedPalletIds = receiveDto.Pallets.Select(p => p.PalletId).ToHashSet();

                        // Process individual item releases (not part of entire pallet releases)
                        foreach (var itemDto in receiveDto.Items)
                        {
                            _logger.LogInformation("Processing individual item {ItemId} - {ItemCode}",
                                itemDto.ItemId, itemDto.ItemCode);

                            if (!itemLookup.TryGetValue(itemDto.ItemId, out var item))
                            {
                                _logger.LogWarning("Item not found: {ItemId}", itemDto.ItemId);
                                continue;
                            }

                            // Only add if this item's pallet is not being released as a whole
                            if (!selectedPalletIds.Contains(item.GIV_RM_ReceivePalletId))
                            {
                                releaseDetails.Add(new GIV_RM_ReleaseDetails
                                {
                                    Id = Guid.NewGuid(),
                                    GIV_RM_ReleaseId = release.Id,
                                    GIV_RM_ReceiveId = receiveDto.ReceiveId,
                                    GIV_RM_ReceivePalletId = item.GIV_RM_ReceivePalletId,
                                    GIV_RM_ReceivePalletItemId = item.Id,
                                    IsEntirePallet = false, // Mark as individual item release
                                    CreatedAt = DateTime.UtcNow,
                                    CreatedBy = userId,
                                    IsDeleted = false,
                                    WarehouseId = _currentUserService.CurrentWarehouseId
                                });
                            }
                            else
                            {
                                _logger.LogInformation("Skipping item {ItemCode} as its pallet is being released entirely",
                                    item.ItemCode);
                            }
                        }

                        _logger.LogInformation("Created {DetailCount} release details for receive {ReceiveId}",
                            releaseDetails.Count, receiveDto.ReceiveId);

                        // Only add the release if it has details
                        if (releaseDetails.Any())
                        {
                            release.GIV_RM_ReleaseDetails = releaseDetails;
                            allReleases.Add(release);
                        }
                    }
                }

                if (!allReleases.Any())
                {
                    _logger.LogWarning("No valid releases created for job");
                    return new ServiceWebResult { Success = false, ErrorMessage = "No valid items selected for release." };
                }

                _logger.LogInformation("Saving {ReleaseCount} releases with total {DetailCount} details",
                    allReleases.Count, allReleases.Sum(r => r.GIV_RM_ReleaseDetails.Count));

                // Save all releases
                _dbContext.GIV_RM_Releases.AddRange(allReleases);
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Job release created successfully: JobId={JobId}, {ReleaseCount} releases created",
                    jobId, allReleases.Count);

                return new ServiceWebResult { Success = true };
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
            {
                await transaction.RollbackAsync();
                _logger.LogWarning("Unique constraint violation in job release creation: {Error}",
                    dbEx.InnerException?.Message);

                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = "Duplicate items detected. Some items may already be scheduled for release. Please refresh the page and try again."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating job release for user {UserId}", userId);
                return new ServiceWebResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        private static bool IsUniqueConstraintViolation(Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
        {
            var message = dbEx.InnerException?.Message ?? "";
            return message.Contains("UK_ReleaseDetails_UniqueItemPerRelease") ||
                   message.Contains("UK_ReleaseDetails_UniquePalletPerRelease") ||
                   message.Contains("UK_ReleaseDetails_PreventMixedTypes") ||
                   message.Contains("duplicate key") ||
                   message.Contains("UNIQUE constraint failed");
        }
        public async Task<List<GroupedPalletCountDto>> GetGroupedPalletCount(DateTime cutoffDate)
        {
            try
            {
                //get all received pallet  from monday to sunday (sunday is cutoffdate)
                _logger.LogDebug("RawMaterial - GetGroupedPalletCount");

                DateTime startDate = cutoffDate.AddDays(-6).Date;
                var rmPallets = await _dbContext.GIV_RM_ReceivePallets
                        .Where(rp => !rp.IsDeleted && !rp.IsReleased)
                        .Where(rp => !rp.GIV_RM_Receive.IsDeleted)
                        .Where(rp => rp.GIV_RM_Receive.ReceivedDate >= startDate && rp.GIV_RM_Receive.ReceivedDate <= cutoffDate)
                        .Where(rp => !rp.GIV_RM_Receive.RawMaterial.IsDeleted)
                            .Select(rp => new
                            {
                                NDG = rp.GIV_RM_Receive.RawMaterial.NDG,
                                Group9 = rp.GIV_RM_Receive.RawMaterial.Group9,
                                Group3 = rp.GIV_RM_Receive.RawMaterial.Group3,
                                Group6 = rp.GIV_RM_Receive.RawMaterial.Group6,
                                Group8 = rp.GIV_RM_Receive.RawMaterial.Group8,
                                Group4_1 = rp.GIV_RM_Receive.RawMaterial.Group4_1,
                                Scentaurus = rp.GIV_RM_Receive.RawMaterial.Scentaurus,
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

                foreach(var rmPallet in rmPallets)
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
                _logger.LogError("Error RawMaterial - GetGroupedPalletCount");
                throw;
            }

        }
        public async Task<RMServiceWebResult> ValidateJobReleaseConflictsAsync(JobReleaseCreateDto dto)
        {
            _logger.LogInformation("Validating job release conflicts for {MaterialCount} materials", dto.Materials.Count);

            try
            {
                var conflicts = new List<JobReleaseConflictDto>();

                foreach (var material in dto.Materials)
                {
                    foreach (var receive in material.Receives)
                    {
                        // Validate entire pallets
                        foreach (var pallet in receive.Pallets)
                        {
                            var palletConflicts = await ValidatePalletConflicts(
                                material.MaterialId,
                                receive.ReceiveId,
                                pallet.PalletId,
                                true // isEntirePallet
                            );

                            conflicts.AddRange(palletConflicts);
                        }

                        // Validate individual items
                        foreach (var item in receive.Items)
                        {
                            var itemConflicts = await ValidateItemConflicts(
                                material.MaterialId,
                                receive.ReceiveId,
                                item.ItemId
                            );

                            conflicts.AddRange(itemConflicts);
                        }
                    }
                }

                if (conflicts.Any())
                {
                    return new RMServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = $"Found {conflicts.Count} conflicts. Please resolve before proceeding.",
                        ValidationDetails = conflicts
                    };
                }

                return new RMServiceWebResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating job release conflicts");
                return new RMServiceWebResult { Success = false, ErrorMessage = "Validation failed due to an error" };
            }
        }

        private async Task<List<JobReleaseConflictDto>> ValidatePalletConflicts(
    Guid materialId,
    Guid receiveId,
    Guid palletId,
    bool isEntirePallet)
        {
            var conflicts = new List<JobReleaseConflictDto>();

            // OPTION A: Strict validation - check ALL existing releases (recommended)
            // Check for existing entire pallet releases (including completed ones)
            var existingEntirePalletReleases = await _dbContext.GIV_RM_ReleaseDetails
                .Include(rd => rd.GIV_RM_Release)
                    .ThenInclude(r => r.GIV_RawMaterial)
                .Include(rd => rd.GIV_RM_ReceivePallet)
                .Where(rd => !rd.IsDeleted
                    // REMOVED: && rd.GIV_RM_Release.ActualReleaseDate == null  
                    // REMOVED: && rd.ActualReleaseDate == null                  
                    && rd.IsEntirePallet == true
                    && rd.GIV_RM_ReceivePalletId == palletId
                    && rd.GIV_RM_ReceiveId == receiveId)
                .ToListAsync();

            foreach (var existingRelease in existingEntirePalletReleases)
            {
                string conflictType = existingRelease.ActualReleaseDate.HasValue || existingRelease.GIV_RM_Release.ActualReleaseDate.HasValue
                    ? "EntirePalletAlreadyReleased"  // Actually released
                    : "EntirePalletAlreadyScheduled"; // Just scheduled

                conflicts.Add(new JobReleaseConflictDto
                {
                    MaterialNo = existingRelease.GIV_RM_Release.GIV_RawMaterial.MaterialNo,
                    ReceiveId = receiveId,
                    PalletCode = existingRelease.GIV_RM_ReceivePallet?.PalletCode,
                    ConflictType = conflictType,
                    ExistingJobId = existingRelease.GIV_RM_Release.JobId,
                    ConflictingItems = new List<string>()
                });
            }

            // If requesting entire pallet, check for individual item conflicts (including completed ones)
            if (isEntirePallet)
            {
                var existingItemReleases = await _dbContext.GIV_RM_ReleaseDetails
                    .Include(rd => rd.GIV_RM_Release)
                        .ThenInclude(r => r.GIV_RawMaterial)
                    .Include(rd => rd.GIV_RM_ReceivePallet)
                    .Include(rd => rd.GIV_RM_ReceivePalletItem)
                    .Where(rd => !rd.IsDeleted
                        // REMOVED: && rd.GIV_RM_Release.ActualReleaseDate == null
                        // REMOVED: && rd.ActualReleaseDate == null
                        && rd.IsEntirePallet == false
                        && rd.GIV_RM_ReceivePalletId == palletId
                        && rd.GIV_RM_ReceiveId == receiveId)
                    .ToListAsync();

                if (existingItemReleases.Any())
                {
                    string conflictType = existingItemReleases.Any(r => r.ActualReleaseDate.HasValue || r.GIV_RM_Release.ActualReleaseDate.HasValue)
                        ? "IndividualItemsAlreadyReleased"   // Some actually released
                        : "IndividualItemsAlreadyScheduled"; // Just scheduled

                    conflicts.Add(new JobReleaseConflictDto
                    {
                        MaterialNo = existingItemReleases.First().GIV_RM_Release.GIV_RawMaterial.MaterialNo,
                        ReceiveId = receiveId,
                        PalletCode = existingItemReleases.First().GIV_RM_ReceivePallet?.PalletCode,
                        ConflictType = conflictType,
                        ExistingJobId = existingItemReleases.First().GIV_RM_Release.JobId,
                        ConflictingItems = existingItemReleases
                            .Select(r => r.GIV_RM_ReceivePalletItem?.ItemCode ?? "Unknown")
                            .ToList()
                    });
                }
            }

            return conflicts;
        }

        private async Task<List<JobReleaseConflictDto>> ValidateItemConflicts(
            Guid materialId,
            Guid receiveId,
            Guid itemId)
        {
            var conflicts = new List<JobReleaseConflictDto>();

            // Get the item to find its pallet
            var item = await _dbContext.GIV_RM_ReceivePalletItems
                .Include(i => i.GIV_RM_ReceivePallet)
                    .ThenInclude(p => p.GIV_RM_Receive)
                        .ThenInclude(r => r.RawMaterial)
                .FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted);

            if (item == null)
            {
                return conflicts;
            }

            var palletId = item.GIV_RM_ReceivePalletId;

            // Check if entire pallet is already scheduled (including completed)
            var entirePalletScheduled = await _dbContext.GIV_RM_ReleaseDetails
                .Include(rd => rd.GIV_RM_Release)
                .AnyAsync(rd => !rd.IsDeleted
                    && rd.IsEntirePallet == true
                    && rd.GIV_RM_ReceivePalletId == palletId
                    && rd.GIV_RM_ReceiveId == receiveId);

            if (entirePalletScheduled)
            {
                conflicts.Add(new JobReleaseConflictDto
                {
                    MaterialNo = item.GIV_RM_ReceivePallet.GIV_RM_Receive.RawMaterial.MaterialNo,
                    ReceiveId = receiveId,
                    PalletCode = item.GIV_RM_ReceivePallet?.PalletCode,
                    ConflictType = "EntirePalletAlreadyScheduled",
                    ExistingJobId = null,
                    ConflictingItems = new List<string>()
                });
            }

            // Check for duplicate individual item (including completed)
            var existingItemRelease = await _dbContext.GIV_RM_ReleaseDetails
                .Include(rd => rd.GIV_RM_Release)
                    .ThenInclude(r => r.GIV_RawMaterial)
                .Include(rd => rd.GIV_RM_ReceivePalletItem)
                .FirstOrDefaultAsync(rd => !rd.IsDeleted
                    && rd.IsEntirePallet == false
                    && rd.GIV_RM_ReceivePalletItemId == itemId);

            if (existingItemRelease != null)
            {
                string conflictType = existingItemRelease.ActualReleaseDate.HasValue || existingItemRelease.GIV_RM_Release.ActualReleaseDate.HasValue
                    ? "IndividualItemAlreadyReleased"   // Actually released
                    : "IndividualItemAlreadyScheduled"; // Just scheduled

                conflicts.Add(new JobReleaseConflictDto
                {
                    MaterialNo = existingItemRelease.GIV_RM_Release.GIV_RawMaterial.MaterialNo,
                    ReceiveId = receiveId,
                    PalletCode = existingItemRelease.GIV_RM_ReceivePalletItem?.GIV_RM_ReceivePallet?.PalletCode,
                    ConflictType = conflictType,
                    ExistingJobId = existingItemRelease.GIV_RM_Release.JobId,
                    ConflictingItems = new List<string> { existingItemRelease.GIV_RM_ReceivePalletItem?.ItemCode ?? "Unknown" }
                });
            }

            return conflicts;
        }
        public async Task<MaterialConflictResponse> GetMaterialReleaseConflictsAsync(Guid materialId)
        {
            try
            {
                var response = new MaterialConflictResponse
                {
                    Pallets = new Dictionary<Guid, ConflictInfo>(),
                    Items = new Dictionary<Guid, ConflictInfo>()
                };

                // Get all scheduled releases for this material that haven't been actually released yet
                var scheduledReleases = await _dbContext.GIV_RM_ReleaseDetails
                    .Include(rd => rd.GIV_RM_Release)
                    .Include(rd => rd.GIV_RM_ReceivePallet)
                    .Include(rd => rd.GIV_RM_ReceivePalletItem)
                    .Where(rd => !rd.IsDeleted
                        && rd.GIV_RM_Release.GIV_RawMaterialId == materialId
                        && rd.GIV_RM_Release.ActualReleaseDate == null  // Job not actually released
                        && rd.ActualReleaseDate == null)                 // Detail not actually released
                    .ToListAsync();

                // Process entire pallet conflicts
                var entirePalletReleases = scheduledReleases
                    .Where(rd => rd.IsEntirePallet && rd.GIV_RM_ReceivePalletId.HasValue)
                    .ToList();

                foreach (var release in entirePalletReleases)
                {
                    var palletId = release.GIV_RM_ReceivePalletId.Value;

                    response.Pallets[palletId] = new ConflictInfo
                    {
                        Type = "EntirePalletScheduled",
                        JobId = release.GIV_RM_Release.JobId,
                        PalletCode = release.GIV_RM_ReceivePallet?.PalletCode
                    };

                    // Also mark all items in this pallet as conflicted
                    var palletItems = await _dbContext.GIV_RM_ReceivePalletItems
                        .Where(item => item.GIV_RM_ReceivePalletId == palletId && !item.IsDeleted)
                        .ToListAsync();

                    foreach (var item in palletItems)
                    {
                        response.Items[item.Id] = new ConflictInfo
                        {
                            Type = "ParentPalletScheduled",
                            JobId = release.GIV_RM_Release.JobId,
                            ItemCode = item.ItemCode,
                            PalletCode = release.GIV_RM_ReceivePallet?.PalletCode
                        };
                    }
                }

                // Process individual item conflicts
                var individualItemReleases = scheduledReleases
                    .Where(rd => !rd.IsEntirePallet && rd.GIV_RM_ReceivePalletItemId.HasValue)
                    .ToList();

                foreach (var release in individualItemReleases)
                {
                    var itemId = release.GIV_RM_ReceivePalletItemId.Value;

                    // Only add if not already marked as parent pallet scheduled
                    if (!response.Items.ContainsKey(itemId))
                    {
                        response.Items[itemId] = new ConflictInfo
                        {
                            Type = "ItemScheduled",
                            JobId = release.GIV_RM_Release.JobId,
                            ItemCode = release.GIV_RM_ReceivePalletItem?.ItemCode,
                            PalletCode = release.GIV_RM_ReceivePalletItem?.GIV_RM_ReceivePallet?.PalletCode
                        };
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting material release conflicts for material {MaterialId}", materialId);
                throw;
            }
        }
        // Add to RawMaterialService.cs

        public async Task<Dictionary<Guid, MaterialConflictResponse>> GetBatchMaterialReleaseConflictsAsync(List<Guid> materialIds)
        {
            try
            {
                _logger.LogInformation("Checking conflicts for {Count} materials in batch", materialIds.Count);

                // Single optimized query to get all scheduled releases for all materials
                var scheduledReleases = await _dbContext.GIV_RM_ReleaseDetails
                    .AsNoTracking()
                    .Where(rd => !rd.IsDeleted
                        && materialIds.Contains(rd.GIV_RM_Release.GIV_RawMaterialId)
                        && rd.GIV_RM_Release.ActualReleaseDate == null  // Job not actually released
                        && rd.ActualReleaseDate == null)                 // Detail not actually released
                    .Select(rd => new ScheduledReleaseConflictDto
                    {
                        MaterialId = rd.GIV_RM_Release.GIV_RawMaterialId,
                        JobId = rd.GIV_RM_Release.JobId,
                        IsEntirePallet = rd.IsEntirePallet,
                        PalletId = rd.GIV_RM_ReceivePalletId,
                        ItemId = rd.GIV_RM_ReceivePalletItemId,
                        PalletCode = rd.GIV_RM_ReceivePallet != null ? rd.GIV_RM_ReceivePallet.PalletCode : null,
                        ItemCode = rd.GIV_RM_ReceivePalletItem != null ? rd.GIV_RM_ReceivePalletItem.ItemCode : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} scheduled release details across all materials", scheduledReleases.Count);

                // Get all items for pallets that have entire pallet conflicts (for marking individual items as conflicted)
                var entirePalletIds = scheduledReleases
                    .Where(sr => sr.IsEntirePallet && sr.PalletId.HasValue)
                    .Select(sr => sr.PalletId.Value)
                    .Distinct()
                    .ToList();

                var palletItems = new Dictionary<Guid, List<Guid>>();
                if (entirePalletIds.Any())
                {
                    var itemsInConflictedPallets = await _dbContext.GIV_RM_ReceivePalletItems
                        .AsNoTracking()
                        .Where(item => entirePalletIds.Contains(item.GIV_RM_ReceivePalletId) && !item.IsDeleted)
                        .Select(item => new { PalletId = item.GIV_RM_ReceivePalletId, ItemId = item.Id })
                        .ToListAsync();

                    palletItems = itemsInConflictedPallets
                        .GroupBy(x => x.PalletId)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.ItemId).ToList());
                }

                // Process conflicts in memory for each material
                var result = new Dictionary<Guid, MaterialConflictResponse>();

                foreach (var materialId in materialIds)
                {
                    var materialConflicts = scheduledReleases.Where(sr => sr.MaterialId == materialId).ToList();
                    result[materialId] = ProcessMaterialConflictsInMemory(materialConflicts, palletItems);
                }

                _logger.LogInformation("Processed conflicts for {Count} materials", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting batch material release conflicts for materials: {MaterialIds}",
                    string.Join(", ", materialIds));
                throw;
            }
        }

        private MaterialConflictResponse ProcessMaterialConflictsInMemory(
    List<ScheduledReleaseConflictDto> materialConflicts,
    Dictionary<Guid, List<Guid>> palletItems)
        {
            var response = new MaterialConflictResponse
            {
                Pallets = new Dictionary<Guid, ConflictInfo>(),
                Items = new Dictionary<Guid, ConflictInfo>()
            };

            // Process entire pallet conflicts
            var entirePalletConflicts = materialConflicts.Where(mc => mc.IsEntirePallet && mc.PalletId.HasValue);
            foreach (var conflict in entirePalletConflicts)
            {
                var palletId = conflict.PalletId.Value;
                // Add pallet conflict
                response.Pallets[palletId] = new ConflictInfo
                {
                    Type = "EntirePalletScheduled",
                    JobId = conflict.JobId,
                    PalletCode = conflict.PalletCode
                };
                // Mark all items in this pallet as conflicted
                if (palletItems.ContainsKey(palletId))
                {
                    foreach (var itemId in palletItems[palletId])
                    {
                        response.Items[itemId] = new ConflictInfo
                        {
                            Type = "ParentPalletScheduled",
                            JobId = conflict.JobId,
                            ItemCode = null, // We don't have item code in this context
                            PalletCode = conflict.PalletCode
                        };
                    }
                }
            }

            // Process individual item conflicts
            var individualItemConflicts = materialConflicts.Where(mc => !mc.IsEntirePallet && mc.ItemId.HasValue);
            foreach (var conflict in individualItemConflicts)
            {
                var itemId = conflict.ItemId.Value;
                // Only add if not already marked as parent pallet scheduled
                if (!response.Items.ContainsKey(itemId))
                {
                    response.Items[itemId] = new ConflictInfo
                    {
                        Type = "ItemScheduled",
                        JobId = conflict.JobId,
                        ItemCode = conflict.ItemCode,
                        PalletCode = conflict.PalletCode
                    };
                }
            }

            return response;
        }
        #endregion

        #region Job Release Delete
        public async Task<ServiceWebResult> DeleteReleaseDetailAsync(Guid releaseDetailId, string userId)
        {
            _logger.LogInformation("Starting delete operation for ReleaseDetail {ReleaseDetailId} by user {UserId}",
                releaseDetailId, userId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Get release detail with row-level locking and include related entities
                var releaseDetail = await _dbContext.GIV_RM_ReleaseDetails
                    .Include(rd => rd.GIV_RM_ReceivePallet)
                    .Include(rd => rd.GIV_RM_ReceivePalletItem)
                    .Include(rd => rd.GIV_RM_Release)
                        .ThenInclude(r => r.GIV_RawMaterial)
                    .Where(rd => rd.Id == releaseDetailId && !rd.IsDeleted)
                    .FirstOrDefaultAsync();

                if (releaseDetail == null)
                {
                    _logger.LogWarning("ReleaseDetail {ReleaseDetailId} not found or already deleted", releaseDetailId);
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = "Release detail not found or already deleted."
                    };
                }

                // Validate deletion criteria with AND logic
                var validationResult = await ValidateReleaseDetailDeletionAsync(releaseDetail);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("ReleaseDetail {ReleaseDetailId} deletion validation failed: {Error}",
                        releaseDetailId, validationResult.ErrorMessage);
                    return validationResult;
                }

                // Perform soft delete
                releaseDetail.IsDeleted = true;
                releaseDetail.ModifiedBy = userId;
                releaseDetail.ModifiedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully deleted ReleaseDetail {ReleaseDetailId} by user {UserId}",
                    releaseDetailId, userId);

                return new ServiceWebResult
                {
                    Success = true,
                    ErrorMessage = $"Release detail for {GetReleaseDetailDisplayName(releaseDetail)} deleted successfully."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting ReleaseDetail {ReleaseDetailId} by user {UserId}",
                    releaseDetailId, userId);
                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the release detail."
                };
            }
        }

        public async Task<ServiceWebResult> DeleteReleaseAsync(Guid releaseId, string userId)
        {
            _logger.LogInformation("Starting delete operation for Release {ReleaseId} by user {UserId}",
                releaseId, userId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Get release with all related data and row-level locking
                var release = await _dbContext.GIV_RM_Releases
                    .Include(r => r.GIV_RawMaterial)
                    .Include(r => r.GIV_RM_ReleaseDetails.Where(rd => !rd.IsDeleted))
                        .ThenInclude(rd => rd.GIV_RM_ReceivePallet)
                    .Include(r => r.GIV_RM_ReleaseDetails.Where(rd => !rd.IsDeleted))
                        .ThenInclude(rd => rd.GIV_RM_ReceivePalletItem)
                    .Where(r => r.Id == releaseId && !r.IsDeleted)
                    .FirstOrDefaultAsync();

                if (release == null)
                {
                    _logger.LogWarning("Release {ReleaseId} not found or already deleted", releaseId);
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = "Release not found or already deleted."
                    };
                }

                // Validate deletion criteria for the release and all its details
                var validationResult = await ValidateReleaseDeletionAsync(release);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("Release {ReleaseId} deletion validation failed: {Error}",
                        releaseId, validationResult.ErrorMessage);
                    return validationResult;
                }

                // Perform soft delete on release and all its details
                release.IsDeleted = true;
                release.ModifiedBy = userId;
                release.ModifiedAt = DateTime.UtcNow;

                foreach (var detail in release.GIV_RM_ReleaseDetails)
                {
                    detail.IsDeleted = true;
                    detail.ModifiedBy = userId;
                    detail.ModifiedAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully deleted Release {ReleaseId} with {DetailCount} details by user {UserId}",
                    releaseId, release.GIV_RM_ReleaseDetails.Count, userId);

                return new ServiceWebResult
                {
                    Success = true,
                    ErrorMessage = $"Release for {release.GIV_RawMaterial.MaterialNo} deleted successfully with {release.GIV_RM_ReleaseDetails.Count} details."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting Release {ReleaseId} by user {UserId}", releaseId, userId);
                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the release."
                };
            }
        }

        public async Task<ServiceWebResult> DeleteJobReleasesAsync(Guid jobId, string userId)
        {
            _logger.LogInformation("Starting delete operation for Job {JobId} by user {UserId}", jobId, userId);

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // Get all releases in the job with related data and row-level locking
                var releases = await _dbContext.GIV_RM_Releases
                    .Include(r => r.GIV_RawMaterial)
                    .Include(r => r.GIV_RM_ReleaseDetails.Where(rd => !rd.IsDeleted))
                        .ThenInclude(rd => rd.GIV_RM_ReceivePallet)
                    .Include(r => r.GIV_RM_ReleaseDetails.Where(rd => !rd.IsDeleted))
                        .ThenInclude(rd => rd.GIV_RM_ReceivePalletItem)
                    .Where(r => r.JobId == jobId && !r.IsDeleted)
                    .ToListAsync();

                if (!releases.Any())
                {
                    _logger.LogWarning("No releases found for Job {JobId}", jobId);
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = "Job not found or no releases to delete."
                    };
                }

                // Validate deletion criteria for all releases in the job
                var validationResult = await ValidateJobDeletionAsync(releases);
                if (!validationResult.Success)
                {
                    _logger.LogWarning("Job {JobId} deletion validation failed: {Error}", jobId, validationResult.ErrorMessage);
                    return validationResult;
                }

                // Perform soft delete on all releases and their details
                var totalDetails = 0;
                foreach (var release in releases)
                {
                    release.IsDeleted = true;
                    release.ModifiedBy = userId;
                    release.ModifiedAt = DateTime.UtcNow;

                    foreach (var detail in release.GIV_RM_ReleaseDetails)
                    {
                        detail.IsDeleted = true;
                        detail.ModifiedBy = userId;
                        detail.ModifiedAt = DateTime.UtcNow;
                        totalDetails++;
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Successfully deleted Job {JobId} with {ReleaseCount} releases and {DetailCount} details by user {UserId}",
                    jobId, releases.Count, totalDetails, userId);

                return new ServiceWebResult
                {
                    Success = true,
                    ErrorMessage = $"Job deleted successfully. Removed {releases.Count} releases with {totalDetails} details."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting Job {JobId} by user {UserId}", jobId, userId);
                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the job releases."
                };
            }
        }

        #endregion

        #region Private Validation Methods

        private async Task<ServiceWebResult> ValidateReleaseDetailDeletionAsync(GIV_RM_ReleaseDetails releaseDetail)
        {
            // Check if ActualReleaseDate is null (AND condition 1)
            if (releaseDetail.ActualReleaseDate.HasValue)
            {
                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot delete {GetReleaseDetailDisplayName(releaseDetail)} - it has already been actually released on {releaseDetail.ActualReleaseDate.Value:yyyy-MM-dd HH:mm}."
                };
            }

            // Check pallet/item IsReleased status (AND condition 2)
            if (releaseDetail.IsEntirePallet)
            {
                // For entire pallet releases, check pallet IsReleased status
                if (releaseDetail.GIV_RM_ReceivePallet?.IsReleased == true)
                {
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete pallet release - Pallet {releaseDetail.GIV_RM_ReceivePallet.PalletCode} is already marked as released."
                    };
                }
            }
            else
            {
                // For individual item releases, check item IsReleased status
                if (releaseDetail.GIV_RM_ReceivePalletItem?.IsReleased == true)
                {
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete item release - Item {releaseDetail.GIV_RM_ReceivePalletItem.ItemCode} is already marked as released."
                    };
                }
            }

            return new ServiceWebResult { Success = true };
        }

        private async Task<ServiceWebResult> ValidateReleaseDeletionAsync(GIV_RM_Release release)
        {
            // Check if the release itself has ActualReleaseDate
            if (release.ActualReleaseDate.HasValue)
            {
                return new ServiceWebResult
                {
                    Success = false,
                    ErrorMessage = $"Cannot delete release - Release for {release.GIV_RawMaterial.MaterialNo} was actually released on {release.ActualReleaseDate.Value:yyyy-MM-dd HH:mm}."
                };
            }

            // Validate all release details using the same criteria
            foreach (var detail in release.GIV_RM_ReleaseDetails)
            {
                var detailValidation = await ValidateReleaseDetailDeletionAsync(detail);
                if (!detailValidation.Success)
                {
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete release - {detailValidation.ErrorMessage}"
                    };
                }
            }

            return new ServiceWebResult { Success = true };
        }

        private async Task<ServiceWebResult> ValidateJobDeletionAsync(List<GIV_RM_Release> releases)
        {
            // Validate all releases in the job
            foreach (var release in releases)
            {
                var releaseValidation = await ValidateReleaseDeletionAsync(release);
                if (!releaseValidation.Success)
                {
                    return new ServiceWebResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete job - {releaseValidation.ErrorMessage}"
                    };
                }
            }

            return new ServiceWebResult { Success = true };
        }

        private string GetReleaseDetailDisplayName(GIV_RM_ReleaseDetails releaseDetail)
        {
            if (releaseDetail.IsEntirePallet)
            {
                return $"pallet {releaseDetail.GIV_RM_ReceivePallet?.PalletCode ?? "Unknown"}";
            }
            else
            {
                return $"item {releaseDetail.GIV_RM_ReceivePalletItem?.ItemCode ?? "Unknown"}";
            }
        }
        #endregion

        #endregion
    }
}
