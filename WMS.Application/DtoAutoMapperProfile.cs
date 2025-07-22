using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WMS.Domain.DTOs.GeneralCodes;
using WMS.Domain.DTOs.GIV_Container;
using WMS.Domain.DTOs.GIV_FG_Receive;
using WMS.Domain.DTOs.GIV_FG_ReceiveItemPhoto;
using WMS.Domain.DTOs.GIV_FG_ReceivePallet;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_FG_ReceivePalletPhoto;
using WMS.Domain.DTOs.GIV_FinishedGood;
using WMS.Domain.DTOs.GIV_RawMaterial;
using WMS.Domain.DTOs.GIV_RM_Receive;
using WMS.Domain.DTOs.GIV_RM_ReceiveItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePallet;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletItem;
using WMS.Domain.DTOs.GIV_RM_ReceivePalletPhoto;
using WMS.Domain.DTOs.RawMaterial;
using WMS.Domain.Models;

namespace WMS.Application
{
    public class DtoAutoMapperProfile : Profile
    {
        public DtoAutoMapperProfile() 
        {
            CreateMap<RawMaterialCreateDto, GIV_RawMaterial>()
            .ForMember(dest => dest.RM_Receive, opt => opt.MapFrom(src => src.Receives));

            CreateMap<ContainerCreateDto, GIV_Container>();
            CreateMap<RM_ReceiveCreateDto, GIV_RM_Receive>()
    .ForMember(dest => dest.RM_ReceivePallets, opt => opt.MapFrom(src => src.RM_ReceivePallets))
    .ForMember(dest => dest.WarehouseId, opt => opt.Ignore());
            CreateMap<GeneralCodeType, GeneralCodeTypeDto>();
            CreateMap<GeneralCode, GeneralCodeDto>();
            CreateMap<ReceivePalletCreateDto, GIV_RM_ReceivePallet>()
            .ForMember(dest => dest.RM_ReceivePalletItems, opt => opt.MapFrom(src => src.RM_ReceivePalletItems)); ;
            CreateMap<RMReceivePalletPhoto, GIV_RM_ReceivePalletPhoto>();
            CreateMap<ReceivePalletItemCreateDto, GIV_RM_ReceivePalletItem>()
    .ForMember(dest => dest.ItemCode, opt => opt.MapFrom(src => src.ItemCode.Trim()))
    .ForMember(dest => dest.BatchNo, opt => opt.MapFrom(src => src.BatchNo))
    .ForMember(dest => dest.ProdDate, opt => opt.MapFrom(src => src.ProdDate))
    .ForMember(dest => dest.Remarks, opt => opt.MapFrom(src => src.Remarks))
    .ForMember(dest => dest.Id, opt => opt.Ignore())
    .ForMember(dest => dest.GIV_RM_ReceivePalletId, opt => opt.Ignore())
    .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
    .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
    .ForMember(dest => dest.WarehouseId, opt => opt.Ignore())
    .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());


            CreateMap<GIV_RawMaterial, RawMaterialDetailsDto>()
.ForMember(dest => dest.TotalBalanceQty,
    opt => opt.MapFrom(src =>
        src.RM_Receive != null
            ? src.RM_Receive
                .SelectMany(r => r.RM_ReceivePallets ?? new List<GIV_RM_ReceivePallet>())
                .SelectMany(p => p.RM_ReceivePalletItems ?? new List<GIV_RM_ReceivePalletItem>())
                .Count(i => !i.IsReleased)
            : 0
    ))
.ForMember(dest => dest.TotalBalancePallet,
    opt => opt.MapFrom(src =>
        src.RM_Receive != null
            ? src.RM_Receive
                .SelectMany(r => r.RM_ReceivePallets ?? new List<GIV_RM_ReceivePallet>())
                .Where(p => (p.RM_ReceivePalletItems ?? new List<GIV_RM_ReceivePalletItem>())
                                .Any(i => !i.IsReleased))
                .Distinct()
                .Count()
            : 0
    ));


            CreateMap<GIV_Container, Containers>();
            CreateMap<GIV_RM_Receive, RM_ReceiveDetailsDto>()
     .ForMember(dest => dest.RawMaterial, opt => opt.Ignore())
     .ForMember(dest => dest.Containers, opt => opt.MapFrom(src => src.Container));

