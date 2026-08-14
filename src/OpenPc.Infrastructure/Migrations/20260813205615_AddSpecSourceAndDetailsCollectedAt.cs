using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenPc.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Detalhes de produto: origem do valor de cada spec (precedência
    /// reference &lt; title &lt; page &lt; manual) e timestamp da coleta da
    /// ficha técnica da página do produto (comando collect-details).
    /// </summary>
    public partial class AddSpecSourceAndDetailsCollectedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "product_attributes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "title");

            migrationBuilder.AddColumn<DateTime>(
                name: "SpecsCollectedAt",
                table: "listings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpecsCollectedAt",
                table: "listings");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "product_attributes");
        }
    }
}
