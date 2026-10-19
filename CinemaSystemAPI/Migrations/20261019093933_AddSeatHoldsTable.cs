using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CinemaSystemAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatHoldsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeatHolds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShowtimeId = table.Column<int>(type: "int", nullable: false),
                    SeatId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    SessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    HeldAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "GETDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "Seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Showtimes_ShowtimeId",
                        column: x => x.ShowtimeId,
                        principalTable: "Showtimes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatHolds_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_ExpiresAt",
                table: "SeatHolds",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_SeatId",
                table: "SeatHolds",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_ShowtimeId_SeatId",
                table: "SeatHolds",
                columns: new[] { "ShowtimeId", "SeatId" });

            migrationBuilder.CreateIndex(
                name: "IX_SeatHolds_UserId",
                table: "SeatHolds",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeatHolds");
        }
    }
}
