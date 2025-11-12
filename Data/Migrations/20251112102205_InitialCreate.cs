using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PowerAnalysis.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoadReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LoadValue = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    DataSource = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Remarks = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoadReadings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoadReading_DataSource",
                table: "LoadReadings",
                column: "DataSource");

            migrationBuilder.CreateIndex(
                name: "IX_LoadReading_Timestamp",
                table: "LoadReadings",
                column: "Timestamp",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoadReading_Timestamp_DataSource",
                table: "LoadReadings",
                columns: new[] { "Timestamp", "DataSource" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoadReadings");
        }
    }
}
