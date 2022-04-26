namespace ParserToyRu.Models
{
    public class Product
    {
        public string Region { get; set; }
        public string Breadcrumbs { get; set; }
        public string Name { get; set; }
        public int? Price { get; set; }
        public int? OldPrice { get; set; }
        public string Availability { get; set; }
        public List<Uri> PicturesLinks { get; set; }
        public Uri ProductLink { get; set; }
    }
}
