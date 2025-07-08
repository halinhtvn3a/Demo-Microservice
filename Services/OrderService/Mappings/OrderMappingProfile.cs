using AutoMapper;
using Shared.Models;
using Shared.Models.DTOs;

namespace OrderService.Mappings;

public class OrderMappingProfile : Profile
{
	public OrderMappingProfile()
	{
		// Entity to DTO mappings
		CreateMap<Order, OrderDto>()
			.ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

		CreateMap<OrderItem, OrderItemDto>();

		// DTO to Entity mappings
		CreateMap<CreateOrderRequest, Order>()
			.ForMember(dest => dest.Id, opt => opt.Ignore())
			.ForMember(dest => dest.OrderNumber, opt => opt.Ignore())
			.ForMember(dest => dest.Status, opt => opt.MapFrom(src => OrderStatus.Pending))
			.ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForMember(dest => dest.TotalAmount, opt => opt.Ignore()) // Calculated separately
			.ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

		CreateMap<CreateOrderItemRequest, OrderItem>()
			.ForMember(dest => dest.Id, opt => opt.Ignore())
			.ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice ?? 0))
			.ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => (src.UnitPrice ?? 0) * src.Quantity));

		CreateMap<UpdateOrderRequest, Order>()
			.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
			.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
	}
}