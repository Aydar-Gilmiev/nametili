using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class LinkExpensesToProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Proposal_Id_Fk",
                table: "Expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Proposal_Id_Fk",
                table: "Expenses",
                column: "Proposal_Id_Fk",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Proposals_Proposal_Id_Fk",
                table: "Expenses",
                column: "Proposal_Id_Fk",
                principalTable: "Proposals",
                principalColumn: "Proposal_Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Proposals_Proposal_Id_Fk",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_Proposal_Id_Fk",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "Proposal_Id_Fk",
                table: "Expenses");
        }
    }
}
