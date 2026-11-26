using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProposalCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Proposals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Proposals",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
