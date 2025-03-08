using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;

using OllamaClientLibrary.Abstractions.Services;

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NPOI.HWPF;
using NPOI.HWPF.Extractor;

namespace OllamaClientLibrary.Services
{
    internal class DocumentService : IDocumentService
    {
        public string? GetTextFromWord(Stream stream, string extension)
        {
            if (extension == ".doc")
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                var document = new HWPFDocument(stream);
                var wordExtractor = new WordExtractor(document);
                var docText = new StringBuilder();
                foreach (var text in wordExtractor.ParagraphText)
                {
                    docText.AppendLine(text.Trim());
                }

                return docText.ToString();
            }

            if (extension == ".docx")
            {
                var doc = new XWPFDocument(stream);
                var stringBuilder = new StringBuilder();
                foreach (var paragraph in doc.Paragraphs)
                {
                    stringBuilder.AppendLine(paragraph.ParagraphText);
                }
                return stringBuilder.ToString();
            }

            throw new NotSupportedException($"File type {extension} is not supported");
        }

        public string? GetTextFromExcel(Stream stream, string extension)
        {
            if (extension == ".xls")
            {
                var workbook = new HSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }

            if (extension == ".xlsx")
            {
                var workbook = new XSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }

            throw new NotSupportedException($"File type {extension} is not supported");
        }

        public async Task<string?> GetTextAsync(Stream stream)
        {
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        
        private static string? ExtractTextFromWorkbook(NPOI.SS.UserModel.IWorkbook workbook)
        {
            var sheet = workbook.GetSheetAt(0);
            if (sheet == null)
                return null;
            var stringBuilder = new StringBuilder();
            for (var i = 0; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null)
                    continue;
                for (var j = 0; j < row.LastCellNum; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell != null)
                        stringBuilder.Append(cell).Append("\t");
                }
                stringBuilder.AppendLine();
            }
            return stringBuilder.ToString();
        }

    }
}