using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductVariantIdToShoppingCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserId_ProductId",
                table: "ShoppingCarts");

            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "ShoppingCarts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_ProductVariantId",
                table: "ShoppingCarts",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId_ProductId_ProductVariantId",
                table: "ShoppingCarts",
                columns: new[] { "UserId", "ProductId", "ProductVariantId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_ProductVariants_ProductVariantId",
                table: "ShoppingCarts",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_ProductVariants_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropIndex(
                name: "IX_ShoppingCarts_UserId_ProductId_ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "ShoppingCarts");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId_ProductId",
                table: "ShoppingCarts",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }
    }
}
