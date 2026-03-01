using System.ComponentModel.DataAnnotations;
using CalendarManager.API.Models.DTOs;
using FluentAssertions;
using Xunit;

namespace CalendarManager.API.Tests.Models;

public class DtoValidationTests
{
    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    public class CreateEventDtoTests
    {
        [Fact]
        public void CreateEventDto_Validates_RequiredTitle()
        {
            var dto = new CreateEventDto
            {
                Title = "",
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(1)
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("Title"));
        }

        [Fact]
        public void CreateEventDto_Validates_RequiredStart()
        {
            var dto = new CreateEventDto
            {
                Title = "Test Event",
                Start = default,
                End = DateTime.Now.AddHours(1)
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateEventDto_Validates_RequiredEnd()
        {
            var dto = new CreateEventDto
            {
                Title = "Test Event",
                Start = DateTime.Now,
                End = default
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }
    }

    public class CreateBookingDtoTests
    {
        [Fact]
        public void CreateBookingRequest_Validates_RequiredClientName()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "",
                ClientEmail = "test@example.com"
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("ClientName"));
        }

        [Fact]
        public void CreateBookingRequest_Validates_RequiredClientEmail()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = ""
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("ClientEmail"));
        }

        [Fact]
        public void CreateBookingRequest_Validates_EmailFormat()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = "not-an-email"
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("ClientEmail"));
        }

        [Fact]
        public void CreateBookingRequest_Validates_ClientNameMaxLength()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = new string('a', 256),
                ClientEmail = "test@example.com"
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("ClientName"));
        }

        [Fact]
        public void CreateBookingRequest_Validates_ClientEmailMaxLength()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = new string('a', 256) + "@example.com"
            };

            var results = Validate(dto);

            results.Should().Contain(r => r.MemberNames.Contains("ClientEmail"));
        }

        [Fact]
        public void CreateBookingRequest_Validates_NotesMaxLength_2000()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = "test@example.com",
                Notes = new string('a', 2001)
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateBookingRequest_Validates_ValidData()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = "test@example.com",
                Notes = "Some notes"
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateBookingRequest_Validates_PhoneFormat()
        {
            var dto = new CreateBookingDto
            {
                ServiceId = Guid.NewGuid(),
                StartTime = DateTime.Now,
                ClientName = "Test Client",
                ClientEmail = "test@example.com",
                ClientPhone = "invalid-phone-string-with-letters-abc"
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }
    }

    public class CreateBusinessProfileDtoTests
    {
        [Fact]
        public void CreateBusinessProfileDto_Validates_RequiredBusinessName()
        {
            var dto = new CreateBusinessProfileDto
            {
                BusinessName = ""
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateBusinessProfileDto_Validates_ValidData()
        {
            var dto = new CreateBusinessProfileDto
            {
                BusinessName = "Test Business",
                Description = "A test business",
                Phone = "555-1234",
                Website = "https://example.com"
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }
    }

    public class CreateServiceDtoTests
    {
        [Fact]
        public void CreateServiceDto_Validates_RequiredName()
        {
            var dto = new CreateServiceDto
            {
                Name = "",
                DurationMinutes = 60,
                Price = 100
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateServiceDto_Validates_RequiredDurationMinutes()
        {
            var dto = new CreateServiceDto
            {
                Name = "Test Service",
                DurationMinutes = 0,
                Price = 100
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateServiceDto_Validates_ValidData()
        {
            var dto = new CreateServiceDto
            {
                Name = "Test Service",
                Description = "A test service",
                DurationMinutes = 60,
                Price = 100,
                Color = "#3B82F6"
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }
    }

    public class CreateWeeklyAvailabilityDtoTests
    {
        [Fact]
        public void CreateWeeklyAvailabilityDto_Validates_RequiredFields()
        {
            var dto = new CreateWeeklyAvailabilityDto
            {
                DayOfWeek = DayOfWeek.Monday,
                StartTime = TimeSpan.FromHours(9),
                EndTime = TimeSpan.FromHours(17),
                IsAvailable = true
            };

            var results = Validate(dto);

            results.Should().BeEmpty();
        }

        [Fact]
        public void CreateWeeklyAvailabilityDto_Validates_DefaultValues()
        {
            var dto = new CreateWeeklyAvailabilityDto
            {
                DayOfWeek = DayOfWeek.Friday
            };

            dto.IsAvailable.Should().BeTrue();
        }
    }
}
