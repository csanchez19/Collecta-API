using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collecta.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_Collections_CollectionId",
                table: "CatalogItems");

            migrationBuilder.RenameColumn(
                name: "CollectionId",
                table: "CatalogItems",
                newName: "SetId");

            migrationBuilder.RenameIndex(
                name: "IX_CatalogItems_CollectionId",
                table: "CatalogItems",
                newName: "IX_CatalogItems_SetId");

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "Collections",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Collections",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Attributes",
                table: "CatalogItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExternalId",
                table: "CatalogItems",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rarity",
                table: "CatalogItems",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CatalogItems",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "CatalogItems",
                type: "TEXT",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CollectionSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CollectionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Series = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ReleaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    SymbolUrl = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionSets_Collections_CollectionId",
                        column: x => x.CollectionId,
                        principalTable: "Collections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_Source_ExternalId",
                table: "Collections",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogItems_Source_ExternalId",
                table: "CatalogItems",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionSets_CollectionId",
                table: "CollectionSets",
                column: "CollectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionSets_Source_ExternalId",
                table: "CollectionSets",
                columns: new[] { "Source", "ExternalId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_CollectionSets_SetId",
                table: "CatalogItems",
                column: "SetId",
                principalTable: "CollectionSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CatalogItems_CollectionSets_SetId",
                table: "CatalogItems");

            migrationBuilder.DropTable(
                name: "CollectionSets");

            migrationBuilder.DropIndex(
                name: "IX_Collections_Source_ExternalId",
                table: "Collections");

            migrationBuilder.DropIndex(
                name: "IX_CatalogItems_Source_ExternalId",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "Attributes",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "ExternalId",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "Rarity",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CatalogItems");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "CatalogItems");

            migrationBuilder.RenameColumn(
                name: "SetId",
                table: "CatalogItems",
                newName: "CollectionId");

            migrationBuilder.RenameIndex(
                name: "IX_CatalogItems_SetId",
                table: "CatalogItems",
                newName: "IX_CatalogItems_CollectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_CatalogItems_Collections_CollectionId",
                table: "CatalogItems",
                column: "CollectionId",
                principalTable: "Collections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
