using Microsoft.AspNetCore.Http;
using Microsoft.Exchange.WebServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs;
using WMS.Domain.DTOs.Common;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RawMaterial.Import;
using WMS.Domain.DTOs.GIV_RawMaterial.Web;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto.Web;
using WMS.Domain.DTOs.Locations;
using WMS.Domain.DTOs.RawMaterial;
using WMS.Domain.DTOs.Users;
using WMS.Domain.Models;

namespace WMS.Application.Interfaces
{
    public interface IRawMaterialService
    {
        Task<ApiResponseDto<Guid>> CreateRawMaterialAsync(RawMaterialCreateDto rawMaterialReceivingDto, List<RM_ReceivePalletPhotoUploadDto> photos,string Username,Guid warehouseid);
        Task<List<RawMaterialDetailsDto>> GetRawMaterialsAsync();
        Task<RawMaterialDetailsDto> GetRawMaterialDetailsByIdAsync(Guid id);
        Task<List<RM_ReceivePalletDetailsDto>> GetPalletsByIdAsync(Guid ReceiveId);
        Task<(bool success, string message, List<string> urls)> GetPalletPhotoPathByIdAsync(Guid palletId);
        Task<List<RawMaterialItemDto>> GetItemsByReceive(Guid receiveId, bool isGroupedView = false, Guid? groupId = null);
        Task<List<RawMaterialGroupedItemDto>> GetGroupedItemsByReceive(Guid receiveId, bool isGroupedView = false, Guid? groupId = null);
        Task<RawMaterialReleaseDto> GetRawMaterialReleaseDetailsAsync(Guid rawmaterialId);
        Task<ServiceWebResult> ReleaseRawMaterialAsync(RawMaterialReleaseSubmitDto RawMaterialReleaseDto, string UserId);
        System.Threading.Tasks.Task ProcessRawMaterialReleases(DateTime today, CancellationToken stoppingToken);
        Task<PaginatedResult<RawMaterialDetailsDto>> GetPaginatedRawMaterials(
            string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<PaginatedResult<RawMaterialBatchDto>> GetPaginatedRawMaterialsByBatch(
    string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<PaginatedResult<RM_ReceiveTableRowDto>> GetPaginatedReceivesByRawMaterialIdAsync(
        Guid rawMaterialId,
        int skip,
        int take,
        string searchTerm,
        int sortColumn,
        bool sortAscending,
    bool showGrouped
    );
        Task<PaginatedResult<RawMaterialItemDto>> GetPaginatedItemsByReceive(
    Guid receiveId, int start, int length, bool isGroupedView = false, Guid? groupId = null);
        Task<PaginatedResult<RawMaterialGroupedItemDto>> GetPaginatedGroupedItemsByReceive(
    Guid receiveId, int start, int length, bool isGroupedView = false, Guid? groupId = null);
        Task<PaginatedResult<RM_ReceivePalletDetailsDto>> GetPaginatedPalletsByReceiveIdAsync(
    Guid receiveId,
    int start,
    int length,
    string searchTerm = null,
    bool isGroupedView = false,
    Guid? groupId = null);
        Task<(bool IsGrouped, Guid? GroupId)> GetReceiveGroupInfoAsync(Guid receiveId);
        Task<RM_ReceivePalletItemDto> GetItemById(Guid ItemId);
        Task<ApiResponseDto<string>> UpdateItemAsync(RM_ReceivePalletItemDto dto);
        Task<(RM_ReceivePalletDetailsDto Pallet, List<LocationDto> Locations)> GetReceivePalletForEditAsync(Guid palletId);
        Task<ApiResponseDto<string>> UpdatePalletAsync(RM_ReceivePalletEditDto dto);
        Task<RawMaterialEditDto?> GetRawMaterialForEditAsync(Guid id);
        Task<ApiResponseDto<string>> UpdateRawMaterialAsync(RawMaterialEditDto dto, string username);
        Task<RM_ReceiveEditDto?> GetEditDtoAsync(Guid id);
        Task<ApiResponseDto<string>> UpdateReceiveAsync(RM_ReceiveEditDto dto, string username);
        Task<RawMaterialImportResult> ImportRawMaterialsAsync(IFormFile file, Guid warehouseId);
        Task<(byte[] fileContent, string fileName)> GenerateRawMaterialExcelAsync(DateTime startDate, DateTime endDate);
        Task<(byte[] fileContent, string fileName)> GenerateRawMaterialWeeklyExcelAsync(DateTime cutoffDate);
        Task<ApiResponseDto<string>> UpdatePalletLocationAsync(Guid palletId, Guid? locationId, string username);
        Task<bool> UpdatePalletGroupFieldAsync(Guid palletId, string fieldName, bool value, string userName);
        Task<bool> UpdateRawMaterialGroupFieldAsync(Guid rawMaterialId, string fieldName, bool value, string userName);
        Task<ApiResponseDto<Guid>> CreateRawMaterialFromWebAsync(
    RawMaterialCreateWebDto dto,
    List<RM_ReceivePalletPhotoWebUploadDto> photos,
    string username,
    Guid warehouseId);
        Task<List<string>> GetDistinctBatchNumbersAsync();
        Task<List<string>> GetDistinctMaterialNumbersAsync();
        Task<PaginatedResult<RawMaterialPalletDto>> GetPaginatedRawMaterialsByPallet(
    string searchTerm, int skip, int take, int sortColumn, bool sortAscending);
        Task<List<string>> GetPalletCodesAsync();
        Task<RawMaterialTotalsDto> GetRawMaterialTotals();
        Task<List<RawMaterialExportDto>> GetRawMaterialsForExport(string searchTerm);
        Task<PaginatedResult<RM_ReleaseTableRowDto>> GetPaginatedReleasesByRawMaterialIdAsync(
    Guid rawMaterialId,
    int start,
    int length,
    string? searchTerm,
    int sortColumn,
    bool sortAscending);

        Task<RM_ReleaseDetailsViewDto?> GetReleaseDetailsByIdAsync(Guid releaseId);

        Task<PaginatedResult<RM_ReleaseDetailsDto>> GetPaginatedReleaseDetailsAsync(
            Guid releaseId,
            int start,
            int length,
            string? searchTerm,
            int sortColumn,
            bool sortAscending);
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
        Task<List<MaterialForJobReleaseDto>> GetAvailableMaterialsForJobReleaseAsync();

        Task<List<JobReleaseInventoryDto>> GetMaterialInventoryForJobReleaseAsync(List<Guid> materialIds);

        Task<ServiceWebResult> CreateJobReleaseAsync(JobReleaseCreateDto dto, string userId);
    }
}
