using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ViewPersonal.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Versions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ReleaseDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Versions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionOsDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OperatingSystem = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    DownloadUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    AppVersionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionOsDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VersionOsDetails_Versions_AppVersionId",
                        column: x => x.AppVersionId,
                        principalTable: "Versions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VersionOsDetails_AppVersionId",
                table: "VersionOsDetails",
                column: "AppVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VersionOsDetails");

            migrationBuilder.DropTable(
                name: "Versions");
        }
    }
}
