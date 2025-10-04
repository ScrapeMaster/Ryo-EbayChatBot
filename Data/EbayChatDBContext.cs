using EbayChatBot.API.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EbayChatBot.API.Data
{
    public class EbayChatDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public EbayChatDbContext(DbContextOptions<EbayChatDbContext> options) : base(options)
        {
        }

        // DbSet declarations
        //public DbSet<User> Users { get; set; }
        public DbSet<Buyer> Buyers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Inquiry> Inquiries { get; set; }
        public DbSet<Issue> Issues { get; set; }
        //public DbSet<SupportTask> SupportTask { get; set; }
        public DbSet<Translation> Translations { get; set; }
        //public DbSet<Team> Teams { get; set; }
        public DbSet<MessageTemplate> MessageTemplates { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<EbayToken> EbayTokens { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<EbayItem> EbayItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Inquiry
            modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.Buyer)
                .WithMany()
                .HasForeignKey(i => i.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inquiry>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Issue
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Order)
                .WithMany()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Buyer)
                .WithMany()
                .HasForeignKey(i => i.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Issue>()
                .HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.Buyer)
                .WithMany()
                .HasForeignKey(o => o.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Order>()
                .HasOne(o => o.Seller)
                .WithMany()
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // SupportTask
            //modelBuilder.Entity<SupportTask>()
            //    .HasOne(t => t.Inquiry)
            //    .WithMany()
            //    .HasForeignKey(t => t.InquiryId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<SupportTask>()
            //    .HasOne(t => t.Issue)
            //    .WithMany()
            //    .HasForeignKey(t => t.IssueId)
            //    .OnDelete(DeleteBehavior.Restrict);

            //modelBuilder.Entity<SupportTask>()
            //    .HasOne(t => t.User)
            //    .WithMany()
            //    .HasForeignKey(t => t.UserId)
            //    .OnDelete(DeleteBehavior.Restrict);

            // Translation
            modelBuilder.Entity<Translation>()
                .HasOne(t => t.Inquiry)
                .WithMany()
                .HasForeignKey(t => t.InquiryId)
                .OnDelete(DeleteBehavior.Cascade);

            // User
            //modelBuilder.Entity<User>()
            //    .HasOne(u => u.Team)
            //    .WithMany()
            //    .HasForeignKey(u => u.TeamId)
            //    .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MessageTemplate>()
                .HasOne(mt => mt.User)
                .WithMany()
                .HasForeignKey(mt => mt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.SenderType)
                .HasConversion<string>();

            modelBuilder.Entity<ChatMessage>()
                .Property(m => m.ReceiverType)
                .HasConversion<string>();

            // Remove unnecessary navigation properties if not needed
            modelBuilder.Entity<ChatMessage>()
                .Ignore(m => m.SenderUser)
                .Ignore(m => m.SenderBuyer)
                .Ignore(m => m.ReceiverUser)
                .Ignore(m => m.ReceiverBuyer);

        }
    }
}
