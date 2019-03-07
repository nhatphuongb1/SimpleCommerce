using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Catalog.Areas.Catalog.ViewModels;
using SimplCommerce.Module.Catalog.Models;
using SimplCommerce.Module.Catalog.Services;
using SimplCommerce.Module.Core.Areas.Core.ViewModels;
using SimplCommerce.Module.Core.Events;
using SimplCommerce.Module.Core.Models;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.Module.Catalog.Areas.Catalog.Controllers
{
    [Area("Catalog")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ProductController : Controller
    {
        private readonly IMediaService _mediaService;
        private readonly IRepository<Product> _productRepository;
        private readonly IMediator _mediator;
        private readonly IProductPricingService _productPricingService;
        private readonly IRepository<Brand> _brandRepository;
        private readonly IRepository<Vendor> _vendorRepository;

        public ProductController(IRepository<Product> productRepository, IMediaService mediaService, IMediator mediator, IProductPricingService productPricingService,IRepository<Brand> brandRepository, IRepository<Vendor> vendorRepository)
        {
            _productRepository = productRepository;
            _mediaService = mediaService;
            _mediator = mediator;
            _productPricingService = productPricingService;
            _brandRepository = brandRepository;
            _vendorRepository = vendorRepository;
        }

        [HttpGet("product/product-overview")]
        public async Task<IActionResult> ProductOverview(long id)
        {
            var product = await _productRepository.Query()
                .Include(x => x.OptionValues)
                .Include(x => x.ProductLinks).ThenInclude(p => p.LinkedProduct)
                .Include(x => x.ThumbnailImage)
                .Include(x => x.Medias).ThenInclude(m => m.Media)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsPublished);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductDetail
            {
                Id = product.Id,
                Name = product.Name,
                CalculatedProductPrice = _productPricingService.CalculateProductPrice(product),
                IsCallForPricing = product.IsCallForPricing,
                IsAllowToOrder = product.IsAllowToOrder,
                StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                StockQuantity = product.StockQuantity,
                ShortDescription = product.ShortDescription,
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
                BrandId = product.BrandId,
                VendorId = product.VendorId
            };

            MapProductVariantToProductVm(product, model);
            MapProductOptionToProductVm(product, model);
            MapProductImagesToProductVm(product, model);

            return PartialView(model);
        }

        public async Task<IActionResult> ProductDetail(long id)
        {
            var product = _productRepository.Query()
                .Include(x => x.OptionValues)
                .Include(x => x.Categories).ThenInclude(c => c.Category)
                .Include(x => x.AttributeValues).ThenInclude(a => a.Attribute)
                .Include(x => x.ProductLinks).ThenInclude(p => p.LinkedProduct).ThenInclude(m => m.ThumbnailImage)
                .Include(x => x.ThumbnailImage)
                .Include(x => x.Medias).ThenInclude(m => m.Media)
                .FirstOrDefault(x => x.Id == id && x.IsPublished);
            if (product == null)
            {
                return NotFound();
            }

            // Get brand
            var brand = _brandRepository.Query().FirstOrDefault(x => x.Id == product.BrandId);
            var vendor = _vendorRepository.Query().FirstOrDefault(x => x.Id == product.VendorId);

            var model = new ProductDetail
            {
                Id = product.Id,
                Name = product.Name,
                CalculatedProductPrice = _productPricingService.CalculateProductPrice(product),
                IsCallForPricing = product.IsCallForPricing,
                IsAllowToOrder = product.IsAllowToOrder,
                StockTrackingIsEnabled = product.StockTrackingIsEnabled,
                StockQuantity = product.StockQuantity,
                ShortDescription = product.ShortDescription,
                MetaTitle = product.MetaTitle,
                MetaKeywords = product.MetaKeywords,
                MetaDescription = product.MetaDescription,
                Description = product.Description,
                Specification = product.Specification,
                ReviewsCount = product.ReviewsCount,
                RatingAverage = product.RatingAverage,
                //  brand id 
                BrandId = product.BrandId,
                
                BrandName = brand==null?"Unknown":brand.Name,
                BrandSlug = brand==null? "Unknown": brand.Slug,
                //  vendor id
                VendorId = product.VendorId,
                VendorName = vendor==null?"Unknown":vendor.Name,
                VendorSlug = vendor==null?"Unknown":vendor.Slug,
                Attributes = product.AttributeValues.Select(x => new ProductDetailAttribute { Name = x.Attribute.Name, Value = x.Value }).ToList(),
                Categories = product.Categories.Select(x => new ProductDetailCategory { Id = x.CategoryId, Name = x.Category.Name, Slug = x.Category.Slug }).ToList(),
            };

            MapProductVariantToProductVm(product, model);
            MapRelatedProductToProductVm(product, model);
            MapProductOptionToProductVm(product, model);
            MapProductImagesToProductVm(product, model);

            await _mediator.Publish(new EntityViewed { EntityId = product.Id, EntityTypeId = "Product" });
            _productRepository.SaveChanges();
            // Same brand products
            var brandQuery = _productRepository.Query().Where(x => x.BrandId == product.BrandId && x.IsPublished && x.IsVisibleIndividually).Include(x=>x.ThumbnailImage);
            var brandPoducts = brandQuery
                .Select(x => ProductThumbnail.FromProduct(x))
                .ToList();
            foreach (var item in brandPoducts)
            {
                item.ThumbnailUrl = _mediaService.GetThumbnailUrl(item.ThumbnailImage);
                item.CalculatedProductPrice = _productPricingService.CalculateProductPrice(item);
            }
            model.SameBrandProducts = brandPoducts;
            // Same vendor products
            var vendorQuery = _productRepository.Query().Where(x => x.VendorId == product.VendorId && x.IsPublished && x.IsVisibleIndividually).Include(x => x.ThumbnailImage);
            var vendorProducts = vendorQuery
                .Select(x => ProductThumbnail.FromProduct(x))
                .ToList();
            foreach (var item in vendorProducts)
            {
                item.ThumbnailUrl = _mediaService.GetThumbnailUrl(item.ThumbnailImage);
                item.CalculatedProductPrice = _productPricingService.CalculateProductPrice(item);
            }
            model.SameVendorProducts = vendorProducts;

            return View(model);
        }

        private void MapProductImagesToProductVm(Product product, ProductDetail model)
        {
            model.Images = product.Medias.Where(x => x.Media.MediaType == Core.Models.MediaType.Image).Select(productMedia => new MediaViewModel
            {
                Url = _mediaService.GetMediaUrl(productMedia.Media),
                ThumbnailUrl = _mediaService.GetThumbnailUrl(productMedia.Media)
            }).ToList();
        }

        private void MapProductOptionToProductVm(Product product, ProductDetail model)
        {
            foreach (var item in product.OptionValues)
            {
                var optionValues = JsonConvert.DeserializeObject<IList<ProductOptionValueVm>>(item.Value);
                foreach (var value in optionValues)
                {
                    if (!model.OptionDisplayValues.ContainsKey(value.Key))
                    {
                        model.OptionDisplayValues.Add(value.Key, new ProductOptionDisplay { DisplayType = item.DisplayType, Value = value.Display });
                    }
                }
            }
        }

        private void MapProductVariantToProductVm(Product product, ProductDetail model)
        {
            var variations = _productRepository
                .Query()
                .Include(x => x.OptionCombinations).ThenInclude(o => o.Option)
                .Where(x => x.LinkedProductLinks.Any(link => link.ProductId == product.Id && link.LinkType == ProductLinkType.Super))
                .Where(x => x.IsPublished)
                .ToList();

            foreach (var variation in variations)
            {
                var variationVm = new ProductDetailVariation
                {
                    Id = variation.Id,
                    Name = variation.Name,
                    NormalizedName = variation.NormalizedName,
                    IsAllowToOrder = variation.IsAllowToOrder,
                    IsCallForPricing = variation.IsCallForPricing,
                    StockTrackingIsEnabled = variation.StockTrackingIsEnabled,
                    StockQuantity = variation.StockQuantity,
                    CalculatedProductPrice = _productPricingService.CalculateProductPrice(variation)
                };

                var optionCombinations = variation.OptionCombinations.OrderBy(x => x.SortIndex);
                foreach (var combination in optionCombinations)
                {
                    variationVm.Options.Add(new ProductDetailVariationOption
                    {
                        OptionId = combination.OptionId,
                        OptionName = combination.Option.Name,
                        Value = combination.Value
                    });
                }

                model.Variations.Add(variationVm);
            }
        }

        private void MapRelatedProductToProductVm(Product product, ProductDetail model)
        {
            var publishedProductLinks = product.ProductLinks.Where(x => x.LinkedProduct.IsPublished && (x.LinkType == ProductLinkType.Related || x.LinkType == ProductLinkType.CrossSell));
            foreach (var productLink in publishedProductLinks)
            {
                var linkedProduct = productLink.LinkedProduct;
                var productThumbnail = ProductThumbnail.FromProduct(linkedProduct);

                productThumbnail.ThumbnailUrl = _mediaService.GetThumbnailUrl(linkedProduct.ThumbnailImage);
                productThumbnail.CalculatedProductPrice = _productPricingService.CalculateProductPrice(linkedProduct);

                if (productLink.LinkType == ProductLinkType.Related)
                {
                    model.RelatedProducts.Add(productThumbnail);
                }

                if (productLink.LinkType == ProductLinkType.CrossSell)
                {
                    model.CrossSellProducts.Add(productThumbnail);
                }
            }
        }
    }
}
