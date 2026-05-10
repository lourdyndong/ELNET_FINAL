using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ELNETFINALPROJECT.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    Username = table.Column<string>(type: "TEXT", nullable: false),
                    Password = table.Column<string>(type: "TEXT", nullable: false),
                    Balance = table.Column<decimal>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RegisteredDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProfilePicture = table.Column<byte[]>(type: "BLOB", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: true),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsVerified = table.Column<bool>(type: "INTEGER", nullable: false),
                    TotalSessions = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPlaytimeMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    CurrentGame = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentStation = table.Column<string>(type: "TEXT", nullable: true),
                    SessionStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SessionHourlyRate = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StationNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentUser = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentPlayerId = table.Column<int>(type: "INTEGER", nullable: true),
                    TimeUsedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TimePaidMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPoweredOn = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsUnavailable = table.Column<bool>(type: "INTEGER", nullable: false),
                    SessionStartTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stations_Accounts_CurrentPlayerId",
                        column: x => x.CurrentPlayerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Stations_CurrentPlayerId",
                table: "Stations",
                column: "CurrentPlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Stations");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
