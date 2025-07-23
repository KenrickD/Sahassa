using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.PalletDto;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet.SkuDto;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_FinishedGood.Web;
using WMS.Domain.DTOs.GIV_Invoicing;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.Models;

namespace WMS.Application.Interfaces
{
    public interface IFinishedGoodService
    {
        Task<ApiResponseDto<string>> CreateFinishedGoodAsync(List<FG_ReceiveCreateDto> receivingdto, List<FG_ReceivePalletPhotoUploadDto> photos, string Username, Guid warehouseid);
        Task<List<FinishedGoodDetailsDto>> GetFinishedGoodsAsync();
        Task<PaginatedResult<FinishedGoodDetailsDto>> GetPaginatedFinishedGoodsAsync(
        int start,
        int length,
        string? searchTerm,
        int sortColumn,
        bool sortAscending);
        Task<FinishedGoodGrandTotalsDto> GetFinishedGoodsGrandTotalsAsync();
        Task<ApiResponseDto<string>> UpdateFinishedGoodGroupFieldAsync(Guid finishedGoodId, string fieldName, bool value, string userName);
        Task<FinishedGoodDetailsDto> GetFinishedGoodDetailsByIdAsync(Guid id);
        Task<List<FG_ReceivePalletDetailsDto>> GetPalletsByIdAsync(Guid ReceiveId);
        Task<(bool success, string message, List<string> url)> GetPalletPhotoPathByIdAsync(Guid palletId);
        Task<List<FinishedGoodItemDto>> GetItemsByReceive(Guid receiveId);
        Task<List<FinishedGoodGroupedItemDto>> GetGroupedItemsByReceive(Guid receiveId);
        Task<(List<FG_ReceiveGroupDto> UnassignedGroups, List<SkuDto> Skus)> GetUnassignedPalletsAndSkusAsync();
        Task<List<FG_ReceiveGroupDto>> GetPalletsBySkuAsync(Guid sku);
        Task AssignPalletsToSkuAsync(Guid skuId, List<Guid> palletIds, List<Guid> unassignedReceiveIds);
        Task<List<ReceiveViewDto>> GetReceivesBySkuAsync(Guid skuId);
        Task<FinishedGoodReleaseDto> GetFinishedGoodReleaseDetailsAsync(Guid finishedgoodId);
        Task<ServiceWebResult> ReleaseFinishedGoodAsync(FinishedGoodReleaseSubmitDto FinishedGoodReleaseDto, string UserId);
        Task ProcessFinishedGoodReleases(DateTime today, CancellationToken stoppingToken);
        Task<FinishedGoodDetailsDto> GetFinishedGoodSummaryByIdAsync(Guid id);
        Task<PaginatedResult<FG_ReceiveSummaryDto>> GetPaginatedReceivesByFinishedGoodIdAsync(Guid finishedGoodId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);
        Task<PaginatedResult<FinishedGoodItemDto>> GetPaginatedItemsByReceive(Guid receiveId, int start, int length);
        Task<PaginatedResult<FinishedGoodGroupedItemDto>> GetPaginatedGroupedItemsByReceive(Guid receiveId, int start, int length);
        Task<FG_ReceivePalletItemEditDto> GetItemById(Guid ItemId);
        Task<ApiResponseDto<string>> UpdateItemAsync(FG_ReceivePalletItemEditDto dto);
        Task<FinishedGoodEditDto?> GetEditFinishedGoodDtoAsync(Guid id);
        Task<ApiResponseDto<string>> UpdateFinishedGoodAsync(FinishedGoodEditDto dto, string username);
        Task<FG_ReceiveEditDto?> GetEditReceiveDtoAsync(Guid id);
        Task<ApiResponseDto<string>> UpdateReceiveAsync(FG_ReceiveEditDto dto, string username);
        Task<(byte[] fileContent, string fileName)> GenerateFinishedGoodExcelAsync(DateTime startDate, DateTime endDate);
        Task<(byte[] fileContent, string fileName)> GenerateFinishedGoodWeeklyExcelAsync(DateTime cutoffDate);
        Task<ApiResponseDto<Guid>> CreateFinishedGoodAsync(FinishedGoodCreateWebDto dto);
        Task<PaginatedResult<FG_ReceivePalletDetailsDto>> GetPaginatedPalletsByReceiveIdAsync(Guid receiveId, int start, int length);
        Task<(FG_ReceivePalletDetailsDto Pallet, List<LocationDto> Locations)> GetReceivePalletForEditAsync(Guid palletId);
        Task<ApiResponseDto<string>> UpdatePalletAsync(FG_ReceivePalletEditDto dto);
        Task<ApiResponseDto<string>> UpdatePalletLocationAsync(Guid palletId, Guid? locationId, string username);
        Task<bool> UpdatePalletGroupFieldAsync(Guid palletId, string fieldName, bool value, string userName);
        Task<PaginatedResult<FG_ReleaseDetailsDto>> GetPaginatedReleaseDetailsAsync(
    Guid releaseId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);
        Task<FG_ReleaseDetailsViewDto?> GetReleaseDetailsByIdAsync(Guid releaseId);
        Task<PaginatedResult<FG_ReleaseTableRowDto>> GetPaginatedReleasesByFinishedGoodIdAsync(
    Guid finishedGoodId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);
        Task<List<GroupedPalletCountDto>> GetGroupedPalletCount(DateTime cutoffDate);
        Task<PaginatedResult<JobReleaseTableRowDto>> GetPaginatedJobReleasesAsync(
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);
        Task<JobReleaseDetailsDto?> GetJobReleaseDetailsByJobIdAsync(Guid jobId);
        Task<PaginatedResult<JobReleaseIndividualReleaseDto>> GetPaginatedJobReleaseIndividualReleasesAsync(
    Guid jobId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);
        Task<(byte[] fileContent, string fileName)> ExportJobReleaseToExcelAsync(Guid jobId);
        Task<List<FinishedGoodForJobReleaseDto>> GetAvailableFinishedGoodsForJobReleaseAsync();

        Task<List<JobReleaseInventoryDto>> GetFinishedGoodInventoryForJobReleaseAsync(List<Guid> finishedGoodIds);

        Task<FinishedGoodConflictResponse> GetFinishedGoodReleaseConflictsAsync(Guid finishedGoodId);

        Task<Dictionary<Guid, FinishedGoodConflictResponse>> GetBatchFinishedGoodReleaseConflictsAsync(List<Guid> finishedGoodIds);
    }
}