            CreateMap<GIV_RM_ReceivePallet, RM_ReceivePalletDetailsDto>()
                .ForMember(dest => dest.RM_Receive, opt => opt.Ignore());

            CreateMap<GIV_RM_ReceivePalletItem, RM_ReceivePalletItemDetailsDto>()
                .ForMember(dest => dest.RM_ReceivePallet, opt => opt.Ignore());

            CreateMap<GIV_RM_ReceivePalletPhoto, RM_ReceivePalletPhotoDetailsDto>()
                .ForMember(dest => dest.RM_ReceivePallet, opt => opt.Ignore());


            CreateMap<FinishedGoodCreateDto, GIV_FinishedGood>();
            CreateMap<FG_ReceiveCreateDto, GIV_FG_Receive>();
            CreateMap<FG_ReceivePalletCreateDto, GIV_FG_ReceivePallet>();
            CreateMap<FG_ReceivePalletItemCreateDto, GIV_FG_ReceivePalletItem>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.GIV_FG_ReceivePalletId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.WarehouseId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore());
            CreateMap<FG_ReceivePalletPhotoCreateDto, GIV_FG_ReceivePalletPhoto>();

            CreateMap<GIV_FinishedGood, FinishedGoodDetailsDto>()
    .ForMember(dest => dest.TotalBalancePallet,
        opt => opt.MapFrom(src =>
            src.FG_Receive != null
                ? src.FG_Receive
                    .SelectMany(r => r.FG_ReceivePallets ?? new List<GIV_FG_ReceivePallet>())
                    .Where(p => (p.FG_ReceivePalletItems ?? new List<GIV_FG_ReceivePalletItem>())
                                .Any(i => !i.IsReleased))
                    .Distinct()
                    .Count()
                : 0
        ))
    .ForMember(dest => dest.TotalBalanceQty,
        opt => opt.MapFrom(src =>
            src.FG_Receive != null
                ? src.FG_Receive
                    .SelectMany(r => r.FG_ReceivePallets ?? new List<GIV_FG_ReceivePallet>())
                    .SelectMany(p => p.FG_ReceivePalletItems ?? new List<GIV_FG_ReceivePalletItem>())
                    .Count(i => !i.IsReleased)
                : 0
        ));


            CreateMap<GIV_FG_Receive, FG_ReceiveDetailsDto>()
                .ForMember(dest => dest.FinishedGood, opt => opt.Ignore());

            CreateMap<GIV_FG_ReceivePallet, FG_ReceivePalletDetailsDto>()
                .ForMember(dest => dest.FG_Receive, opt => opt.Ignore());

            CreateMap<GIV_FG_ReceivePalletItem, FG_ReceivePalletItemDetailsDto>()
                .ForMember(dest => dest.FG_ReceivePallet, opt => opt.Ignore());

            CreateMap<GIV_FG_ReceivePalletPhoto, FG_ReceivePalletPhotoDetailsDto>()
                .ForMember(dest => dest.FG_ReceivePallet, opt => opt.Ignore());


            CreateMap<GIV_FinishedGood, FinishedGoodAuditDto>();
            CreateMap<GIV_FG_Receive, FG_ReceiveAuditDto>();
            CreateMap<GIV_FG_ReceivePallet, FG_ReceivePalletAuditDto>();
            CreateMap<GIV_FG_ReceivePalletItem, FG_ReceivePalletItemAuditDto>();
            CreateMap<GIV_FG_ReceivePalletPhoto, FG_ReceivePalletPhotoAuditDto>();

            CreateMap<GIV_RawMaterial, RawMaterialAuditDto>();
            CreateMap<GIV_RM_Receive, RM_ReceiveAuditDto>();
            CreateMap<GIV_RM_ReceivePallet, RM_ReceivePalletAuditDto>();
            CreateMap<GIV_RM_ReceivePalletItem, RM_ReceivePalletItemAuditDto>();
            CreateMap<GIV_RM_ReceivePalletPhoto, RM_ReceivePalletPhotoAuditDto>();

        }
    }
}
