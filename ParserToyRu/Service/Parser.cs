using System.Text;
using AngleSharp.Html.Parser;
using ParserToyRu.Models;
using AngleSharp.Html.Dom;

namespace ParserToyRu.Service
{
    public class Parser
    {
        public string PathToWriteCSV { get; set; }

        public Parser(string pathToWriteCSV)
        {
            PathToWriteCSV = pathToWriteCSV;
        }

        public async Task<bool> ParsePagesAsync(Uri link, long codeKLADR)
        {
            var document = await GetResponseDocumentAsync(link, codeKLADR);

            int pageCount = Convert.ToInt32(document.QuerySelector("nav>ul.pagination.justify-content-between").QuerySelectorAll("*").Reverse().FirstOrDefault(m => Int32.TryParse(m.TextContent, out int o)).TextContent);

            Task<List<Product>>[] tasks = new Task<List<Product>>[pageCount];
            for(int i = 0; i < pageCount; i++)
            {
                tasks[i] = ParseSectionAsync(new Uri(link.AbsoluteUri + $"&PAGEN_8={i+1}"), codeKLADR);
                // Необходимо тк иначе сервер перестает отвечать, может быть думает что ддос?)
                Thread.Sleep(1000);
            }
            List<Product> resultProducts = new List<Product>();
            foreach(var productList in await Task.WhenAll(tasks))
            {
                resultProducts.AddRange(productList);
            }

            bool isRecorded = await SaveToCsvAsync(resultProducts);

            return isRecorded;
        }

        public async Task<List<Product>> ParseSectionAsync(Uri link, long codeKLADR)
        {
            Console.WriteLine(link.AbsoluteUri + " Try To Get Section Response");

            var document = await GetResponseDocumentAsync(link, codeKLADR);

            Console.WriteLine(link.AbsoluteUri + " Section Response received");

            List<Task<Product>> tasks = new();
            foreach(var product in document.QuerySelectorAll("a.d-block.p-1.product-name.gtm-click"))
            {
                tasks.Add(ParseProductAsync(new Uri("https://www.toy.ru" + product.GetAttribute("href")), codeKLADR));
            }
            List<Product> resultProducts = (await Task.WhenAll(tasks)).ToList();

            Console.WriteLine(link.AbsoluteUri + " Section Done");

            return resultProducts;
        }

        public async Task<Product> ParseProductAsync(Uri link, long codeKLADR)
        {
            var document = await GetResponseDocumentAsync(link, codeKLADR);

            Product product = new Product()
            {
                Region = document.QuerySelector("div.col-12.select-city-link>a").TextContent.Trim(new char[] { '\n', '\t', ' ' }),
                Name = document.QuerySelector("h1.detail-name").TextContent,
                Price = document.QuerySelector("span.price")?.TextContent.PriceToInt(),
                OldPrice = document.QuerySelector("span.old-price")?.TextContent.PriceToInt(),
                Availability = document.All.FirstOrDefault(m => (m.LocalName == "span" && m.ClassName == "ok") || (m.LocalName == "div" && m.ClassName == "net-v-nalichii")).TextContent.Trim(),
                ProductLink = link,
                PicturesLinks = new()
            };
            foreach (var pictLink in document.QuerySelectorAll("div.card-slider-for>div>a"))
            {
                product.PicturesLinks.Add(new(pictLink.GetAttribute("href")));
            }
            foreach (var bc in document.QuerySelector("nav.breadcrumb").QuerySelectorAll("*").Where(m => m.LocalName == "a" || (m.LocalName == "span" && m.ClassName == "breadcrumb-item active d-none d-block")))
            {
                product.Breadcrumbs += bc.TextContent + "/";
            }

            return product;
        }

        private async Task<IHtmlDocument> GetResponseDocumentAsync(Uri link, long codeKLADR)
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Cookie", $"BITRIX_SM_city={codeKLADR}");

            byte[] responseByte = await client.GetByteArrayAsync(link);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            string response = encoding.GetString(responseByte, 0, responseByte.Length);

            HtmlParser hParser = new HtmlParser();
            IHtmlDocument document = await hParser.ParseDocumentAsync(response);

            return document;
        }

        public void CreateHeaders()
        {
            using (StreamWriter wstream = new StreamWriter(PathToWriteCSV, true))
            {
                wstream.WriteLine($"{nameof(Product.Region)};{nameof(Product.Breadcrumbs)};{nameof(Product.Name)};{nameof(Product.Price)};{nameof(Product.OldPrice)};{nameof(Product.Availability)};{nameof(Product.PicturesLinks)};{nameof(Product.ProductLink)}");
            }
        }

        private async Task<bool> SaveToCsvAsync(List<Product> products)
        {
            using (StreamWriter wstream = new StreamWriter(PathToWriteCSV, true))
            {
                foreach(var p in products)
                {
                    await wstream.WriteLineAsync(p.Region + ";" + p.Breadcrumbs + ";" + p.Name + ";" + p.Price + ";" + p.OldPrice + ";" + p.Availability + ";" + String.Join(' ', p.PicturesLinks) + ";" + p.ProductLink);
                }
            }
            return true;
        }
    }

    public static class StringExtension
    {
        public static int PriceToInt(this string str)
        {
            int i = 0;
            int t;
            StringBuilder result = new();
            while (Int32.TryParse(str[i].ToString(), out t) || str[i].Equals(' '))
            {
                if (!str[i].Equals(' '))
                {
                    result.Append(t);
                }
                i++;
            }
            return Convert.ToInt32(result.ToString());
        }
    }
}
