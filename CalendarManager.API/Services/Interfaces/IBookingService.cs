using CalendarManager.API.Models.DTOs;

namespace CalendarManager.API.Services.Interfaces;

public interface IBookingService
{
    /// <summary>
    /// Creates a new booking
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="createBooking">Booking details</param>
    /// <returns>Booking confirmation with management URL</returns>
    Task<BookingConfirmationDto> CreateBookingAsync(Guid businessProfileId, CreateBookingDto createBooking);
    
    /// <summary>
    /// Cancels a booking by ID (business owner only)
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>Cancelled booking</returns>
    Task<BookingDto> CancelBookingAsync(Guid businessProfileId, Guid bookingId);
    
    /// <summary>
    /// Cancels a booking by cancellation token (public)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cancelled booking</returns>
    Task<BookingDto> CancelBookingByTokenAsync(Guid cancellationToken);
    
    /// <summary>
    /// Gets a booking by cancellation token
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Booking if found</returns>
    Task<BookingDto?> GetBookingByTokenAsync(Guid cancellationToken);
    
    /// <summary>
    /// Gets all bookings for a business
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="startDate">Optional start date filter</param>
    /// <param name="endDate">Optional end date filter</param>
    /// <returns>List of bookings</returns>
    Task<List<BookingDto>> GetBookingsAsync(Guid businessProfileId, DateTime? startDate = null, DateTime? endDate = null);
    
    /// <summary>
    /// Gets a booking by ID
    /// </summary>
    /// <param name="businessProfileId">Business profile ID</param>
    /// <param name="bookingId">Booking ID</param>
    /// <returns>Booking if found</returns>
    Task<BookingDto?> GetBookingByIdAsync(Guid businessProfileId, Guid bookingId);
}
