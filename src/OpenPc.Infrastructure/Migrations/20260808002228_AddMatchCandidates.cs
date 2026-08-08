using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMatchCandidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_match_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreId = table.Column<Guid>(type: "uuid", nullable: false),
                    StoreSku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Similarity = table.Column<double>(type: "double precision", precision: 4, scale: 3, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_match_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_match_candidates_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_product_match_candidates_stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_match_candidates_ProductId",
                table: "product_match_candidates",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_match_candidates_StoreId_StoreSku",
                table: "product_match_candidates",
                columns: new[] { "StoreId", "StoreSku" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_match_candidates");
        }
    }
}
