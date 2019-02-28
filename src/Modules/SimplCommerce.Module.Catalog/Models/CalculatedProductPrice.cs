namespace SimplCommerce.Module.Catalog.Models
{
    public class CalculatedProductPrice
    {
        public long Id { get; set; }
        public decimal Price { get; set; }

        public decimal? OldPrice { get; set; }

        public int PercentOfSaving { get; set; }
    }
}
