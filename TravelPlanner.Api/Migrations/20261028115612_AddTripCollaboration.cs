using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TravelPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTripCollaboration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Chat_Message_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Trip_Id_Fk = table.Column<int>(type: "integer", nullable: false),
                    User_Id_Fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Chat_Message_Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Trips_Trip_Id_Fk",
                        column: x => x.Trip_Id_Fk,
                        principalTable: "Trips",
                        principalColumn: "Trip_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Users_User_Id_Fk",
                        column: x => x.User_Id_Fk,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProposalComments",
                columns: table => new
                {
                    Proposal_Comment_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Proposal_Id_Fk = table.Column<int>(type: "integer", nullable: false),
                    User_Id_Fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposalComments", x => x.Proposal_Comment_Id);
                    table.ForeignKey(
                        name: "FK_ProposalComments_Proposals_Proposal_Id_Fk",
                        column: x => x.Proposal_Id_Fk,
                        principalTable: "Proposals",
                        principalColumn: "Proposal_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProposalComments_Users_User_Id_Fk",
                        column: x => x.User_Id_Fk,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripPhotos",
                columns: table => new
                {
                    Trip_Photo_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Caption = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content_Type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Image_Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    Created_At = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Trip_Id_Fk = table.Column<int>(type: "integer", nullable: false),
                    User_Id_Fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPhotos", x => x.Trip_Photo_Id);
                    table.ForeignKey(
                        name: "FK_TripPhotos_Trips_Trip_Id_Fk",
                        column: x => x.Trip_Id_Fk,
                        principalTable: "Trips",
                        principalColumn: "Trip_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripPhotos_Users_User_Id_Fk",
                        column: x => x.User_Id_Fk,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TripPlaces",
                columns: table => new
                {
                    Trip_Place_Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Trip_Id_Fk = table.Column<int>(type: "integer", nullable: false),
                    User_Id_Fk = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TripPlaces", x => x.Trip_Place_Id);
                    table.ForeignKey(
                        name: "FK_TripPlaces_Trips_Trip_Id_Fk",
                        column: x => x.Trip_Id_Fk,
                        principalTable: "Trips",
                        principalColumn: "Trip_Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TripPlaces_Users_User_Id_Fk",
                        column: x => x.User_Id_Fk,
                        principalTable: "Users",
                        principalColumn: "User_Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_Trip_Id_Fk_Chat_Message_Id",
                table: "ChatMessages",
                columns: new[] { "Trip_Id_Fk", "Chat_Message_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_User_Id_Fk",
                table: "ChatMessages",
                column: "User_Id_Fk");

            migrationBuilder.CreateIndex(
                name: "IX_ProposalComments_Proposal_Id_Fk_Proposal_Comment_Id",
                table: "ProposalComments",
                columns: new[] { "Proposal_Id_Fk", "Proposal_Comment_Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ProposalComments_User_Id_Fk",
                table: "ProposalComments",
                column: "User_Id_Fk");

            migrationBuilder.CreateIndex(
                name: "IX_TripPhotos_Trip_Id_Fk",
                table: "TripPhotos",
                column: "Trip_Id_Fk");

            migrationBuilder.CreateIndex(
                name: "IX_TripPhotos_User_Id_Fk",
                table: "TripPhotos",
                column: "User_Id_Fk");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlaces_Trip_Id_Fk",
                table: "TripPlaces",
                column: "Trip_Id_Fk");

            migrationBuilder.CreateIndex(
                name: "IX_TripPlaces_User_Id_Fk",
                table: "TripPlaces",
                column: "User_Id_Fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ProposalComments");

            migrationBuilder.DropTable(
                name: "TripPhotos");

            migrationBuilder.DropTable(
                name: "TripPlaces");
        }
    }
}
