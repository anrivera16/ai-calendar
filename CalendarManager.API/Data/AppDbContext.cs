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
        }
    }
}