using System.Linq;

namespace OllamaClientLibrary.Extensions
{
    static class PageExtensions
    {
        public static bool IsImageBasedPage(this UglyToad.PdfPig.Content.Page page)
        {
            // Calculate text-to-area ratio
            var pageArea = page.Width * page.Height;
            var textCoverage = page.Letters.Count / pageArea;

            return page.GetImages().Any() && textCoverage < 0.0001;
        }
    }
}