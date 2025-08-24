using Microsoft.EntityFrameworkCore;
using WeStay.MessagingService.Models;

namespace WeStay.MessagingService.Data
{
    public class MessagingDbContext : DbContext
    {
        public MessagingDbContext(DbContextOptions<MessagingDbContext> options) : base(options)
        {
        }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationType> ConversationTypes { get; set; }
        public DbSet<ConversationParticipant> ConversationParticipants { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageRead> MessageReads { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.Type)
                .WithMany()
                .HasForeignKey(c => c.TypeId);

            modelBuilder.Entity<ConversationParticipant>()
                .HasOne(cp => cp.Conversation)
                .WithMany(c => c.Participants)
                .HasForeignKey(cp => cp.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MessageRead>()
                .HasOne(mr => mr.Message)
                .WithMany(m => m.MessageReads)
                .HasForeignKey(mr => mr.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create indexes
            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.ConversationGuid)
                .IsUnique();

            modelBuilder.Entity<Conversation>()
                .HasIndex(c => c.UpdatedAt);

            modelBuilder.Entity<ConversationParticipant>()
                .HasIndex(cp => new { cp.ConversationId, cp.UserId })
                .IsUnique();

            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.ConversationId, m.CreatedAt });

            modelBuilder.Entity<MessageRead>()
                .HasIndex(mr => new { mr.MessageId, mr.UserId })
                .IsUnique();

            // Seed data
            modelBuilder.Entity<ConversationType>().HasData(
                new ConversationType { Id = 1, Name = "Direct", Description = "Direct message between two users", CreatedAt = DateTime.UtcNow },
                new ConversationType { Id = 2, Name = "Booking", Description = "Conversation related to a specific booking", CreatedAt = DateTime.UtcNow },
                new ConversationType { Id = 3, Name = "Support", Description = "Conversation with support team", CreatedAt = DateTime.UtcNow },
                new ConversationType { Id = 4, Name = "Group", Description = "Group conversation with multiple participants", CreatedAt = DateTime.UtcNow }
            );
        }
    }
}