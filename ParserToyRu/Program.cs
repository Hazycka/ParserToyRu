using ParserToyRu.Service;

namespace ParserToyRu
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Parser toyRuParser = new(@"D:\Temp\C# learning\ParserToyRu\results.csv");
            toyRuParser.CreateHeaders();
            var response1 = await toyRuParser.ParsePagesAsync(new Uri("https://www.toy.ru/catalog/boy_transport/?filterseccode%5B0%5D=transport"), 77000000000);
            var response2 = await toyRuParser.ParsePagesAsync(new Uri("https://www.toy.ru/catalog/boy_transport/?filterseccode%5B0%5D=transport"), 61000001000);
        }
    }
}