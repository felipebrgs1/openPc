using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPc.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Permite mais de uma peça por categoria em slots multi (memory/storage):
    /// o unique (BuildId, CategoryId) vira índice comum.
    /// </summary>
    public partial class AllowMultipleMemoryStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_build_items_BuildId_CategoryId",
                table: "build_items");

            migrationBuilder.CreateIndex(
                name: "IX_build_items_BuildId_CategoryId",
                table: "build_items",
                columns: new[] { "BuildId", "CategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_build_items_BuildId_CategoryId",
                table: "build_items");

            migrationBuilder.CreateIndex(
                name: "IX_build_items_BuildId_CategoryId",
                table: "build_items",
                columns: new[] { "BuildId", "CategoryId" },
                unique: true);
        }
    }
}
