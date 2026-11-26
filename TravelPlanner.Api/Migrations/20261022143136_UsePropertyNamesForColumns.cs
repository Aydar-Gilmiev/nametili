using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class UsePropertyNamesForColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Trips_TripId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_PayerUserId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseShares_Expenses_ExpenseId",
                table: "ExpenseShares");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseShares_Users_UserId",
                table: "ExpenseShares");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Trips_TripId",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_Trips_TripId",
                table: "Proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_Users_UserId",
                table: "Proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Trips_TripId",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Users_FromUserId",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Users_ToUserId",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Proposals_ProposalId",
                table: "ScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Trips_TripId",
                table: "ScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TripMembers_Trips_TripId",
                table: "TripMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TripMembers_Users_UserId",
                table: "TripMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Users_OwnerUserId",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Proposals_ProposalId",
                table: "Votes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Users_UserId",
                table: "Votes");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Votes",
                newName: "User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "ProposalId",
                table: "Votes",
                newName: "Proposal_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Votes",
                newName: "Vote_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_UserId",
                table: "Votes",
                newName: "IX_Votes_User_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_ProposalId_UserId",
                table: "Votes",
                newName: "IX_Votes_Proposal_Id_Fk_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "Users",
                newName: "Password_Hash");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Users",
                newName: "User_Name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "User_Id");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "Trips",
                newName: "Start_Date");

            migrationBuilder.RenameColumn(
                name: "OwnerUserId",
                table: "Trips",
                newName: "Owner_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Trips",
                newName: "Trip_Name");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                table: "Trips",
                newName: "End_Date");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Trips",
                newName: "Trip_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_OwnerUserId",
                table: "Trips",
                newName: "IX_Trips_Owner_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "TripMembers",
                newName: "User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "TripMembers",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TripMembers",
                newName: "TripMember_Id");

            migrationBuilder.RenameIndex(
                name: "IX_TripMembers_UserId",
                table: "TripMembers",
                newName: "IX_TripMembers_User_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_TripMembers_TripId_UserId",
                table: "TripMembers",
                newName: "IX_TripMembers_Trip_Id_Fk_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "ScheduleItems",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "StartAt",
                table: "ScheduleItems",
                newName: "Start_At");

            migrationBuilder.RenameColumn(
                name: "ProposalId",
                table: "ScheduleItems",
                newName: "Proposal_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "EndAt",
                table: "ScheduleItems",
                newName: "End_At");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ScheduleItems",
                newName: "ScheduleItem_Id");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleItems_TripId_StartAt",
                table: "ScheduleItems",
                newName: "IX_ScheduleItems_Trip_Id_Fk_Start_At");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleItems_ProposalId",
                table: "ScheduleItems",
                newName: "IX_ScheduleItems_Proposal_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "Repayments",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "ToUserId",
                table: "Repayments",
                newName: "To_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "PaidOn",
                table: "Repayments",
                newName: "Paid_On");

            migrationBuilder.RenameColumn(
                name: "FromUserId",
                table: "Repayments",
                newName: "From_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Repayments",
                newName: "Repayment_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_TripId",
                table: "Repayments",
                newName: "IX_Repayments_Trip_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_ToUserId",
                table: "Repayments",
                newName: "IX_Repayments_To_User_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_FromUserId",
                table: "Repayments",
                newName: "IX_Repayments_From_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "VotingClosed",
                table: "Proposals",
                newName: "Voting_Closed");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Proposals",
                newName: "User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "Proposals",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "PlannedCost",
                table: "Proposals",
                newName: "Planned_Cost");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Proposals",
                newName: "Proposal_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Proposals_UserId",
                table: "Proposals",
                newName: "IX_Proposals_User_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_Proposals_TripId",
                table: "Proposals",
                newName: "IX_Proposals_Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "Invitations",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Invitations",
                newName: "Created_At");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Invitations",
                newName: "Invitation_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Invitations_TripId_Email",
                table: "Invitations",
                newName: "IX_Invitations_Trip_Id_Fk_Email");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ExpenseShares",
                newName: "User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "ExpenseId",
                table: "ExpenseShares",
                newName: "Expense_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ExpenseShares",
                newName: "ExpenseShare_Id");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseShares_UserId",
                table: "ExpenseShares",
                newName: "IX_ExpenseShares_User_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseShares_ExpenseId_UserId",
                table: "ExpenseShares",
                newName: "IX_ExpenseShares_Expense_Id_Fk_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "TripId",
                table: "Expenses",
                newName: "Trip_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "PayerUserId",
                table: "Expenses",
                newName: "Payer_User_Id_Fk");

            migrationBuilder.RenameColumn(
                name: "OccurredOn",
                table: "Expenses",
                newName: "Occurred_On");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Expenses",
                newName: "Expense_Id");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_TripId",
                table: "Expenses",
                newName: "IX_Expenses_Trip_Id_Fk");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_PayerUserId",
                table: "Expenses",
                newName: "IX_Expenses_Payer_User_Id_Fk");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Trips_Trip_Id_Fk",
                table: "Expenses",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_Payer_User_Id_Fk",
                table: "Expenses",
                column: "Payer_User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseShares_Expenses_Expense_Id_Fk",
                table: "ExpenseShares",
                column: "Expense_Id_Fk",
                principalTable: "Expenses",
                principalColumn: "Expense_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseShares_Users_User_Id_Fk",
                table: "ExpenseShares",
                column: "User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Trips_Trip_Id_Fk",
                table: "Invitations",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_Trips_Trip_Id_Fk",
                table: "Proposals",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_Users_User_Id_Fk",
                table: "Proposals",
                column: "User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Trips_Trip_Id_Fk",
                table: "Repayments",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Users_From_User_Id_Fk",
                table: "Repayments",
                column: "From_User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Users_To_User_Id_Fk",
                table: "Repayments",
                column: "To_User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Proposals_Proposal_Id_Fk",
                table: "ScheduleItems",
                column: "Proposal_Id_Fk",
                principalTable: "Proposals",
                principalColumn: "Proposal_Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Trips_Trip_Id_Fk",
                table: "ScheduleItems",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripMembers_Trips_Trip_Id_Fk",
                table: "TripMembers",
                column: "Trip_Id_Fk",
                principalTable: "Trips",
                principalColumn: "Trip_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripMembers_Users_User_Id_Fk",
                table: "TripMembers",
                column: "User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Users_Owner_User_Id_Fk",
                table: "Trips",
                column: "Owner_User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Proposals_Proposal_Id_Fk",
                table: "Votes",
                column: "Proposal_Id_Fk",
                principalTable: "Proposals",
                principalColumn: "Proposal_Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Users_User_Id_Fk",
                table: "Votes",
                column: "User_Id_Fk",
                principalTable: "Users",
                principalColumn: "User_Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Trips_Trip_Id_Fk",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Users_Payer_User_Id_Fk",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseShares_Expenses_Expense_Id_Fk",
                table: "ExpenseShares");

            migrationBuilder.DropForeignKey(
                name: "FK_ExpenseShares_Users_User_Id_Fk",
                table: "ExpenseShares");

            migrationBuilder.DropForeignKey(
                name: "FK_Invitations_Trips_Trip_Id_Fk",
                table: "Invitations");

            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_Trips_Trip_Id_Fk",
                table: "Proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_Proposals_Users_User_Id_Fk",
                table: "Proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Trips_Trip_Id_Fk",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Users_From_User_Id_Fk",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_Repayments_Users_To_User_Id_Fk",
                table: "Repayments");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Proposals_Proposal_Id_Fk",
                table: "ScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduleItems_Trips_Trip_Id_Fk",
                table: "ScheduleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_TripMembers_Trips_Trip_Id_Fk",
                table: "TripMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_TripMembers_Users_User_Id_Fk",
                table: "TripMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Trips_Users_Owner_User_Id_Fk",
                table: "Trips");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Proposals_Proposal_Id_Fk",
                table: "Votes");

            migrationBuilder.DropForeignKey(
                name: "FK_Votes_Users_User_Id_Fk",
                table: "Votes");

            migrationBuilder.RenameColumn(
                name: "User_Id_Fk",
                table: "Votes",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Proposal_Id_Fk",
                table: "Votes",
                newName: "ProposalId");

            migrationBuilder.RenameColumn(
                name: "Vote_Id",
                table: "Votes",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_User_Id_Fk",
                table: "Votes",
                newName: "IX_Votes_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Votes_Proposal_Id_Fk_User_Id_Fk",
                table: "Votes",
                newName: "IX_Votes_ProposalId_UserId");

            migrationBuilder.RenameColumn(
                name: "User_Name",
                table: "Users",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Password_Hash",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "User_Id",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "Trip_Name",
                table: "Trips",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Start_Date",
                table: "Trips",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "Owner_User_Id_Fk",
                table: "Trips",
                newName: "OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "End_Date",
                table: "Trips",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "Trip_Id",
                table: "Trips",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Trips_Owner_User_Id_Fk",
                table: "Trips",
                newName: "IX_Trips_OwnerUserId");

            migrationBuilder.RenameColumn(
                name: "User_Id_Fk",
                table: "TripMembers",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "TripMembers",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "TripMember_Id",
                table: "TripMembers",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_TripMembers_User_Id_Fk",
                table: "TripMembers",
                newName: "IX_TripMembers_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_TripMembers_Trip_Id_Fk_User_Id_Fk",
                table: "TripMembers",
                newName: "IX_TripMembers_TripId_UserId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "ScheduleItems",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "Start_At",
                table: "ScheduleItems",
                newName: "StartAt");

            migrationBuilder.RenameColumn(
                name: "Proposal_Id_Fk",
                table: "ScheduleItems",
                newName: "ProposalId");

            migrationBuilder.RenameColumn(
                name: "End_At",
                table: "ScheduleItems",
                newName: "EndAt");

            migrationBuilder.RenameColumn(
                name: "ScheduleItem_Id",
                table: "ScheduleItems",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleItems_Trip_Id_Fk_Start_At",
                table: "ScheduleItems",
                newName: "IX_ScheduleItems_TripId_StartAt");

            migrationBuilder.RenameIndex(
                name: "IX_ScheduleItems_Proposal_Id_Fk",
                table: "ScheduleItems",
                newName: "IX_ScheduleItems_ProposalId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "Repayments",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "To_User_Id_Fk",
                table: "Repayments",
                newName: "ToUserId");

            migrationBuilder.RenameColumn(
                name: "Paid_On",
                table: "Repayments",
                newName: "PaidOn");

            migrationBuilder.RenameColumn(
                name: "From_User_Id_Fk",
                table: "Repayments",
                newName: "FromUserId");

            migrationBuilder.RenameColumn(
                name: "Repayment_Id",
                table: "Repayments",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_Trip_Id_Fk",
                table: "Repayments",
                newName: "IX_Repayments_TripId");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_To_User_Id_Fk",
                table: "Repayments",
                newName: "IX_Repayments_ToUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Repayments_From_User_Id_Fk",
                table: "Repayments",
                newName: "IX_Repayments_FromUserId");

            migrationBuilder.RenameColumn(
                name: "Voting_Closed",
                table: "Proposals",
                newName: "VotingClosed");

            migrationBuilder.RenameColumn(
                name: "User_Id_Fk",
                table: "Proposals",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "Proposals",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "Planned_Cost",
                table: "Proposals",
                newName: "PlannedCost");

            migrationBuilder.RenameColumn(
                name: "Proposal_Id",
                table: "Proposals",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Proposals_User_Id_Fk",
                table: "Proposals",
                newName: "IX_Proposals_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Proposals_Trip_Id_Fk",
                table: "Proposals",
                newName: "IX_Proposals_TripId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "Invitations",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "Created_At",
                table: "Invitations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Invitation_Id",
                table: "Invitations",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Invitations_Trip_Id_Fk_Email",
                table: "Invitations",
                newName: "IX_Invitations_TripId_Email");

            migrationBuilder.RenameColumn(
                name: "User_Id_Fk",
                table: "ExpenseShares",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Expense_Id_Fk",
                table: "ExpenseShares",
                newName: "ExpenseId");

            migrationBuilder.RenameColumn(
                name: "ExpenseShare_Id",
                table: "ExpenseShares",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseShares_User_Id_Fk",
                table: "ExpenseShares",
                newName: "IX_ExpenseShares_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_ExpenseShares_Expense_Id_Fk_User_Id_Fk",
                table: "ExpenseShares",
                newName: "IX_ExpenseShares_ExpenseId_UserId");

            migrationBuilder.RenameColumn(
                name: "Trip_Id_Fk",
                table: "Expenses",
                newName: "TripId");

            migrationBuilder.RenameColumn(
                name: "Payer_User_Id_Fk",
                table: "Expenses",
                newName: "PayerUserId");

            migrationBuilder.RenameColumn(
                name: "Occurred_On",
                table: "Expenses",
                newName: "OccurredOn");

            migrationBuilder.RenameColumn(
                name: "Expense_Id",
                table: "Expenses",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_Trip_Id_Fk",
                table: "Expenses",
                newName: "IX_Expenses_TripId");

            migrationBuilder.RenameIndex(
                name: "IX_Expenses_Payer_User_Id_Fk",
                table: "Expenses",
                newName: "IX_Expenses_PayerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Trips_TripId",
                table: "Expenses",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Users_PayerUserId",
                table: "Expenses",
                column: "PayerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseShares_Expenses_ExpenseId",
                table: "ExpenseShares",
                column: "ExpenseId",
                principalTable: "Expenses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ExpenseShares_Users_UserId",
                table: "ExpenseShares",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Invitations_Trips_TripId",
                table: "Invitations",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_Trips_TripId",
                table: "Proposals",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Proposals_Users_UserId",
                table: "Proposals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Trips_TripId",
                table: "Repayments",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Users_FromUserId",
                table: "Repayments",
                column: "FromUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Repayments_Users_ToUserId",
                table: "Repayments",
                column: "ToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Proposals_ProposalId",
                table: "ScheduleItems",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduleItems_Trips_TripId",
                table: "ScheduleItems",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripMembers_Trips_TripId",
                table: "TripMembers",
                column: "TripId",
                principalTable: "Trips",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TripMembers_Users_UserId",
                table: "TripMembers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_Users_OwnerUserId",
                table: "Trips",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Proposals_ProposalId",
                table: "Votes",
                column: "ProposalId",
                principalTable: "Proposals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Votes_Users_UserId",
                table: "Votes",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
