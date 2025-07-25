using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WMS.Application.Helpers;
using WMS.Application.Interfaces;
using WMS.Application.Options;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.API;
using WMS.Domain.DTOs.AWS;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.Interfaces;
using WMS.Domain.Models;
using WMS.Infrastructure.Data;
using static WMS.Domain.DTOs.AppConsts;
using static WMS.Domain.Enumerations.Enumerations;
namespace WMS.Application.Services
{
    public class ContainerService : IContainerService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IAwsS3Service _awsS3Service;
        private readonly AwsS3Config _awsS3Config;
        private readonly ILogger<ContainerService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly GivaudanApiOptions _givaudanApiOptions;
        private readonly PDFHelper _pdfHelper;
        private readonly IAPIService _apiService;
        private readonly IConfiguration _configuration;
        private readonly string _localPhotoFolder;
        private readonly IWebHostEnvironment _env;

        public ContainerService(
            AppDbContext dbContext,
            IMapper mapper,
            ICurrentUserService currentUserService,
            IAwsS3Service awsS3Service,
            IOptions<AwsS3Config> awsOptions,
            ILogger<ContainerService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<GivaudanApiOptions> givaudanApiOptions,
            PDFHelper pDFHelper,
            IAPIService apiService,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _awsS3Service = awsS3Service;
            _awsS3Config = awsOptions.Value;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _givaudanApiOptions = givaudanApiOptions.Value;
            _pdfHelper = pDFHelper;
            _apiService = apiService;
            _configuration = configuration;
            _localPhotoFolder = configuration.GetValue<string>("LocalPhotoFolder") ?? "C:\\Temp\\ContainerPhotos\\";
            _env = env;
        }
        public async Task<Guid> DeltaUpdateContainerAsync(ContainerCreateDto containerDto, string username, bool whFlag)
        {

            var container = _mapper.Map<GIV_Container>(containerDto);
            container.CreatedBy = username;
            container.CreatedAt = DateTime.UtcNow.ToLocalTime();

            // Determine if a container already exists by ContainerUrl ending
            var externalId = containerDto.ContainerURL?.TrimEnd('/').Split('/').Last();
            GIV_Container? existingContainer = null;

            if (!string.IsNullOrEmpty(externalId))
            {
                //_logger.LogInformation("Checking for existing container with external ID: {ExternalId}", externalId);
                //existingContainer = await _dbContext.GIV_Containers
                //    .Where(c => c.ContainerURL != null && c.ContainerURL.EndsWith("/" + externalId) && !c.IsDeleted)
                //    .FirstOrDefaultAsync();

                //14 Jul 2025, Added for job import non hsc too
                //since givaudan is using int as id, might duplicate checking using external id so change to container no and validate by status (only new and unstuff is ongoing one)
                _logger.LogInformation("Checking for existing container with container number: {containerNo} and status either new or unstuffed only.", containerDto.ContainerNo_GW);
                existingContainer = await _dbContext.GIV_Containers
                    .Include(p => p.Status)
                    .Where(c => c.ContainerURL != null && c.Status != null
                    && c.ContainerNo_GW == containerDto.ContainerNo_GW
                    && (c.Status.Name == ContainerStatusCode.New || c.Status.Name == ContainerStatusCode.Unstuffed)
                    && !c.IsDeleted)
                    .FirstOrDefaultAsync();
            }


            if (whFlag)
            {
                // Insert if not exists
                if (existingContainer == null)
                {
                    var newStatus = await _dbContext.GeneralCodes
                .Where(gc => gc.Name == ContainerStatusCode.New &&
                             gc.GeneralCodeType.Name == Domain.DTOs.AppConsts.GeneralCodeType.CONTAINER_STATUS && !gc.IsDeleted)
                .FirstOrDefaultAsync();

                    if (newStatus == null)
                    {
                        _logger.LogWarning("Default container status 'NEW' not found in GeneralCodes.");
                        throw new InvalidOperationException("Container status 'NEW' not defined.");
                    }
                    container.Id = Guid.NewGuid();
                    container.StatusId = newStatus.Id;
                    container.IsDeleted = false;
                    container.WarehouseId = _currentUserService.CurrentWarehouseId;
                    await _dbContext.GIV_Containers.AddAsync(container);


                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Container created with ID: {ContainerId}", container.Id);
                    return container.Id;
                }

                // Exists: update
                existingContainer.Remarks = container.Remarks;
                existingContainer.PlannedDelivery_GW = container.PlannedDelivery_GW;
                existingContainer.HBL = container.HBL;
                existingContainer.PO = container.PO;
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Container with ID: {ContainerId} updated)", existingContainer.Id);
                return existingContainer.Id;
            }
            else
            {
                // Delete if exists
                if (existingContainer != null)
                {
                    var cancelStatus = await _dbContext.GeneralCodes
                .Where(gc => gc.Name == ContainerStatusCode.Cancelled &&
                             gc.GeneralCodeType.Name == Domain.DTOs.AppConsts.GeneralCodeType.CONTAINER_STATUS && !gc.IsDeleted)
                .FirstOrDefaultAsync();

                    if (cancelStatus == null)
                    {
                        _logger.LogWarning("Default container status 'CANCEL' not found in GeneralCodes.");
                        throw new InvalidOperationException("Container status 'CANCEL' not defined.");
                    }
                    existingContainer.IsDeleted = true;
                    existingContainer.StatusId = cancelStatus.Id;
                    existingContainer.ModifiedBy = username;
                    existingContainer.ModifiedAt = DateTime.UtcNow.ToLocalTime();


                    await _dbContext.SaveChangesAsync();
                    _logger.LogInformation("Container with ID: {ContainerId} marked as deleted", existingContainer.Id);
                    return existingContainer.Id;
                }

                // Not exists: do nothing
                return Guid.Empty;
            }
        }

        public async Task<List<Containers>> GetAllContainerAsync()
        {
            _logger.LogInformation("Fetching all containers");

            var containers = await _dbContext.GIV_Containers
                .Where(c => !c.IsDeleted)
                .Select(c => new Containers
                {
                    ContainerId = c.Id,
                    ContainerNo_GW = c.ContainerNo_GW,
                    PlannedDelivery_GW = c.PlannedDelivery_GW,
                    Remarks = c.Remarks,
                    ContainerURL = c.ContainerURL,
                    HasPhotos = c.ContainerPhotos.Any(),
                    UnstuffedBy = c.UnstuffedBy,
                    UnstuffedDate = c.UnstuffedDate,
                    JobReference = c.JobReference,
                    SealNo = c.SealNo,
                    Size = c.Size
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} containers", containers.Count);

            return containers;
        }
        public async Task<List<Containers>> GetContainersByProcessTypeAsync(ContainerProcessType processType)
        {
            _logger.LogInformation("Fetching all containers");

            var containers = await _dbContext.GIV_Containers
                .Where(c => !c.IsDeleted && c.ProcessType == processType)
                .Select(c => new Containers
                {
                    ContainerId = c.Id,
                    ContainerNo_GW = c.ContainerNo_GW,
                    PlannedDelivery_GW = c.PlannedDelivery_GW,
                    Remarks = c.Remarks,
                    ContainerURL = c.ContainerURL,
                    HasPhotos = c.ContainerPhotos.Any(),
                    UnstuffedBy = c.UnstuffedBy,
                    UnstuffedDate = c.UnstuffedDate,
                    JobReference = c.JobReference,
                    SealNo = c.SealNo,
                    Size = c.Size
                })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} containers", containers.Count);

            return containers;
        }

        public async Task<PaginatedResult<ContainerViewDto>> GetPaginatedContainersAsync(
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending)
        {
            _logger.LogInformation("Fetching paginated containers with SearchTerm={SearchTerm}, Start={Start}, Length={Length}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                searchTerm, start, length, sortColumn, sortAscending);

            var query = _dbContext.GIV_Containers
                .Include(p => p.Status)
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.Status != null && x.Status.Name != ContainerStatusCode.Cancelled && x.ProcessType == ContainerProcessType.Import);

            // Apply database search only
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowered = searchTerm.ToLower();
                //var dateFilters = BuildDateFilters(searchTerm);

                query = query.Where(x =>
                    // Text-based searches
                    x.ContainerNo_GW.ToLower().Contains(lowered) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(lowered)) ||
                    (x.UnstuffedBy != null && x.UnstuffedBy.ToLower().Contains(lowered)) ||
                    (x.PO != null && x.PO.ToLower().Contains(lowered))

                // Date-based searches on PlannedDelivery_GW
                //(dateFilters.HasDateFilter && x.PlannedDelivery_GW.HasValue && (
                //    (dateFilters.StartDate.HasValue && dateFilters.EndDate.HasValue &&
                //     x.PlannedDelivery_GW.Value >= dateFilters.StartDate.Value &&
                //     x.PlannedDelivery_GW.Value <= dateFilters.EndDate.Value) ||
                //    (dateFilters.ExactDate.HasValue &&
                //     x.PlannedDelivery_GW.Value.Date == dateFilters.ExactDate.Value.Date)
                //)) ||

                //// Date-based searches on UnstuffedDate
                //(dateFilters.HasDateFilter && x.UnstuffedDate.HasValue && (
                //    (dateFilters.StartDate.HasValue && dateFilters.EndDate.HasValue &&
                //     x.UnstuffedDate.Value >= dateFilters.StartDate.Value &&
                //     x.UnstuffedDate.Value <= dateFilters.EndDate.Value) ||
                //    (dateFilters.ExactDate.HasValue &&
                //     x.UnstuffedDate.Value.Date == dateFilters.ExactDate.Value.Date)
                //))
                );
            }

            var total = await query.CountAsync();
            _logger.LogDebug("Total containers after filtering: {Total}", total);

            query = sortColumn switch
            {
                0 => sortAscending ? query.OrderBy(x => x.ContainerNo_GW) : query.OrderByDescending(x => x.ContainerNo_GW),
                2 => sortAscending ? query.OrderBy(x => x.PlannedDelivery_GW) : query.OrderByDescending(x => x.PlannedDelivery_GW),
                3 => sortAscending ? query.OrderBy(x => x.Remarks) : query.OrderByDescending(x => x.Remarks),
                4 => sortAscending ? query.OrderBy(x => x.UnstuffedBy) : query.OrderByDescending(x => x.UnstuffedBy),
                5 => sortAscending ? query.OrderBy(x => x.UnstuffedDate) : query.OrderByDescending(x => x.UnstuffedDate),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var entities = await query
                .Include(x => x.ContainerPhotos)
                .Skip(start)
                .Take(length)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} container records", entities.Count);

            var allKeys = entities
                .SelectMany(c => c.ContainerPhotos)
                .Where(p => !string.IsNullOrEmpty(p.PhotoFile))
                .Select(p => p.PhotoFile)
                .Distinct()
                .ToList();

            _logger.LogDebug("Generating pre-signed URLs for {PhotoCount} container photos", allKeys.Count);

            var urlMap = await _awsS3Service.GenerateMultiplePresignedUrlsAsync(allKeys, FileType.Photo);

            var containerViewDtos = entities.Select(x => new ContainerViewDto
            {
                ContainerId = x.Id,
                ContainerNo_GW = x.ContainerNo_GW,
                PlannedDelivery_GW = x.PlannedDelivery_GW,
                Remarks = x.Remarks,
                ContainerURL = x.ContainerURL,
                UnstuffedBy = x.UnstuffedBy,
                UnstuffedDate = x.UnstuffedDate,
                HasPhotos = x.ContainerPhotos.Any(),
                PhotoCount = x.ContainerPhotos.Count,
                IsLoose = x.IsLoose,
                IsSamplingArrAtWarehouse = x.IsSamplingArrAtWarehouse,
                ContainerPhotos = x.ContainerPhotos.Select(p => new ContainerPhotoDto
                {
                    Id = p.Id,
                    FileName = p.FileName ?? string.Empty,
                    FileType = p.FileType,
                    FileSizeBytes = p.FileSizeBytes,
                    ContentType = p.ContentType ?? string.Empty,
                    S3Key = p.PhotoFile,
                    Url = urlMap.TryGetValue(p.PhotoFile, out var presignedUrl) ? presignedUrl : string.Empty
                }).ToList()
            }).ToList();

            // Use HashSet to avoid duplicate job IDs with better performance
            var jobIdSet = new HashSet<int>();
            var jobImportNonHSCIdSet = new HashSet<int>();
            var jobImportCEVAIdSet = new HashSet<int>();
            List<JobTypeAndIdsDto> requestDtos = new List<JobTypeAndIdsDto>
            {
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT_NONHSC, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT_CEVA, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL_WL, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL_RDC, JobIds = [] },
            };

            // Extract jobIds and assign JobId
            foreach (var container in containerViewDtos)
            {
                //int jobId = ExtractJobIdFromUrl(container.ContainerURL);
                var (jobId, jobType) = ExtractJobInfoFromUrl(container.ContainerURL);
                container.JobId = jobId;

                if (jobId != 0)
                {
                    requestDtos.FirstOrDefault(x => x.JobType == jobType)?.JobIds.Add(jobId);

                    container.JobType = jobType;
                }
            }

            var jobImports = await GetExternalContainerInfosByJobIdsAsync(requestDtos);

            if (jobImports != null && jobImports.Any())
            {
                foreach (var container in containerViewDtos)
                {
                    container.ConcatePO = jobImports.FirstOrDefault(x => x.JobId == container.JobId && x.JobType == container.JobType)?.Marks;
                }
            }

            _logger.LogInformation("Returning paginated result with {Count} containers", containerViewDtos.Count);

            return new PaginatedResult<ContainerViewDto>
            {
                TotalCount = total,
                FilteredCount = total,
                Items = containerViewDtos
            };
        }

        public async Task<PaginatedResult<ContainerViewDto>> GetPaginatedContainersByTypeAsync(
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending,
            ContainerProcessType processType = ContainerProcessType.Import) // Default to Import
        {
            _logger.LogInformation("Fetching paginated containers with ProcessType={ProcessType}, SearchTerm={SearchTerm}, Start={Start}, Length={Length}, SortColumn={SortColumn}, SortAscending={SortAscending}",
                processType, searchTerm, start, length, sortColumn, sortAscending);

            var query = _dbContext.GIV_Containers
                .Include(p => p.Status)
                .AsNoTracking()
                .Where(x => !x.IsDeleted
                           && x.Status != null
                           && x.Status.Name != ContainerStatusCode.Cancelled
                           && x.ProcessType == processType); // Add ProcessType filter

            // Apply database search only
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var lowered = searchTerm.ToLower();

                query = query.Where(x =>
                    // Text-based searches
                    x.ContainerNo_GW.ToLower().Contains(lowered) ||
                    (x.Remarks != null && x.Remarks.ToLower().Contains(lowered)) ||
                    (x.UnstuffedBy != null && x.UnstuffedBy.ToLower().Contains(lowered)) ||
                    (x.StuffedBy != null && x.StuffedBy.ToLower().Contains(lowered)) || // Add StuffedBy search
                    (x.JobReference != null && x.JobReference.ToLower().Contains(lowered)) || // Add JobReference search
                    (x.SealNo != null && x.SealNo.ToLower().Contains(lowered)) || // Add SealNo search
                    (x.PO != null && x.PO.ToLower().Contains(lowered))
                );
            }

            var total = await query.CountAsync();
            _logger.LogDebug("Total containers after filtering: {Total}", total);

            // Update sorting to handle export-specific columns
            query = sortColumn switch
            {
                0 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.JobReference) : query.OrderByDescending(x => x.JobReference))
                    : (sortAscending ? query.OrderBy(x => x.ContainerNo_GW) : query.OrderByDescending(x => x.ContainerNo_GW)),
                1 => sortAscending ? query.OrderBy(x => x.ContainerNo_GW) : query.OrderByDescending(x => x.ContainerNo_GW),
                2 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.SealNo) : query.OrderByDescending(x => x.SealNo))
                    : (sortAscending ? query.OrderBy(x => x.PlannedDelivery_GW) : query.OrderByDescending(x => x.PlannedDelivery_GW)),
                3 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.Size) : query.OrderByDescending(x => x.Size))
                    : (sortAscending ? query.OrderBy(x => x.Remarks) : query.OrderByDescending(x => x.Remarks)),
                4 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.Remarks) : query.OrderByDescending(x => x.Remarks))
                    : (sortAscending ? query.OrderBy(x => x.UnstuffedBy) : query.OrderByDescending(x => x.UnstuffedBy)),
                5 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.StuffedDate) : query.OrderByDescending(x => x.StuffedDate))
                    : (sortAscending ? query.OrderBy(x => x.UnstuffedDate) : query.OrderByDescending(x => x.UnstuffedDate)),
                6 => processType == ContainerProcessType.Export
                    ? (sortAscending ? query.OrderBy(x => x.StuffedBy) : query.OrderByDescending(x => x.StuffedBy))
                    : query.OrderByDescending(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var entities = await query
                .Include(x => x.ContainerPhotos)
                .Skip(start)
                .Take(length)
                .ToListAsync();

            _logger.LogDebug("Retrieved {Count} container records", entities.Count);

            var allKeys = entities
                .SelectMany(c => c.ContainerPhotos)
                .Where(p => !string.IsNullOrEmpty(p.PhotoFile))
                .Select(p => p.PhotoFile)
                .Distinct()
                .ToList();

            _logger.LogDebug("Generating pre-signed URLs for {PhotoCount} container photos", allKeys.Count);

            var urlMap = await _awsS3Service.GenerateMultiplePresignedUrlsAsync(allKeys, FileType.Photo);

            var containerViewDtos = entities.Select(x => new ContainerViewDto
            {
                ContainerId = x.Id,
                ContainerNo_GW = x.ContainerNo_GW,
                PlannedDelivery_GW = x.PlannedDelivery_GW,
                Remarks = x.Remarks,
                ContainerURL = x.ContainerURL,
                UnstuffedBy = x.UnstuffedBy,
                UnstuffedDate = x.UnstuffedDate,
                StuffedBy = x.StuffedBy, // Add new export fields
                StuffedDate = x.StuffedDate,
                JobReference = x.JobReference,
                SealNo = x.SealNo,
                Size = x.Size,
                HasPhotos = x.ContainerPhotos.Any(),
                PhotoCount = x.ContainerPhotos.Count,
                IsLoose = x.IsLoose,
                IsSamplingArrAtWarehouse = x.IsSamplingArrAtWarehouse,
                ContainerPhotos = x.ContainerPhotos.Select(p => new ContainerPhotoDto
                {
                    Id = p.Id,
                    FileName = p.FileName ?? string.Empty,
                    FileType = p.FileType,
                    FileSizeBytes = p.FileSizeBytes,
                    ContentType = p.ContentType ?? string.Empty,
                    S3Key = p.PhotoFile,
                    Url = urlMap.TryGetValue(p.PhotoFile, out var presignedUrl) ? presignedUrl : string.Empty
                }).ToList()
            }).ToList();

            // Job ID extraction logic remains the same...
            var jobIdSet = new HashSet<int>();
            var jobImportNonHSCIdSet = new HashSet<int>();
            var jobImportCEVAIdSet = new HashSet<int>();
            List<JobTypeAndIdsDto> requestDtos = new List<JobTypeAndIdsDto>
            {
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT_NONHSC, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.IMPORT_CEVA, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL_WL, JobIds = [] },
                new JobTypeAndIdsDto{ JobType = GivaudanJobTye.EXPORT_FCL_RDC, JobIds = [] },

            };

            // Extract jobIds and assign JobId
            foreach (var container in containerViewDtos)
            {
                //int jobId = ExtractJobIdFromUrl(container.ContainerURL);
                var (jobId, jobType) = ExtractJobInfoFromUrl(container.ContainerURL);
                container.JobId = jobId;

                if (jobId != 0)
                {
                    requestDtos.FirstOrDefault(x => x.JobType == jobType)?.JobIds.Add(jobId);
                    container.JobType = jobType;
                }
            }

            var jobs = await GetExternalContainerInfosByJobIdsAsync(requestDtos);
            if (jobs != null && jobs.Any())
            {
                foreach (var container in containerViewDtos)
                {
                    var job = jobs.FirstOrDefault(x => x.JobId == container.JobId && x.JobType == container.JobType);
                    if (job == null) continue;

                    int containerSize = job.ContainerSize;

                    container.ConcatePO = job.Marks;
                    container.JobReference = job.YourRef;
                    container.SealNo = container.SealNo ?? job.SealNumber;
                    container.Size = container.Size != 0 ? container.Size : containerSize;
                    container.SealNo = container.SealNo ?? job.SealNumber;
                }
            }
            _logger.LogInformation("Returning paginated result with {Count} containers for ProcessType {ProcessType}", containerViewDtos.Count, processType);

            return new PaginatedResult<ContainerViewDto>
            {
                TotalCount = total,
                FilteredCount = total,
                Items = containerViewDtos
            };
        }
        public async Task<ApiResponseDto<string>> UpdateContainerAsync(ContainerUpdateDto dto, string username)
        {
            try
            {
                var container = await _dbContext.GIV_Containers
                     .Include(c => c.ContainerPhotos)
                     .Include(c => c.Warehouse)
                     .FirstOrDefaultAsync(c => c.Id == dto.Id && !c.IsDeleted);

                if (container == null)
                    return ApiResponseDto<string>.ErrorResult("Container not found");

                if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return ApiResponseDto<string>.ErrorResult(
                        "Invalid user ID format.",
                        new List<string> { "User ID could not be parsed to GUID." }
                    );
                }

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

                if (user == null)
                {
                    return ApiResponseDto<string>.ErrorResult(
                        $"User '{_currentUserService.GetCurrentUsername}' not found.",
                        new List<string> { "Invalid User." }
                    );
                }

                var client = await _dbContext.Clients
                    .FirstOrDefaultAsync(c => c.Id == user.ClientId && !c.IsDeleted);

                if (client == null)
                {
                    return ApiResponseDto<string>.ErrorResult(
                        $"User '{_currentUserService.GetCurrentUsername}' Client ID is not set.",
                        new List<string> { "Invalid Client ID." }
                    );
                }


                // Partial field updates
                if (dto.UnstuffedDate.HasValue)
                    container.UnstuffedDate = DateTime.SpecifyKind(dto.UnstuffedDate.Value, DateTimeKind.Utc);

                if (!string.IsNullOrWhiteSpace(dto.UnstuffedBy))
                    container.UnstuffedBy = dto.UnstuffedBy;

                if (!string.IsNullOrWhiteSpace(dto.Remarks))
                    container.Remarks = dto.Remarks;

                if (!string.IsNullOrWhiteSpace(dto.ContainerURL))
                {
                    var externalId = dto.ContainerURL?.TrimEnd('/').Split('/').Last();
                    GIV_Container? existingContainer = null;

                    if (!string.IsNullOrEmpty(externalId))
                    {
                        _logger.LogInformation("Checking for existing container with external ID: {ExternalId}", externalId);
                        existingContainer = await _dbContext.GIV_Containers
                            .Where(c => c.Id != dto.Id && c.ContainerURL != null && c.ContainerURL.EndsWith("/" + externalId) && !c.IsDeleted)
                            .FirstOrDefaultAsync();
                    }
                    if (existingContainer != null)
                    {
                        _logger.LogWarning("Container with URL {ContainerURL} already exists.", dto.ContainerURL);
                        return ApiResponseDto<string>.ErrorResult("Container with this URL already exists.");
                    }
                    container.ContainerURL = dto.ContainerURL;
                }



                if (!string.IsNullOrWhiteSpace(dto.PO))
                    container.PO = dto.PO;

                if (!string.IsNullOrWhiteSpace(dto.HBL))
                    container.HBL = dto.HBL;

                if (dto.UnstuffStartTime.HasValue)
                    container.UnstuffStartTime = DateTime.SpecifyKind(dto.UnstuffStartTime.Value, DateTimeKind.Utc);

                if (dto.UnstuffEndTime.HasValue)
                    container.UnstuffEndTime = DateTime.SpecifyKind(dto.UnstuffEndTime.Value, DateTimeKind.Utc);

                if (!string.IsNullOrWhiteSpace(dto.SealNo))
                    container.SealNo = dto.SealNo;

                if (dto.Size != 0)
                    container.Size = dto.Size;

                if (dto.StuffedDate.HasValue)
                    container.StuffedDate = DateTime.SpecifyKind(dto.StuffedDate.Value, DateTimeKind.Utc);

                if (!string.IsNullOrWhiteSpace(dto.StuffedBy))
                    container.StuffedBy = dto.StuffedBy;

                if (dto.StuffStartTime.HasValue)
                    container.StuffStartTime = DateTime.SpecifyKind(dto.StuffStartTime.Value, DateTimeKind.Utc);

                if (dto.StuffEndTime.HasValue)
                    container.StuffEndTime = DateTime.SpecifyKind(dto.StuffEndTime.Value, DateTimeKind.Utc);

                if (dto.Photos != null && dto.Photos.Any())
                {
                    var warehouse = container.Warehouse;
                    List<GIV_ContainerPhoto> givContainerPhotos = new List<GIV_ContainerPhoto>();

                    foreach (var file in dto.Photos)
                    {
                        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var key = $"{_awsS3Config.FolderEnvironment}/{warehouse.Code}/{client.Code}/Container/{container.Id}/{uniqueFileName}";

                        var s3Key = await _awsS3Service.UploadFileAsync(file, key);
                        var fileType = _awsS3Service.DetermineFileType(file);

                        givContainerPhotos.Add(new GIV_ContainerPhoto
                        {
                            Id = Guid.NewGuid(),
                            ContainerId = container.Id,
                            PhotoFile = s3Key,
                            FileName = file.FileName,
                            FileType = fileType,
                            FileSizeBytes = file.Length,
                            ContentType = file.ContentType,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = username
                        });
                    }
                    await _dbContext.GIV_ContainerPhotos.AddRangeAsync(givContainerPhotos);
                    givContainerPhotos.ForEach(x =>
                    {
                        container.ContainerPhotos.Add(x);
                    });
                }
                //update status
                var unstuffStatus = await _dbContext.GeneralCodes
                    .Where(gc => gc.Name == ContainerStatusCode.Unstuffed &&
                                 gc.GeneralCodeType.Name == Domain.DTOs.AppConsts.GeneralCodeType.CONTAINER_STATUS && !gc.IsDeleted)
                    .FirstOrDefaultAsync();
                if (unstuffStatus == null)
                {
                    _logger.LogWarning("Default container status 'UNSTUFFED' not found in GeneralCodes.");
                    return ApiResponseDto<string>.ErrorResult("Container status 'UNSTUFFED' not defined.");
                }
                container.StatusId = unstuffStatus.Id;
                container.ModifiedBy = username;
                container.ModifiedAt = DateTime.UtcNow.ToLocalTime();
                _logger.LogInformation("Updating container with ID: {ContainerId} by user: {Username}", container.Id, username);
                _dbContext.Update(container);
                await _dbContext.SaveChangesAsync();

                return ApiResponseDto<string>.SuccessResult(container.Id.ToString(), "Container updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResponseDto<string>.ErrorResult(ex.Message, new List<string>());
            }
        }

        public async Task<ApiResponseDto<List<string>>> GetContainerPhotoUrlsAsync(Guid containerId)
        {
            var container = await _dbContext.GIV_Containers
                .Include(c => c.ContainerPhotos)
                .FirstOrDefaultAsync(c => c.Id == containerId && !c.IsDeleted);

            if (container == null)
                return ApiResponseDto<List<string>>.ErrorResult("Container not found.");

            var keys = container.ContainerPhotos
                .Where(p => !string.IsNullOrWhiteSpace(p.PhotoFile))
                .Select(p => p.PhotoFile)
                .ToList();

            if (!keys.Any())
                return ApiResponseDto<List<string>>.SuccessResult(new List<string>(), "No photos available.");

            var urls = await _awsS3Service.GenerateMultiplePresignedUrlsAsync(keys, FileType.Photo);
            return ApiResponseDto<List<string>>.SuccessResult(urls.Values.ToList(), "Photo URLs retrieved successfully.");
        }
        public async Task<ApiResponseDto<string>> DeleteContainerPhotoAsync(Guid photoId)
        {
            var photo = await _dbContext.GIV_ContainerPhotos
                .FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted);
            _logger.LogInformation("Attempting to delete container photo with ID: {PhotoId}", photoId);
            if (photo == null)
            {
                return ApiResponseDto<string>.ErrorResult("Photo not found.");
            }

            try
            {
                // Delete from S3
                await _awsS3Service.DeleteObjectAsync(photo.PhotoFile);
                _logger.LogInformation("Deleted photo file from S3: {PhotoFile}", photo.PhotoFile);
                // Remove from DB
                _dbContext.GIV_ContainerPhotos.Remove(photo);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Deleted container photo with ID: {PhotoId}", photoId);
                return ApiResponseDto<string>.SuccessResult(photoId.ToString(), "Photo deleted successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete container photo with ID {PhotoId}", photoId);
                return ApiResponseDto<string>.ErrorResult("Error deleting photo.");
            }
        }
        public async Task<ApiResponseDto<string>> ReplaceContainerPhotoAsync(Guid photoId, IFormFile newFile)
        {
            _logger.LogInformation("Replacing photo with ID: {PhotoId}", photoId);
            var photo = await _dbContext.GIV_ContainerPhotos
                .Include(p => p.Container)
                .ThenInclude(c => c.Warehouse)
                .FirstOrDefaultAsync(p => p.Id == photoId && !p.IsDeleted);

            if (photo == null)
                return ApiResponseDto<string>.ErrorResult("Photo not found.");

            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return ApiResponseDto<string>.ErrorResult(
        "Invalid user ID format.",
        new List<string> { "User ID could not be parsed to GUID." }
    );
            }

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

            if (user == null)
            {
                return ApiResponseDto<string>.ErrorResult(
                    $"User '{_currentUserService.GetCurrentUsername}' not found.",
                    new List<string> { "Invalid User." }
                );
            }

            var client = await _dbContext.Clients
                .FirstOrDefaultAsync(c => c.Id == user.ClientId && !c.IsDeleted);

            if (client == null)
            {
                return ApiResponseDto<string>.ErrorResult(
                    $"User '{_currentUserService.GetCurrentUsername}' Client ID is not set.",
                    new List<string> { "Invalid Client ID." }
                );
            }

            try
            {
                // Delete old file
                await _awsS3Service.DeleteObjectAsync(photo.PhotoFile);
                _logger.LogInformation("Deleted old photo file: {PhotoFile}", photo.PhotoFile);
                // Upload new file
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(newFile.FileName)}";
                var key = $"{_awsS3Config.FolderEnvironment}/{photo.Container.Warehouse.Code}/{client.Code}/Container/{photo.ContainerId}/{uniqueFileName}";

                var newKey = await _awsS3Service.UploadFileAsync(newFile, key);
                var newUrl = await _awsS3Service.GeneratePresignedUrlAsync(newKey, FileType.Photo);
                _logger.LogInformation("Uploaded new photo file: {NewFileName} with key: {NewKey}", newFile.FileName, newKey);
                // Update DB
                photo.PhotoFile = newKey;
                photo.FileName = newFile.FileName;
                photo.FileSizeBytes = newFile.Length;
                photo.ContentType = newFile.ContentType;
                photo.FileType = _awsS3Service.DetermineFileType(newFile);
                photo.ModifiedAt = DateTime.UtcNow;
                photo.ModifiedBy = _currentUserService.GetCurrentUsername;
                _logger.LogInformation("Replacing photo with ID: {PhotoId} for container: {ContainerId}", photoId, photo.ContainerId);
                await _dbContext.SaveChangesAsync();

                return ApiResponseDto<string>.SuccessResult(newUrl, "Photo updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to replace photo");
                return ApiResponseDto<string>.ErrorResult("Failed to update photo.");
            }
        }
        public async Task<ApiResponseDto<byte[]>> GenerateExcelReportAsync(Guid containerId)
        {
            _logger.LogInformation("Generating Excel report for container ID: {ContainerId}", containerId);
            ExcelPackage.License.SetNonCommercialOrganization("HSC WMS");
            var container = await _dbContext.GIV_Containers
                .FirstOrDefaultAsync(c => c.Id == containerId && !c.IsDeleted);

            if (container == null)
                return ApiResponseDto<byte[]>.ErrorResult("Container not found.");

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Report");

            ws.Cells["A1"].Value = "Vessel Name";
            ws.Cells["B1"].Value = "Job No";
            ws.Cells["C1"].Value = "Voyage No";
            ws.Cells["D1"].Value = "POL";
            ws.Cells["E1"].Value = "Container No";
            ws.Cells["F1"].Value = "Seal No";
            ws.Cells["G1"].Value = "Size";
            ws.Cells["H1"].Value = "ETA";

            _logger.LogInformation("Container report data populated for container ID: {ContainerId}", containerId);
            return ApiResponseDto<byte[]>.SuccessResult(package.GetAsByteArray(), "Excel generated");
        }
        public async Task<ApiResponseDto<(byte[] FileContent, string FileName)>> GeneratePdfReportAsync(
            Guid containerId,
            CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope(
                "PDF Report Pipeline for ContainerId={ContainerId}", containerId);
            _logger.LogInformation("▶️ Starting GeneratePdfReportAsync");
            var clientname = await _dbContext.Clients
                .Where(c => c.Id == _currentUserService.CurrentClientId && !c.IsDeleted)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(clientname))
            {
                _logger.LogWarning("Client name not found for ClientId={ClientId}", _currentUserService.CurrentClientId);
                return ApiResponseDto<(byte[] FileContent, string FileName)>.ErrorResult("Client name not found.");
            }

            var container = await _dbContext.GIV_Containers
                .Include(c => c.ContainerPhotos)
                .Include(c => c.Status)
                .FirstOrDefaultAsync(c => c.Id == containerId && !c.IsDeleted, cancellationToken);
            if (container == null)
            {
                _logger.LogWarning("Container not found (Id={ContainerId})", containerId);
                return ApiResponseDto<(byte[] FileContent, string FileName)>.ErrorResult("Container not found.");
            }
            _logger.LogInformation("Loaded container (Id={ContainerId}) with {PhotoCount} container photos",
                containerId, container.ContainerPhotos.Count);

            if (container.ContainerPhotos.Any() && container.ContainerPhotos.First().CreatedAt != null)
            {
                container.ContainerPhotos = container.ContainerPhotos
                    .OrderBy(p => p.CreatedAt)
                    .ToList();
                _logger.LogInformation("Sorted container photos by CreatedAt timestamp");
            }
            else if (container.ContainerPhotos.Any())
            {
                container.ContainerPhotos = container.ContainerPhotos
                    .OrderBy(p => p.Id)  
                    .ToList();
                _logger.LogInformation("Sorted container photos by Id as fallback");
            }

            var receives = await _dbContext.GIV_RM_Receives
                .Include(r => r.RawMaterial)
                .Include(r => r.PackageType)
                .Include(r => r.RM_ReceivePallets)
                    .ThenInclude(p => p.RM_ReceivePalletItems)
                .Where(r => r.ContainerId == containerId)
                .ToListAsync(cancellationToken);
            _logger.LogInformation("Loaded {ReceiveCount} receives", receives.Count);

            var (urlId, jobType) = ExtractJobInfoFromUrl(container.ContainerURL);

            _logger.LogInformation("Extracted JobId={JobId} from ContainerURL='{Url}'",
                urlId, container.ContainerURL);
            var swExt = Stopwatch.StartNew();
            var externalData = await GetExternalContainerInfoAsync(urlId, jobType, cancellationToken);
            swExt.Stop();
            _logger.LogInformation("External API fetch completed in {Elapsed}ms, data {HasData}",
                swExt.ElapsedMilliseconds, externalData != null ? "FOUND" : "NULL");

            var allPhotoKeys = container.ContainerPhotos
                .Select(c => c.PhotoFile)
                .Distinct()
                .ToList();
            _logger.LogInformation("Total distinct photo keys: {KeyCount}", allPhotoKeys.Count);

            var swSign = Stopwatch.StartNew();
            var photoUrls = await _awsS3Service
                .GenerateMultiplePresignedUrlsAsync(allPhotoKeys, FileType.Photo);
            swSign.Stop();
            _logger.LogInformation("Generated {UrlCount} presigned URLs in {Elapsed}ms",
                photoUrls.Count, swSign.ElapsedMilliseconds);

            var photoStreams = new Dictionary<string, MemoryStream>();
            using var http = _httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(30);

            foreach (var (key, url) in photoUrls)
            {
                try
                {
                    _logger.LogInformation("Downloading image for key={Key}", key);
                    var bytes = await http.GetByteArrayAsync(url, cancellationToken);
                    photoStreams[key] = new MemoryStream(bytes);
                    _logger.LogInformation("Downloaded {ByteCount} bytes for key={Key}", bytes.Length, key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to download image for key={Key}. Skipping in PDF.", key);
                }
            }
            _logger.LogInformation("Prepared {StreamCount} images for PDF", photoStreams.Count);

            var swPdf = Stopwatch.StartNew();
            var pdfBytes = _pdfHelper.GenerateContainerReport(
                container, receives, externalData, photoStreams, clientname);
            swPdf.Stop();
            _logger.LogInformation("Rendered PDF in {Elapsed}ms, final size={Size} bytes",
                swPdf.ElapsedMilliseconds, pdfBytes.Length);

            string fileName = $"UnstuffingReport_{container.ContainerNo_GW}.pdf";
            _logger.LogInformation("Generated PDF with filename: {FileName}", fileName);

            return ApiResponseDto<(byte[] FileContent, string FileName)>.SuccessResult(
                (pdfBytes, fileName), "PDF generated successfully");
        }


        private int ExtractJobIdFromUrl(string? url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return 0;

            var lastSegment = uri.Segments.Last().TrimEnd('/');
            return int.TryParse(lastSegment, out var id) ? id : 0;
        }
        private (int jobId, string jobType) ExtractJobInfoFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return (0, string.Empty);

            var segments = uri.Segments.Select(s => s.TrimEnd('/')).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            // Expected: [..., "JobImport"/"JobExport", "Edit"/"EditNonHSC"/"EditCEVA", "1079"]
            if (segments.Length < 3)
                return (0, string.Empty);

            var controller = segments[^3]; // "JobImport", "JobExport", etc.
            var action = segments[^2]; // "Edit", "EditNonHSC", "EditCEVA", etc.
            var jobIdString = segments[^1]; // "1079"

            if (!int.TryParse(jobIdString, out var jobId) || jobId <= 0)
                return (0, string.Empty);

            // Build jobType: Controller + Action suffix (if not "Edit")
            var jobType = action.Equals("Edit", StringComparison.OrdinalIgnoreCase)
                ? controller // "JobImport", "JobExport"
                : $"{controller}{action.Substring(4)}"; // "JobImportNonHSC", "JobImportCEVA"

            return (jobId, jobType);
        }
        private async Task<JobVesselCntrInfoDto?> GetExternalContainerInfoAsync(int jobId, string jobType, CancellationToken cancellationToken = default)
        {
            using var scope = _logger.BeginScope("External API call for JobId={JobId}", jobId);

            if (jobId == 0)
            {
                _logger.LogWarning("JobId is 0, skipping external API");
                return null;
            }

            // 1) Read and validate config
            var email = _configuration["APIInfo:GivaudanEmail"];
            var pass = _configuration["APIInfo:GivaudanPassword"];
            var authUrl = _configuration["APIInfo:GivaudanAuthUrl"];
            var baseUrl = _configuration["APIInfo:GivaudanBaseUrl"];
            _logger.LogDebug("Config: AuthUrl={AuthUrl}, BaseUrl={BaseUrl}, EmailSet={EmailSet}",
                authUrl, baseUrl, !string.IsNullOrWhiteSpace(email));

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(pass) ||
                string.IsNullOrWhiteSpace(authUrl) ||
                string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Missing one or more APIInfo configuration values");
                return null;
            }

            try
            {
                // 2) Authenticate
                _logger.LogInformation("Requesting bearer token from {AuthUrl}", authUrl);
                var token = await _apiService
                    .GetBearerTokenFromEmailPasswordAsync(email, pass, authUrl, cancellationToken);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogInformation("Received empty bearer token");
                    return null;
                }
                _logger.LogInformation("Bearer token length={Length}", token.Length);

                // 3) Call data endpoint
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var url = $"{baseUrl}/{jobType}/GetVesselContainerInfoById/{jobId}";

                _logger.LogInformation("Calling external GET {Url}", url);

                var sw = Stopwatch.StartNew();
                var response = await client.GetAsync(url, cancellationToken);
                sw.Stop();

                _logger.LogInformation("External API responded {StatusCode} in {Elapsed}ms",
                    response.StatusCode, sw.ElapsedMilliseconds);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Non-success status code: {StatusCode} with {Content}", response.StatusCode, response.Content);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogInformation("Response JSON (first 500 chars): {Snippet}",
                    json.Length > 500 ? json.Substring(0, 500) : json);

                var dto = JsonSerializer.Deserialize<JobVesselCntrInfoDto>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _logger.LogInformation("Deserialized external data: {HasDto}", dto != null);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in external API call for JobId={JobId}", jobId);
                return null;
            }
        }
        private async Task<List<JobVesselCntrInfoDto>?> GetExternalContainerInfosByJobIdsAsync(
            List<JobTypeAndIdsDto> jobTypeAndIdsRequestDtos,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Return null if no job IDs provided
                if (jobTypeAndIdsRequestDtos.Count == 0)
                {
                    return null;
                }

                // Get authentication token
                var email = _configuration["APIInfo:GivaudanEmail"];
                var password = _configuration["APIInfo:GivaudanPassword"];
                var authUrl = _configuration["APIInfo:GivaudanAuthUrl"];
                var baseUrl = _configuration["APIInfo:GivaudanBaseUrl"];

                var token = await _apiService.GetBearerTokenFromEmailPasswordAsync(
                    email!,
                    password!,
                    authUrl!,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Bearer token was null or empty.");
                    return null;
                }

                // Make unified API call
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var url = $"{baseUrl}/Job/GetVesselContainerInfoByJobTypeAndListId";
                var requestBody = JsonSerializer.Serialize(new
                {
                    jobTypeAndIdsRequestDtos = jobTypeAndIdsRequestDtos
                });

                var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(url, content, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Failed to fetch container infoStatus: {StatusCode}",
                        response.StatusCode);
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<List<JobVesselCntrInfoDto>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling unified container info API ");
                return null;
            }
        }
        public async Task<List<JobAttachmentsDto>?> GetExternalContainerInfoAttachmentByIdAsync(Guid containerId, CancellationToken cancellationToken = default)
        {
            var container = await _dbContext.GIV_Containers.FindAsync(containerId);
            if (container == null) return null;

            var (jobId, jobType) = ExtractJobInfoFromUrl(container.ContainerURL);

            using var scope = _logger.BeginScope("GetExternalContainerInfoAttachmentByIdAsync - JobId={JobId}", jobId);

            _logger.LogInformation("Starting external API call for job attachments - JobId: {JobId}", jobId);

            // Input validation
            if (jobId <= 0)
            {
                _logger.LogWarning("Invalid JobId provided: {JobId}. JobId must be positive integer", jobId);
                return null;
            }

            // Configuration validation with detailed logging
            _logger.LogDebug("Validating API configuration settings");
            var email = _configuration["APIInfo:GivaudanEmail"];
            var pass = _configuration["APIInfo:GivaudanPassword"];
            var authUrl = _configuration["APIInfo:GivaudanAuthUrl"];
            var baseUrl = _configuration["APIInfo:GivaudanBaseUrl"];

            var configStatus = new
            {
                EmailConfigured = !string.IsNullOrWhiteSpace(email),
                PasswordConfigured = !string.IsNullOrWhiteSpace(pass),
                AuthUrlConfigured = !string.IsNullOrWhiteSpace(authUrl),
                BaseUrlConfigured = !string.IsNullOrWhiteSpace(baseUrl),
                AuthUrl = authUrl,
                BaseUrl = baseUrl,
                EmailMasked = !string.IsNullOrWhiteSpace(email) ? $"{email[..Math.Min(3, email.Length)]}***" : "MISSING"
            };

            _logger.LogDebug("Configuration validation result: {@ConfigStatus}", configStatus);

            if (!configStatus.EmailConfigured || !configStatus.PasswordConfigured ||
                !configStatus.AuthUrlConfigured || !configStatus.BaseUrlConfigured)
            {
                _logger.LogError("Missing required APIInfo configuration. Email: {EmailSet}, Password: {PasswordSet}, AuthUrl: {AuthUrlSet}, BaseUrl: {BaseUrlSet}",
                    configStatus.EmailConfigured, configStatus.PasswordConfigured,
                    configStatus.AuthUrlConfigured, configStatus.BaseUrlConfigured);
                return null;
            }

            try
            {
                // Authentication phase
                _logger.LogInformation("Phase 1: Requesting bearer token from authentication endpoint");
                _logger.LogDebug("Authentication URL: {AuthUrl}", authUrl);

                var authStopwatch = Stopwatch.StartNew();
                var token = await _apiService
                    .GetBearerTokenFromEmailPasswordAsync(email, pass, authUrl, cancellationToken);
                authStopwatch.Stop();

                _logger.LogInformation("Authentication completed in {AuthDuration}ms", authStopwatch.ElapsedMilliseconds);

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Authentication failed - received empty or null bearer token");
                    return null;
                }

                _logger.LogInformation("Bearer token received successfully - Length: {TokenLength} characters", token.Length);
                _logger.LogDebug("Token preview: {TokenPreview}...", token.Length > 20 ? token[..20] : token);

                // API call phase
                _logger.LogInformation("Phase 2: Making external API call for job attachments");

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var requestUrl = $"{baseUrl}/{jobType}/Get{jobType}AttachmentsById/{jobId}";
           
                _logger.LogInformation("Target URL: {RequestUrl}", requestUrl);

                var apiStopwatch = Stopwatch.StartNew();
                var response = await client.GetAsync(requestUrl, cancellationToken);
                apiStopwatch.Stop();

                var responseDetails = new
                {
                    StatusCode = (int)response.StatusCode,
                    StatusCodeName = response.StatusCode.ToString(),
                    Duration = apiStopwatch.ElapsedMilliseconds,
                    ContentLength = response.Content.Headers.ContentLength,
                    ContentType = response.Content.Headers.ContentType?.ToString()
                };

                _logger.LogInformation("API call completed - Status: {StatusCode} ({StatusCodeName}), Duration: {Duration}ms, ContentLength: {ContentLength}",
                    responseDetails.StatusCode, responseDetails.StatusCodeName, responseDetails.Duration, responseDetails.ContentLength);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("API call failed with status {StatusCode}. Response content: {ErrorContent}",
                        response.StatusCode, errorContent);

                    // Log additional response headers that might help with debugging
                    if (response.Headers.Any())
                    {
                        var headers = response.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}");
                        _logger.LogDebug("Response headers: {ResponseHeaders}", string.Join(" | ", headers));
                    }

                    return null;
                }

                // Response processing phase
                _logger.LogInformation("Phase 3: Processing successful API response");

                var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var contentPreview = jsonContent.Length > 500 ? jsonContent[..500] + "..." : jsonContent;

                _logger.LogDebug("Response JSON preview (first 500 chars): {JsonPreview}", contentPreview);
                _logger.LogInformation("Response content length: {ContentLength} characters", jsonContent.Length);

                var deserializationStopwatch = Stopwatch.StartNew();
                var attachmentDtos = JsonSerializer.Deserialize<List<JobAttachmentsDto>>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                deserializationStopwatch.Stop();

                _logger.LogInformation("JSON deserialization completed in {DeserializationDuration}ms", deserializationStopwatch.ElapsedMilliseconds);

                if (attachmentDtos == null)
                {
                    _logger.LogWarning("Deserialization returned null - possibly malformed JSON response");
                    return null;
                }

                _logger.LogInformation("Successfully retrieved {AttachmentCount} attachments for JobId: {JobId}",
                    attachmentDtos.Count, jobId);

                if (attachmentDtos.Count > 0)
                {
                    _logger.LogDebug("Attachment details: {AttachmentSummary}",
                        attachmentDtos.Select(dto => new { dto.FileName }).ToList());
                }

                var totalDuration = authStopwatch.ElapsedMilliseconds + apiStopwatch.ElapsedMilliseconds + deserializationStopwatch.ElapsedMilliseconds;
                _logger.LogInformation("External API operation completed successfully - Total duration: {TotalDuration}ms (Auth: {AuthDuration}ms, API: {ApiDuration}ms, Deserialization: {DeserializationDuration}ms)",
                    totalDuration, authStopwatch.ElapsedMilliseconds, apiStopwatch.ElapsedMilliseconds, deserializationStopwatch.ElapsedMilliseconds);

                return attachmentDtos;
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
            {
                _logger.LogWarning("External API call was cancelled for JobId: {JobId}", jobId);
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request failed for JobId: {JobId}. Message: {ErrorMessage}",
                    jobId, ex.Message);
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON deserialization failed for JobId: {JobId}. Invalid response format. Message: {ErrorMessage}",
                    jobId, ex.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during external API call for JobId: {JobId}. Exception type: {ExceptionType}, Message: {ErrorMessage}",
                    jobId, ex.GetType().Name, ex.Message);
                return null;
            }
        }
        public async Task<ContainerPalletsResponse> GetContainerPalletsAsync(
            Guid containerId,
            int page = 1,
            int pageSize = 10,
            string? searchTerm = null)
        {
            _logger.LogInformation("Getting container pallets for container {ContainerId}, page {Page}, pageSize {PageSize}",
                containerId, page, pageSize);

            try
            {
                // Get container info
                var container = await _dbContext.GIV_Containers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == containerId && !c.IsDeleted);

                if (container == null)
                {
                    return new ContainerPalletsResponse();
                }

                // Build query for pallets from receives linked to this container
                var query = _dbContext.GIV_RM_ReceivePallets
                    .AsNoTracking()
                    .Include(p => p.GIV_RM_Receive)
                        .ThenInclude(r => r.RawMaterial)
                    .Include(p => p.Location)
                    .Include(p => p.RM_ReceivePalletItems)
                    .Where(p => p.GIV_RM_Receive.ContainerId == containerId && !p.IsDeleted);

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    query = query.Where(p =>
                        (p.PalletCode != null && p.PalletCode.ToLower().Contains(searchLower)) ||
                        p.RM_ReceivePalletItems.Any(i => i.ItemCode.ToLower().Contains(searchLower)));
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination and get data
                var pallets = await query
                    .OrderBy(p => p.PalletCode)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ContainerPalletDto
                    {
                        Id = p.Id,
                        ReceiveId = p.GIV_RM_ReceiveId,
                        PalletCode = p.PalletCode,
                        PackSize = p.PackSize,
                        Quantity = p.RM_ReceivePalletItems.Count,
                        QuantityBalance = p.RM_ReceivePalletItems.Count(i => !i.IsReleased),
                        ItemCount = p.RM_ReceivePalletItems.Count,
                        LocationName = p.Location != null ? p.Location.Barcode : null,
                        //Group3 = p.Group3,
                        //Group6 = p.Group6,
                        //Group8 = p.Group8,
                        //Group9 = p.Group9,
                        //NDG = p.NDG,
                        //Scentaurus = p.Scentaurus,
                        MaterialNo = p.GIV_RM_Receive.RawMaterial.MaterialNo,
                        BatchNo = p.GIV_RM_Receive.BatchNo
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} pallets for container {ContainerId}", pallets.Count, containerId);

                return new ContainerPalletsResponse
                {
                    Pallets = pallets,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize,
                    ContainerNo = container.ContainerNo_GW ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting container pallets for container {ContainerId}", containerId);
                return new ContainerPalletsResponse();
            }
        }

        public async Task<List<PalletItemDto>> GetPalletItemsAsync(Guid palletId)
        {
            _logger.LogInformation("Getting pallet items for pallet {PalletId}", palletId);

            try
            {
                var items = await _dbContext.GIV_RM_ReceivePalletItems
                    .AsNoTracking()
                    .Where(i => i.GIV_RM_ReceivePalletId == palletId && !i.IsDeleted)
                    .OrderBy(i => i.ItemCode)
                    .Select(i => new PalletItemDto
                    {
                        Id = i.Id,
                        ItemCode = i.ItemCode,
                        BatchNo = i.BatchNo,
                        ProdDate = i.ProdDate,
                        //DG = i.DG,
                        Remarks = i.Remarks,
                        IsReleased = i.IsReleased
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} items for pallet {PalletId}", items.Count, palletId);
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pallet items for pallet {PalletId}", palletId);
                return new List<PalletItemDto>();
            }
        }
    }
}
