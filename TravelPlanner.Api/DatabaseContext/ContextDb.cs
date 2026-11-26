using TravelPlanner.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace TravelPlanner.Api.DatabaseContext
{
    public class ContextDb : DbContext
    {
        public ContextDb(DbContextOptions options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripMember> TripMembers { get; set; }
        public DbSet<Invitation> Invitations { get; set; }
        public DbSet<Proposal> Proposals { get; set; }
        public DbSet<Vote> Votes { get; set; }
        public DbSet<ScheduleItem> ScheduleItems { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseShare> ExpenseShares { get; set; }
        public DbSet<Repayment> Repayments { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ProposalComment> ProposalComments { get; set; }
        public DbSet<TripPlace> TripPlaces { get; set; }
        public DbSet<TripPhoto> TripPhotos { get; set; }
    }
}
