using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBuilds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "builds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_builds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "build_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    ListingId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_build_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_build_items_builds_BuildId",
                        column: x => x.BuildId,
                        principalTable: "builds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_build_items_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_build_items_listings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "listings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_build_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_build_items_BuildId_CategoryId",
                table: "build_items",
                columns: new[] { "BuildId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_build_items_CategoryId",
                table: "build_items",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_build_items_ListingId",
                table: "build_items",
                column: "ListingId");

            migrationBuilder.CreateIndex(
                name: "IX_build_items_ProductId",
                table: "build_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_builds_Slug",
                table: "builds",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "build_items");

            migrationBuilder.DropTable(
                name: "builds");
        }
    }
}
