using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantIdToSalesItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "SalesItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesItems_ProductVariantId",
                table: "SalesItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalesItems_ProductVariants_ProductVariantId",
                table: "SalesItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesItems_ProductVariants_ProductVariantId",
                table: "SalesItems");

            migrationBuilder.DropIndex(
                name: "IX_SalesItems_ProductVariantId",
                table: "SalesItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "SalesItems");
        }
    }
}
