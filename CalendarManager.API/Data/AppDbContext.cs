using CalendarManager.API.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CalendarManager.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<OAuthToken> OAuthTokens { get; set; }
    public DbSet<UserPreferences> UserPreferences { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<BusinessProfile> BusinessProfiles { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<AvailabilityRule> AvailabilityRules { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Booking> Bookings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.GoogleUserId).HasColumnName("google_user_id");
            entity.Property(e => e.DisplayName).HasColumnName("display_name");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.GoogleUserId).IsUnique();
        });

        // OAuthToken entity configuration
        modelBuilder.Entity<OAuthToken>(entity =>
        {
            entity.ToTable("oauth_tokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AccessToken).HasColumnName("access_token");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.TokenType).HasColumnName("token_type");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.OAuthTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserPreferences entity configuration
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WorkingHoursStart).HasColumnName("working_hours_start");
            entity.Property(e => e.WorkingHoursEnd).HasColumnName("working_hours_end");
            entity.Property(e => e.WorkingDays).HasColumnName("working_days").HasColumnType("jsonb");
            entity.Property(e => e.MeetingBufferMinutes).HasColumnName("meeting_buffer_minutes");
            entity.Property(e => e.PreferredMeetingDuration).HasColumnName("preferred_meeting_duration");
            entity.Property(e => e.Timezone).HasColumnName("timezone");
            entity.Property(e => e.PreferencesText).HasColumnName("preferences_text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithOne(u => u.UserPreferences)
                .HasForeignKey<UserPreferences>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation entity configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Title).HasColumnName("title");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.UpdatedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Conversations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message entity configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.Role).HasColumnName("role");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.FunctionCalls).HasColumnName("function_calls").HasColumnType("jsonb");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // BusinessProfile entity configuration
        modelBuilder.Entity<BusinessProfile>(entity =>
        {
            entity.ToTable("business_profiles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.BusinessName).HasColumnName("business_name");
            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.LogoUrl).HasColumnName("logo_url");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.Website).HasColumnName("website");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithOne(u => u.BusinessProfile)
                .HasForeignKey<BusinessProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Service entity configuration
        modelBuilder.Entity<Service>(entity =>
        {
            entity.ToTable("services");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessProfileId).HasColumnName("business_profile_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Color).HasColumnName("color");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.BusinessProfileId);
            entity.HasIndex(e => new { e.BusinessProfileId, e.IsActive });

            entity.HasOne(e => e.BusinessProfile)
                .WithMany(b => b.Services)
                .HasForeignKey(e => e.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AvailabilityRule entity configuration
        modelBuilder.Entity<AvailabilityRule>(entity =>
        {
            entity.ToTable("availability_rules");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessProfileId).HasColumnName("business_profile_id");
            entity.Property(e => e.RuleType).HasColumnName("rule_type");
            entity.Property(e => e.DayOfWeek).HasColumnName("day_of_week");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.SpecificDate).HasColumnName("specific_date");
            entity.Property(e => e.IsAvailable).HasColumnName("is_available");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.BusinessProfileId);
            entity.HasIndex(e => new { e.BusinessProfileId, e.SpecificDate });

            entity.HasOne(e => e.BusinessProfile)
                .WithMany(b => b.AvailabilityRules)
                .HasForeignKey(e => e.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Client entity configuration
        modelBuilder.Entity<Client>(entity =>
        {
            entity.ToTable("clients");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.Email);
        });

        // Booking entity configuration
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("bookings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BusinessProfileId).HasColumnName("business_profile_id");
            entity.Property(e => e.ServiceId).HasColumnName("service_id");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.GoogleCalendarEventId).HasColumnName("google_calendar_event_id");
            entity.Property(e => e.CancellationToken).HasColumnName("cancellation_token");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.BusinessProfileId);
            entity.HasIndex(e => e.ClientId);
            entity.HasIndex(e => e.CancellationToken).IsUnique();
            entity.HasIndex(e => new { e.BusinessProfileId, e.StartTime });

            entity.HasOne(e => e.BusinessProfile)
                .WithMany(b => b.Bookings)
                .HasForeignKey(e => e.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Service)
                .WithMany(s => s.Bookings)
                .HasForeignKey(e => e.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Client)
                .WithMany(c => c.Bookings)
                .HasForeignKey(e => e.ClientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure automatic timestamp updates will be handled in SaveChanges override
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            // Update UpdatedAt for entities that have this property
            if (entry.Entity is OAuthToken oauthToken && entry.State == EntityState.Modified)
            {
                oauthToken.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is UserPreferences userPrefs && entry.State == EntityState.Modified)
            {
                userPrefs.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Conversation conversation && entry.State == EntityState.Modified)
            {
                conversation.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is BusinessProfile businessProfile && entry.State == EntityState.Modified)
            {
                businessProfile.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Service service && entry.State == EntityState.Modified)
            {
                service.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is AvailabilityRule availabilityRule && entry.State == EntityState.Modified)
            {
                availabilityRule.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Client client && entry.State == EntityState.Modified)
            {
                client.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.Entity is Booking booking && entry.State == EntityState.Modified)
            {
                booking.UpdatedAt = DateTime.UtcNow;
            }

            // Set CreatedAt for new entities
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is BusinessProfile bp)
                    bp.CreatedAt = DateTime.UtcNow;
                else if (entry.Entity is Service svc)
                    svc.CreatedAt = DateTime.UtcNow;
                else if (entry.Entity is AvailabilityRule ar)
                    ar.CreatedAt = DateTime.UtcNow;
                else if (entry.Entity is Client cl)
                    cl.CreatedAt = DateTime.UtcNow;
                else if (entry.Entity is Booking bk)
                    bk.CreatedAt = DateTime.UtcNow;
            }
        }
    }
}