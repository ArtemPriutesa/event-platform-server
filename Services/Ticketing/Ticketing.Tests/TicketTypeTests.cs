using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Ticketing.Bll.Contracts;
using Ticketing.Bll.Mapping;
using Ticketing.Bll.Validation;
using Ticketing.Domain.Entities;
using Ticketing.Domain.Exceptions;
using Xunit;

namespace Ticketing.Tests;

public sealed class TicketTypeTests
{
    private static readonly Guid EventPublicId = Guid.Parse("a1000000-0000-4000-8000-000000000001");

    [Fact]
    public void ReserveAndSellReserved_UpdateAvailableInventory()
    {
        var ticketType = new TicketType(EventPublicId, "Concert", "Standard", 100m, 10);

        ticketType.Reserve(4);
        ticketType.SellReserved(3);

        Assert.Equal(6, ticketType.AvailableQuantity);
        Assert.Equal(1, ticketType.ReservedQuantity);
        Assert.Equal(3, ticketType.SoldQuantity);
    }

    [Fact]
    public void ReserveMoreThanAvailable_ThrowsConflict()
    {
        var ticketType = new TicketType(EventPublicId, "Concert", "Standard", 100m, 1);

        Assert.Throws<ConflictException>(() => ticketType.Reserve(2));
    }

    [Fact]
    public void ChangeQuantityBelowReservedAndSold_ThrowsDomainException()
    {
        var ticketType = new TicketType(EventPublicId, "Concert", "Standard", 100m, 2);
        ticketType.Reserve(1);
        ticketType.SellReserved(1);

        Assert.Throws<DomainException>(() => ticketType.ChangeQuantity(0));
    }

    [Fact]
    public void MappingProfile_IsValid()
    {
        var configuration = new MapperConfiguration(
            options => options.AddProfile<TicketingMappingProfile>(),
            NullLoggerFactory.Instance);

        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task CreateReservationValidator_RejectsDuplicateTicketTypes()
    {
        var ticketTypeId = 12L;
        var request = new CreateReservationRequest
        {
            UserPublicId = Guid.NewGuid(),
            Contact = new ReservationContactRequest
            {
                Name = "Attendee",
                Email = "attendee@example.com"
            },
            Items =
            [
                new ReservationItemRequest { TicketTypeId = ticketTypeId, Quantity = 1 },
                new ReservationItemRequest { TicketTypeId = ticketTypeId, Quantity = 2 }
            ]
        };
        var validator = new CreateReservationRequestValidator();

        var result = await validator.ValidateAsync(request);

        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("only once", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TicketTypeQueryValidator_AcceptsCaseInsensitiveSortOptions()
    {
        var validator = new TicketTypeQueryValidator();

        var result = await validator.ValidateAsync(new TicketTypeQuery(SortBy: "PRICE", SortDir: "DESC"));

        Assert.True(result.IsValid);
    }
}
