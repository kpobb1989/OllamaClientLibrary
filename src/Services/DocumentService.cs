﻿using NPOI.HSSF.UserModel;
using NPOI.POIFS.FileSystem;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;

using OllamaClientLibrary.Abstractions.Services;

using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UglyToad.PdfPig;

namespace OllamaClientLibrary.Services
{
    internal class DocumentService : IDocumentService
    {
        private readonly IOcrService _ocrService;

        public DocumentService(IOcrService ocrService)
        {
            _ocrService = ocrService;
        }

        public string? GetTextFromWord(Stream stream, string extension)
        {
            if (extension == ".doc")
            {
                var fs = new POIFSFileSystem(stream);

                var extractor = new NPOI.HSSF.Extractor.EventBasedExcelExtractor(fs);
                return extractor.Text;
            }
            else if (extension == ".docx")
            {
                using var doc = new XWPFDocument(stream);
                var stringBuilder = new StringBuilder();
                foreach (var paragraph in doc.Paragraphs)
                {
                    stringBuilder.AppendLine(paragraph.ParagraphText);
                }
                return stringBuilder.ToString();
            }
            else
            {
                throw new NotSupportedException($"File type {extension} is not supported");
            }
        }

        public string? GetTextFromExcel(Stream stream, string extension)
        {
            if (extension == ".xls")
            {
                using var workbook = new HSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }
            else if (extension == ".xlsx")
            {
                using var workbook = new XSSFWorkbook(stream);
                return ExtractTextFromWorkbook(workbook);
            }
            else
            {
                throw new NotSupportedException($"File type {extension} is not supported");
            }
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
            for (int i = 0; i <= sheet.LastRowNum; i++)
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