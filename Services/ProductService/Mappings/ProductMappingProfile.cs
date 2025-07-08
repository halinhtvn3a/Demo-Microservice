using AutoMapper;
using Shared.Models;
using Shared.Models.DTOs;
using Shared.Models.Events;
using ProductService.Protos;
using DtoCreateProductRequest = Shared.Models.DTOs.CreateProductRequest;
using DtoUpdateProductRequest = Shared.Models.DTOs.UpdateProductRequest;
using GrpcCreateProductRequest = ProductService.Protos.CreateProductRequest;
using GrpcUpdateProductRequest = ProductService.Protos.UpdateProductRequest;

namespace ProductService.Mappings;

public class ProductMappingProfile : Profile
{
	public ProductMappingProfile()
	{
		// Entity to DTO mappings
		CreateMap<Product, ProductDto>();

		// Entity to gRPC mappings
		CreateMap<Product, ProductResponse>()
			.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")))
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")));

		// DTO to Entity mappings
		CreateMap<DtoCreateProductRequest, Product>()
			.ForMember(dest => dest.Id, opt => opt.Ignore())
			.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

		CreateMap<DtoUpdateProductRequest, Product>()
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

		// gRPC to Entity mappings
		CreateMap<GrpcCreateProductRequest, Product>()
			.ForMember(dest => dest.Id, opt => opt.Ignore())
			.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

		CreateMap<GrpcUpdateProductRequest, Product>()
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null && !string.IsNullOrEmpty(srcMember.ToString())));

		// Entity to Event mappings
		CreateMap<Product, ProductStockUpdatedEvent>()
			.ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.Id))
			.ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Name))
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
	}
}