using AutoMapper;
using Ticketing.Bll.Contracts;
using Ticketing.Domain.Entities;

namespace Ticketing.Bll.Mapping;

public sealed class TicketingMappingProfile : Profile
{
    public TicketingMappingProfile()
    {
        CreateMap<TicketType, TicketTypeResponse>()
            .ForCtorParam(nameof(TicketTypeResponse.AvailableQuantity), options => options.MapFrom(source => source.AvailableQuantity));
        CreateMap<Reservation, ReservationResponse>();
        CreateMap<ReservationDetails, ReservationDetailsResponse>();
        CreateMap<ReservationItem, ReservationItemResponse>();
        CreateMap<Ticket, TicketResponse>();
    }
}
