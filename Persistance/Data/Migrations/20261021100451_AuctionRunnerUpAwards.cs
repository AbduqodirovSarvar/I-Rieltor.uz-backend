using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistance.Data.Migrations
{
    /// <inheritdoc />
    public partial class AuctionRunnerUpAwards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_AuctionResults_Winner",
                table: "AuctionResults");

            migrationBuilder.AddColumn<bool>(
                name: "OfferToRunnerUp",
                table: "Auctions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AuctionAwards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuctionResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ParticipantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BidId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RespondBy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentDueAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionAwards", x => x.Id);
                    table.CheckConstraint("CK_AuctionAwards_Price", "\"Price\" > 0");
                    table.CheckConstraint("CK_AuctionAwards_Rank", "\"Rank\" BETWEEN 1 AND 2");
                    table.CheckConstraint("CK_AuctionAwards_WinnerBound", "\"Rank\" = 2 OR \"Status\" NOT IN ('OFFERED', 'DECLINED', 'EXPIRED')");
                    table.ForeignKey(
                        name: "FK_AuctionAwards_AuctionParticipants_ParticipantId",
                        column: x => x.ParticipantId,
                        principalTable: "AuctionParticipants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuctionAwards_AuctionResults_AuctionResultId",
                        column: x => x.AuctionResultId,
                        principalTable: "AuctionResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuctionAwards_Bids_BidId",
                        column: x => x.BidId,
                        principalTable: "Bids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuctionResults_Winner",
                table: "AuctionResults",
                sql: "(\"Outcome\" IN ('AWAITING_PAYMENT', 'SOLD')) = (\"WinnerParticipantId\" IS NOT NULL AND \"WinningBidId\" IS NOT NULL AND \"FinalPrice\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAwards_AuctionResultId_Live",
                table: "AuctionAwards",
                column: "AuctionResultId",
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Status\" IN ('OFFERED', 'ACCEPTED')");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAwards_AuctionResultId_ParticipantId",
                table: "AuctionAwards",
                columns: new[] { "AuctionResultId", "ParticipantId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAwards_AuctionResultId_Rank",
                table: "AuctionAwards",
                columns: new[] { "AuctionResultId", "Rank" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAwards_BidId",
                table: "AuctionAwards",
                column: "BidId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionAwards_ParticipantId",
                table: "AuctionAwards",
                column: "ParticipantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionAwards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AuctionResults_Winner",
                table: "AuctionResults");

            migrationBuilder.DropColumn(
                name: "OfferToRunnerUp",
                table: "Auctions");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AuctionResults_Winner",
                table: "AuctionResults",
                sql: "(\"Outcome\" IN ('SOLD', 'WINNER_REFUSED')) = (\"WinnerParticipantId\" IS NOT NULL AND \"WinningBidId\" IS NOT NULL AND \"FinalPrice\" IS NOT NULL)");
        }
    }
}
