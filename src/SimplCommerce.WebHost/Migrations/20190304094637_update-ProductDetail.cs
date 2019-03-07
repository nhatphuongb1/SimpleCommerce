using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SimplCommerce.WebHost.Migrations
{
    public partial class updateProductDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Catalog_CalculatedProductPrice",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Price = table.Column<decimal>(nullable: false),
                    OldPrice = table.Column<decimal>(nullable: true),
                    PercentOfSaving = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_CalculatedProductPrice", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Catalog_ProductThumbnail",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(nullable: true),
                    Slug = table.Column<string>(nullable: true),
                    Price = table.Column<decimal>(nullable: false),
                    OldPrice = table.Column<decimal>(nullable: true),
                    SpecialPrice = table.Column<decimal>(nullable: true),
                    IsCallForPricing = table.Column<bool>(nullable: false),
                    IsAllowToOrder = table.Column<bool>(nullable: false),
                    StockQuantity = table.Column<int>(nullable: true),
                    SpecialPriceStart = table.Column<DateTimeOffset>(nullable: true),
                    SpecialPriceEnd = table.Column<DateTimeOffset>(nullable: true),
                    ThumbnailImageId = table.Column<long>(nullable: true),
                    ThumbnailUrl = table.Column<string>(nullable: true),
                    ReviewsCount = table.Column<int>(nullable: false),
                    RatingAverage = table.Column<double>(nullable: true),
                    CalculatedProductPriceId = table.Column<long>(nullable: true),
                    ProductId = table.Column<long>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalog_ProductThumbnail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Catalog_ProductThumbnail_Catalog_CalculatedProductPrice_CalculatedProductPriceId",
                        column: x => x.CalculatedProductPriceId,
                        principalTable: "Catalog_CalculatedProductPrice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Catalog_ProductThumbnail_Catalog_Product_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Catalog_Product",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Catalog_ProductThumbnail_Core_Media_ThumbnailImageId",
                        column: x => x.ThumbnailImageId,
                        principalTable: "Core_Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductThumbnail_CalculatedProductPriceId",
                table: "Catalog_ProductThumbnail",
                column: "CalculatedProductPriceId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductThumbnail_ProductId",
                table: "Catalog_ProductThumbnail",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Catalog_ProductThumbnail_ThumbnailImageId",
                table: "Catalog_ProductThumbnail",
                column: "ThumbnailImageId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Catalog_ProductThumbnail");

            migrationBuilder.DropTable(
                name: "Catalog_CalculatedProductPrice");
        }
    }
}
