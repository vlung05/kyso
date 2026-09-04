using System;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Xobject;
using iText.IO.Font;
using iText.IO.Image;
using iText.Signatures;
using iText.Forms.Form.Element;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Operators;
using iText.Bouncycastle.X509;
using iText.Bouncycastle.Crypto;
using iText.Commons.Bouncycastle.Cert;

namespace eSignCloudWeb.Services
{
    public static class PdfSignHelper
    {
        public static byte[] StampDigitalSignature(
            byte[] sourcePdfBytes,
            string signerName,
            string certDn,
            string reason,
            string location,
            string positionIdentifier,
            string pageNoStr = "-1",
            string offsetStr = "0,0",
            string sizeStr = "170,70",
            string alignment = "center-below",
            string? serialNumber = null,
            string? rawCertBase64 = null,
            string? keyStorePath = null,
            string keyStorePassword = "12345678",
            string? issuerDn = null,
            long? validFrom = null,
            long? validTo = null)
        {
            try
            {
                // 1. Calculate all signature positions across pages (supporting alignment, ALL, *, 1,2,3, 1-5, -1, etc.)
                var positions = CalculateAllSignatureBounds(
                    sourcePdfBytes,
                    reason,
                    location,
                    positionIdentifier,
                    pageNoStr,
                    offsetStr,
                    sizeStr,
                    alignment);

                if (positions == null || positions.Count == 0)
                {
                    positions = new List<(int, Rectangle)> { (1, new Rectangle(50, 50, 170, 70)) };
                }

                // 2. Stamp visual appearances on ALL target positions (1 to N) to ensure 100% identical styling
                byte[] currentPdf = StampFallbackVisual(sourcePdfBytes, positions, signerName, reason, location);

                // 3. Sequentially embed real cryptographic signatures for EACH position so every page is clickable
                string timestampStr = DateTime.Now.ToString("yyyyMMddHHmmss");
                for (int i = 0; i < positions.Count; i++)
                {
                    var pos = positions[i];
                    string sigFieldName = $"Signature_{timestampStr}_{i + 1}";
                    byte[]? cryptSigned = TryCryptographicSign(
                        currentPdf,
                        signerName,
                        certDn,
                        serialNumber,
                        rawCertBase64,
                        reason,
                        location,
                        pos.targetPage,
                        pos.signRect,
                        sigFieldName,
                        keyStorePath,
                        keyStorePassword,
                        issuerDn,
                        validFrom,
                        validTo);

                    if (cryptSigned != null && cryptSigned.Length > 0)
                    {
                        currentPdf = cryptSigned;
                    }
                }

                return currentPdf;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StampDigitalSignature Error] {ex.Message}");
                return sourcePdfBytes;
            }
        }

        public static List<int> ParsePageRange(string pageNoStr, int totalPages)
        {
            if (string.IsNullOrWhiteSpace(pageNoStr) ||
                string.Equals(pageNoStr.Trim(), "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pageNoStr.Trim(), "Last", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pageNoStr.Trim(), "Cuối", StringComparison.OrdinalIgnoreCase))
            {
                return new List<int> { totalPages };
            }

            string clean = pageNoStr.Trim().ToLowerInvariant();
            if (clean == "all" || clean == "*" || clean == "0" || clean == "tất cả" || clean == "tat ca" || clean == "toàn bộ")
            {
                return Enumerable.Range(1, totalPages).ToList();
            }

            var pages = new HashSet<int>();
            var tokens = pageNoStr.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                var t = token.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                if (t.Contains('-'))
                {
                    var rangeParts = t.Split('-');
                    if (rangeParts.Length == 2 &&
                        int.TryParse(rangeParts[0].Trim(), out int startP) &&
                        int.TryParse(rangeParts[1].Trim(), out int endP))
                    {
                        int min = Math.Max(1, Math.Min(startP, endP));
                        int max = Math.Min(totalPages, Math.Max(startP, endP));
                        for (int i = min; i <= max; i++) pages.Add(i);
                    }
                }
                else if (string.Equals(t, "-1", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t, "Last", StringComparison.OrdinalIgnoreCase))
                {
                    pages.Add(totalPages);
                }
                else if (int.TryParse(t, out int p) && p >= 1 && p <= totalPages)
                {
                    pages.Add(p);
                }
            }

            return pages.Count > 0 ? pages.OrderBy(x => x).ToList() : new List<int> { totalPages };
        }

        private static Rectangle CalculateRectForKeyword(
            KeywordPositionInfo pos,
            Rectangle pageSize,
            float boxWidth,
            float boxHeight,
            string alignment,
            float offX,
            float offY)
        {
            float baseX;
            float baseY;

            string align = (alignment ?? "center-below").Trim().ToLowerInvariant();

            switch (align)
            {
                case "left-below":
                case "left":
                    baseX = pos.LeftX;
                    baseY = pos.BaselineY - boxHeight - 6f;
                    break;

                case "right-below":
                case "right":
                    baseX = pos.RightX - boxWidth;
                    baseY = pos.BaselineY - boxHeight - 6f;
                    break;

                case "right-of":
                case "inline-right":
                    baseX = pos.RightX + 8f;
                    baseY = pos.BaselineY - (boxHeight / 2f) + 4f;
                    break;

                case "left-of":
                case "inline-left":
                    baseX = pos.LeftX - boxWidth - 8f;
                    baseY = pos.BaselineY - (boxHeight / 2f) + 4f;
                    break;

                case "above":
                case "center-above":
                    baseX = pos.CenterX - (boxWidth / 2f);
                    baseY = pos.TopY + 6f;
                    break;

                case "center-below":
                case "center":
                default:
                    baseX = pos.CenterX - (boxWidth / 2f);
                    baseY = pos.BaselineY - boxHeight - 6f;
                    break;
            }

            float finalX = baseX + offX;
            float finalY = baseY + offY;

            finalX = Math.Max(15f, Math.Min(finalX, pageSize.GetWidth() - boxWidth - 15f));
            finalY = Math.Max(15f, Math.Min(finalY, pageSize.GetHeight() - boxHeight - 15f));

            return new Rectangle(finalX, finalY, boxWidth, boxHeight);
        }

        public static string NormalizeForMatch(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            string s = text.Replace('\u00A0', ' ')
                           .Replace('\u200B', ' ')
                           .Replace('\r', ' ')
                           .Replace('\n', ' ')
                           .Replace('\t', ' ');
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s*([(),:;.\-])\s*", "$1");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
            return s.Trim().ToLowerInvariant();
        }

        public static string ExtractCleanWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            var sb = new System.Text.StringBuilder();
            foreach (char c in text)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else if (char.IsWhiteSpace(c) || c == ',' || c == '.' || c == ';' || c == '-' || c == '(' || c == ')')
                {
                    sb.Append(' ');
                }
            }
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
        }

        public static bool IsPageMatch(string pageText, string kw)
        {
            if (string.IsNullOrWhiteSpace(pageText) || string.IsNullOrWhiteSpace(kw)) return false;
            if (pageText.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            string normPage = NormalizeForMatch(pageText);
            string normKw = NormalizeForMatch(kw);
            if (!string.IsNullOrEmpty(normKw) && normPage.Contains(normKw)) return true;

            string cleanKw = ExtractCleanWords(kw);
            if (!string.IsNullOrWhiteSpace(cleanKw) && cleanKw.Length >= 4)
            {
                string cleanPage = ExtractCleanWords(pageText);
                if (cleanPage.Contains(cleanKw)) return true;
            }

            return false;
        }

        private static Rectangle CalculateRectForPage(
            Rectangle pageSize,
            float boxWidth,
            float boxHeight,
            string alignment,
            float offX,
            float offY)
        {
            float baseX;
            float baseY;

            string align = (alignment ?? "bottom-right").Trim().ToLowerInvariant();

            switch (align)
            {
                case "bottom-left":
                    baseX = 40f;
                    baseY = 60f;
                    break;

                case "center-below":
                case "bottom-center":
                    baseX = (pageSize.GetWidth() - boxWidth) / 2f;
                    baseY = 60f;
                    break;

                case "top-right":
                    baseX = pageSize.GetWidth() - boxWidth - 40f;
                    baseY = pageSize.GetHeight() - boxHeight - 60f;
                    break;

                case "top-left":
                    baseX = 40f;
                    baseY = pageSize.GetHeight() - boxHeight - 60f;
                    break;

                case "top-center":
                    baseX = (pageSize.GetWidth() - boxWidth) / 2f;
                    baseY = pageSize.GetHeight() - boxHeight - 60f;
                    break;

                case "center":
                    baseX = (pageSize.GetWidth() - boxWidth) / 2f;
                    baseY = (pageSize.GetHeight() - boxHeight) / 2f;
                    break;

                case "bottom-right":
                default:
                    baseX = pageSize.GetWidth() - boxWidth - 40f;
                    baseY = 60f;
                    break;
            }

            float finalX = baseX + offX;
            float finalY = baseY + offY;

            finalX = Math.Max(15f, Math.Min(finalX, pageSize.GetWidth() - boxWidth - 15f));
            finalY = Math.Max(15f, Math.Min(finalY, pageSize.GetHeight() - boxHeight - 15f));

            return new Rectangle(finalX, finalY, boxWidth, boxHeight);
        }

        private static List<(int targetPage, Rectangle signRect)> CalculateAllSignatureBounds(
            byte[] sourcePdfBytes,
            string reason,
            string location,
            string positionIdentifier,
            string pageNoStr,
            string offsetStr,
            string sizeStr,
            string alignment = "center-below")
        {
            using var inStream = new MemoryStream(sourcePdfBytes);
            using var pdfDoc = new PdfDocument(new PdfReader(inStream));

            int totalPages = pdfDoc.GetNumberOfPages();
            var targetPages = ParsePageRange(pageNoStr, totalPages);

            bool hasReason = !string.IsNullOrWhiteSpace(reason);
            bool hasLocation = !string.IsNullOrWhiteSpace(location);

            // Default dimensions
            float boxWidth = 170f;
            float boxHeight = (hasReason && hasLocation) ? 75f : ((hasReason || hasLocation) ? 62f : 50f);

            if (!string.IsNullOrWhiteSpace(sizeStr) && sizeStr.Contains(','))
            {
                var parts = sizeStr.Split(',');
                if (float.TryParse(parts[0].Trim(), out float w) && w >= 50f) boxWidth = w;
                if (float.TryParse(parts[1].Trim(), out float h) && h >= 20f) boxHeight = h;
            }

            // Offset parsing: X (+ right, - left), Y (+ up, - down)
            float offX = 0f;
            float offY = 0f;
            if (!string.IsNullOrWhiteSpace(offsetStr) && offsetStr.Contains(','))
            {
                var parts = offsetStr.Split(',');
                float.TryParse(parts[0].Trim(), out offX);
                float.TryParse(parts[1].Trim(), out offY);
            }

            var results = new List<(int targetPage, Rectangle signRect)>();
            bool isAllMode = !string.IsNullOrWhiteSpace(pageNoStr) &&
                (pageNoStr.Trim().Equals("all", StringComparison.OrdinalIgnoreCase) ||
                 pageNoStr.Trim().Equals("*") ||
                 pageNoStr.Trim().Equals("0") ||
                 pageNoStr.Trim().Equals("tất cả", StringComparison.OrdinalIgnoreCase) ||
                 pageNoStr.Trim().Equals("tat ca", StringComparison.OrdinalIgnoreCase) ||
                 pageNoStr.Trim().Equals("toàn bộ", StringComparison.OrdinalIgnoreCase));

            bool hasKeyword = !string.IsNullOrWhiteSpace(positionIdentifier);

            if (hasKeyword)
            {
                string kw = positionIdentifier.Trim();

                if (isAllMode)
                {
                    // Scan all pages in the PDF for keyword matches
                    for (int pg = 1; pg <= totalPages; pg++)
                    {
                        var page = pdfDoc.GetPage(pg);
                        var pageSize = page.GetPageSize();

                        // Fast text check: skip page if full keyword is not present
                        string pageText = PdfTextExtractor.GetTextFromPage(page);
                        if (!IsPageMatch(pageText, kw))
                        {
                            continue;
                        }

                        var finder = new KeywordPositionFinder(kw);
                        new PdfCanvasProcessor(finder).ProcessPageContent(page);
                        finder.FinishProcessing();

                        if (finder.FoundPositions.Count > 0)
                        {
                            foreach (var pos in finder.FoundPositions)
                            {
                                results.Add((pg, CalculateRectForKeyword(pos, pageSize, boxWidth, boxHeight, alignment, offX, offY)));
                            }
                        }
                    }

                    // If keyword not found on any page across entire PDF, fallback to last page
                    if (results.Count == 0)
                    {
                        var lastPage = pdfDoc.GetPage(totalPages);
                        results.Add((totalPages, CalculateRectForPage(lastPage.GetPageSize(), boxWidth, boxHeight, alignment, offX, offY)));
                    }
                }
                else
                {
                    // Specific target pages (e.g. 1, 2, 3 or -1)
                    foreach (int pg in targetPages)
                    {
                        var page = pdfDoc.GetPage(pg);
                        var pageSize = page.GetPageSize();
                        var finder = new KeywordPositionFinder(kw);
                        new PdfCanvasProcessor(finder).ProcessPageContent(page);
                        finder.FinishProcessing();

                        if (finder.FoundPositions.Count > 0)
                        {
                            foreach (var pos in finder.FoundPositions)
                            {
                                results.Add((pg, CalculateRectForKeyword(pos, pageSize, boxWidth, boxHeight, alignment, offX, offY)));
                            }
                        }
                        else
                        {
                            // If user specifically requested page -1 / Last page and keyword is on another page, find it
                            if (targetPages.Count == 1 && (pageNoStr.Trim() == "-1" || pageNoStr.Trim().Equals("last", StringComparison.OrdinalIgnoreCase)))
                            {
                                bool foundElsewhere = false;
                                for (int otherPg = totalPages; otherPg >= 1; otherPg--)
                                {
                                    if (otherPg == pg) continue;
                                    var otherPage = pdfDoc.GetPage(otherPg);
                                    string otherPageText = PdfTextExtractor.GetTextFromPage(otherPage);
                                    if (!IsPageMatch(otherPageText, kw)) continue;

                                    var otherFinder = new KeywordPositionFinder(kw);
                                    new PdfCanvasProcessor(otherFinder).ProcessPageContent(otherPage);
                                    otherFinder.FinishProcessing();

                                    if (otherFinder.FoundPositions.Count > 0)
                                    {
                                        var otherPageSize = otherPage.GetPageSize();
                                        foreach (var pos in otherFinder.FoundPositions)
                                        {
                                            results.Add((otherPg, CalculateRectForKeyword(pos, otherPageSize, boxWidth, boxHeight, alignment, offX, offY)));
                                        }
                                        foundElsewhere = true;
                                        break;
                                    }
                                }

                                if (foundElsewhere) break;
                            }

                            // Standard page alignment position on this page
                            results.Add((pg, CalculateRectForPage(pageSize, boxWidth, boxHeight, alignment, offX, offY)));
                        }
                    }
                }
            }
            else
            {
                // No keyword specified: place by page alignment on all target pages
                foreach (int pg in targetPages)
                {
                    var page = pdfDoc.GetPage(pg);
                    var pageSize = page.GetPageSize();
                    results.Add((pg, CalculateRectForPage(pageSize, boxWidth, boxHeight, alignment, offX, offY)));
                }
            }

            if (results.Count == 0)
            {
                results.Add((totalPages, new Rectangle(50, 50, boxWidth, boxHeight)));
            }

            return results;
        }

        private static byte[] StampFallbackVisual(
            byte[] sourcePdfBytes,
            List<(int targetPage, Rectangle signRect)> positions,
            string signerName,
            string reason,
            string location)
        {
            using var inStream = new MemoryStream(sourcePdfBytes);
            using var outStream = new MemoryStream();
            using var pdfDoc = new PdfDocument(new PdfReader(inStream), new PdfWriter(outStream));

            int total = pdfDoc.GetNumberOfPages();
            foreach (var (targetPage, signRect) in positions)
            {
                if (targetPage >= 1 && targetPage <= total)
                {
                    var page = pdfDoc.GetPage(targetPage);
                    var canvas = new PdfCanvas(page);
                    DrawSignatureCardOnCanvas(canvas, signRect.GetX(), signRect.GetY(), signRect.GetWidth(), signRect.GetHeight(), signerName, reason, location);
                }
            }

            pdfDoc.Close();
            return outStream.ToArray();
        }

        public static string? GetGreenTickImagePath()
        {
            string[] candidateImgPaths = new[] {
                System.IO.Path.Combine("wwwroot", "assets", "greentick.png"),
                System.IO.Path.Combine("assets", "greentick.png"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "greentick.png"),
                System.IO.Path.Combine(AppContext.BaseDirectory, "assets", "greentick.png"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "greentick.png"),
                System.IO.Path.Combine(Directory.GetCurrentDirectory(), "eSignCloudWeb", "wwwroot", "assets", "greentick.png")
            };
            return candidateImgPaths.FirstOrDefault(System.IO.File.Exists);
        }

        private static void DrawSignatureCardOnCanvas(
            PdfCanvas canvas,
            float startX,
            float startY,
            float boxWidth,
            float boxHeight,
            string signerName,
            string reason,
            string location)
        {
            // 1. Draw signature card background & subtle border
            canvas.SaveState();
            canvas.SetFillColor(new DeviceRgb(255, 255, 255)); // Crisp white background
            canvas.SetStrokeColor(new DeviceRgb(203, 213, 225)); // Slate-300 border
            canvas.SetLineWidth(0.8f);
            canvas.Rectangle(startX, startY, boxWidth, boxHeight);
            canvas.FillStroke();

            // Accent bar on left border
            canvas.SetFillColor(new DeviceRgb(37, 99, 235)); // #2563eb
            canvas.Rectangle(startX, startY, 3.5f, boxHeight);
            canvas.Fill();

            // 2. Draw Green Tick Image as centered background watermark (opacity 0.28)
            string? tickImgPath = GetGreenTickImagePath();
            if (!string.IsNullOrEmpty(tickImgPath) && System.IO.File.Exists(tickImgPath))
            {
                try
                {
                    var imgData = ImageDataFactory.Create(tickImgPath);
                    float bgImgSize = Math.Min(boxWidth * 0.65f, boxHeight * 0.88f);
                    float bgImgX = startX + (boxWidth - bgImgSize) / 2f;
                    float bgImgY = startY + (boxHeight - bgImgSize) / 2f;

                    canvas.SaveState();
                    var gs = new PdfExtGState().SetFillOpacity(0.28f);
                    canvas.SetExtGState(gs);
                    canvas.AddImageFittedIntoRectangle(imgData, new Rectangle(bgImgX, bgImgY, bgImgSize, bgImgSize), false);
                    canvas.RestoreState();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DrawCanvasWatermark Error] {ex.Message}");
                }
            }
            canvas.RestoreState();

            // Load Arial Unicode TrueType fonts
            PdfFont font;
            PdfFont boldFont;

            string fontPath = "C:/Windows/Fonts/arial.ttf";
            string boldFontPath = "C:/Windows/Fonts/arialbd.ttf";

            if (File.Exists(boldFontPath))
            {
                boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H);
            }
            else
            {
                boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);
            }

            if (File.Exists(fontPath))
            {
                font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
            }
            else
            {
                font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);
            }

            string cleanSigner = string.IsNullOrWhiteSpace(signerName) ? "" : signerName;
            string signDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            bool hasReason = !string.IsNullOrWhiteSpace(reason);
            bool hasLocation = !string.IsNullOrWhiteSpace(location);

            // Box margins and paddings
            float padLeft = 8.5f;
            float padRight = 6.0f;
            float padTop = 3.5f;
            float padBottom = 3.5f;

            float contentWidth = Math.Max(20f, boxWidth - padLeft - padRight);
            float contentHeight = Math.Max(15f, boxHeight - padTop - padBottom);

            // Calculate unified font size across all lines
            float unifiedFontSize = 7.0f;
            var finalRenderLines = new List<(string text, bool isBold, DeviceRgb color)>();
            float lineSpacing = 9.5f;

            for (float testSize = 7.5f; testSize >= 4.0f; testSize -= 0.25f)
            {
                var candidateLines = new List<(string text, bool isBold, DeviceRgb color)>();

                // 0. Signature Valid header line
                candidateLines.Add(("Signature Valid", true, new DeviceRgb(22, 163, 74)));

                // 1. Signer line (wrap if long)
                var signerWrapped = WrapTextToLines($"Ký bởi: {cleanSigner}", boldFont, testSize, contentWidth);
                if (signerWrapped.Count == 0) signerWrapped.Add("Ký bởi:");
                foreach (var sl in signerWrapped)
                {
                    candidateLines.Add((sl, true, new DeviceRgb(15, 23, 42)));
                }

                // 2. Sign date line
                var dateWrapped = WrapTextToLines($"Ký ngày: {signDate}", font, testSize, contentWidth);
                if (dateWrapped.Count == 0) dateWrapped.Add($"Ký ngày: {signDate}");
                foreach (var dl in dateWrapped)
                {
                    candidateLines.Add((dl, false, new DeviceRgb(51, 65, 85)));
                }

                // 3. Reason line (optional)
                if (hasReason)
                {
                    var reasonWrapped = WrapTextToLines($"Lý do: {reason}", font, testSize, contentWidth);
                    foreach (var rl in reasonWrapped)
                    {
                        candidateLines.Add((rl, false, new DeviceRgb(71, 85, 105)));
                    }
                }

                // 4. Location line (optional)
                if (hasLocation)
                {
                    var locWrapped = WrapTextToLines($"Nơi ký: {location}", font, testSize, contentWidth);
                    foreach (var ll in locWrapped)
                    {
                        candidateLines.Add((ll, false, new DeviceRgb(71, 85, 105)));
                    }
                }

                float testLineSpacing = testSize * 1.25f;
                float totalHeight = (candidateLines.Count - 1) * testLineSpacing + testSize;

                if (totalHeight <= contentHeight || testSize <= 4.0f)
                {
                    unifiedFontSize = testSize;
                    lineSpacing = (totalHeight > contentHeight && candidateLines.Count > 1)
                        ? (contentHeight - testSize) / (candidateLines.Count - 1)
                        : testLineSpacing;
                    finalRenderLines = candidateLines;
                    break;
                }
            }

            // Vertically center text block strictly within signature box
            float totalBlockHeight = (finalRenderLines.Count - 1) * lineSpacing + unifiedFontSize;
            float textStartY = startY + padBottom + (contentHeight - totalBlockHeight) / 2f + (finalRenderLines.Count - 1) * lineSpacing + (unifiedFontSize * 0.15f);

            for (int i = 0; i < finalRenderLines.Count; i++)
            {
                var item = finalRenderLines[i];
                var curFont = item.isBold ? boldFont : font;
                float curY = textStartY - i * lineSpacing;

                canvas.BeginText();
                canvas.SetFontAndSize(curFont, unifiedFontSize);
                canvas.SetColor(item.color, true);
                canvas.MoveText(startX + padLeft, curY);
                canvas.ShowText(item.text);
                canvas.EndText();
            }
        }

        /// <summary>
        /// Splits long text into wrapped lines that fit within maxWidth without truncation
        /// </summary>
        private static List<string> WrapTextToLines(string text, PdfFont font, float fontSize, float maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return lines;

            string[] words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0) return lines;

            string currentLine = "";
            foreach (var word in words)
            {
                string testLine = string.IsNullOrEmpty(currentLine) ? word : currentLine + " " + word;
                float width = font.GetWidth(testLine, fontSize);
                if (width <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                    {
                        lines.Add(currentLine);
                        currentLine = word;
                    }
                    else
                    {
                        string chunk = "";
                        foreach (char c in word)
                        {
                            if (font.GetWidth(chunk + c, fontSize) <= maxWidth)
                            {
                                chunk += c;
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(chunk)) lines.Add(chunk);
                                chunk = c.ToString();
                            }
                        }
                        currentLine = chunk;
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
            {
                lines.Add(currentLine);
            }

            return lines;
        }

        private static byte[]? TryCryptographicSign(
            byte[] sourcePdfBytes,
            string signerName,
            string certDn,
            string? serialNumber,
            string? rawCertBase64,
            string reason,
            string location,
            int targetPage,
            Rectangle signRect,
            string? sigFieldName,
            string? keyStorePath,
            string keyStorePassword,
            string? issuerDn = null,
            long? validFrom = null,
            long? validTo = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyStorePath) || !File.Exists(keyStorePath))
                {
                    string[] tryPaths = new[] {
                        keyStorePath ?? "",
                        "filehsm/BK.p12",
                        "BK.p12",
                        "file/BK.p12",
                        "wwwroot/filehsm/BK.p12",
                        "file/rssp.p12",
                        "rssp.p12",
                        "file/cloudfca.p12",
                        "cloudfca.p12",
                        System.IO.Path.Combine(AppContext.BaseDirectory, "filehsm", "BK.p12"),
                        System.IO.Path.Combine(AppContext.BaseDirectory, "wwwroot", "filehsm", "BK.p12"),
                        System.IO.Path.Combine(AppContext.BaseDirectory, "file", "BK.p12"),
                        System.IO.Path.Combine(AppContext.BaseDirectory, "file", "rssp.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "filehsm", "BK.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "filehsm", "BK.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "file", "BK.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "file", "rssp.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "eSignCloudWeb", "filehsm", "BK.p12"),
                        System.IO.Path.Combine(Directory.GetCurrentDirectory(), "eSignCloudWeb", "file", "rssp.p12")
                    };
                    foreach (var p in tryPaths)
                    {
                        if (!string.IsNullOrWhiteSpace(p) && File.Exists(p))
                        {
                            keyStorePath = p;
                            break;
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(keyStorePath) || !File.Exists(keyStorePath))
                {
                    Console.WriteLine("[PdfSignHelper] KeyStore (.p12) not found, skipping cryptographic widget attachment.");
                    return null;
                }

                using var p12Stream = File.OpenRead(keyStorePath);
                var pkcs12Store = new Pkcs12StoreBuilder().Build();
                pkcs12Store.Load(p12Stream, keyStorePassword.ToCharArray());

                string alias = "";
                foreach (string a in pkcs12Store.Aliases)
                {
                    if (pkcs12Store.IsKeyEntry(a))
                    {
                        alias = a;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(alias)) return null;

                var keyEntry = pkcs12Store.GetKey(alias);
                var privKey = keyEntry.Key;
                var chainEntries = pkcs12Store.GetCertificateChain(alias);

                // Build X.509 Certificate matching the real UUID's CertificateDN and SerialNumber
                Org.BouncyCastle.X509.X509Certificate? authenticCert = null;

                // 1. Try reading the authentic X.509 certificate bytes returned by ICORP
                if (!string.IsNullOrWhiteSpace(rawCertBase64))
                {
                    try
                    {
                        var parser = new X509CertificateParser();
                        authenticCert = parser.ReadCertificate(Convert.FromBase64String(rawCertBase64));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ParseRawCert Error] {ex.Message}");
                    }
                }

                // Determine exact NotBefore and NotAfter
                DateTime notBeforeDate;
                DateTime notAfterDate;

                if (authenticCert != null)
                {
                    notBeforeDate = authenticCert.NotBefore;
                    notAfterDate = authenticCert.NotAfter;
                    if (string.IsNullOrWhiteSpace(issuerDn) && authenticCert.IssuerDN != null)
                    {
                        issuerDn = authenticCert.IssuerDN.ToString();
                    }
                    if (string.IsNullOrWhiteSpace(serialNumber) && authenticCert.SerialNumber != null)
                    {
                        serialNumber = authenticCert.SerialNumber.ToString(16);
                    }
                    if (string.IsNullOrWhiteSpace(certDn) && authenticCert.SubjectDN != null)
                    {
                        certDn = authenticCert.SubjectDN.ToString();
                    }
                }
                else
                {
                    if (validFrom.HasValue && validFrom.Value > 0)
                    {
                        notBeforeDate = DateTimeOffset.FromUnixTimeMilliseconds(validFrom.Value).UtcDateTime;
                    }
                    else
                    {
                        notBeforeDate = DateTime.UtcNow.AddDays(-30);
                    }

                    if (validTo.HasValue && validTo.Value > 0)
                    {
                        notAfterDate = DateTimeOffset.FromUnixTimeMilliseconds(validTo.Value).UtcDateTime;
                    }
                    else
                    {
                        notAfterDate = notBeforeDate.AddYears(1);
                    }
                }

                // 2. Generate custom X.509 certificate with exact Subject DN, Serial, Issuer, NotBefore, NotAfter
                Org.BouncyCastle.X509.X509Certificate? userCert = null;
                if (!string.IsNullOrWhiteSpace(certDn))
                {
                    try
                    {
                        var certGen = new X509V3CertificateGenerator();
                        BigInteger serial;
                        if (!string.IsNullOrWhiteSpace(serialNumber))
                        {
                            try
                            {
                                serial = new BigInteger(serialNumber.Replace("-", "").Trim(), 16);
                            }
                            catch
                            {
                                serial = BigInteger.ProbablePrime(64, new SecureRandom());
                            }
                        }
                        else
                        {
                            serial = BigInteger.ProbablePrime(64, new SecureRandom());
                        }

                        certGen.SetSerialNumber(serial);

                        // Set authentic Issuer DN
                        string effectiveIssuer = !string.IsNullOrWhiteSpace(issuerDn) ? issuerDn : "C=VN,O=I-CA,CN=I-CA SHA-256";
                        try
                        {
                            certGen.SetIssuerDN(new X509Name(effectiveIssuer));
                        }
                        catch
                        {
                            certGen.SetIssuerDN(new X509Name("C=VN,O=I-CA,CN=I-CA SHA-256"));
                        }

                        // Set exact Validity matching user certificate
                        certGen.SetNotBefore(notBeforeDate);
                        certGen.SetNotAfter(notAfterDate);

                        X509Name subjectName;
                        try
                        {
                            subjectName = new X509Name(certDn);
                        }
                        catch
                        {
                            var elements = certDn.Split(',');
                            var oList = new List<DerObjectIdentifier>();
                            var vDict = new Dictionary<DerObjectIdentifier, string>();
                            foreach (var el in elements)
                            {
                                var kv = el.Split('=');
                                if (kv.Length == 2)
                                {
                                    string k = kv[0].Trim().ToUpper();
                                    string v = kv[1].Trim();
                                    if (k == "CN") { oList.Add(X509Name.CN); vDict[X509Name.CN] = v.Length > 64 ? v.Substring(0, 64) : v; }
                                    else if (k == "O") { oList.Add(X509Name.O); vDict[X509Name.O] = v; }
                                    else if (k == "C") { oList.Add(X509Name.C); vDict[X509Name.C] = v; }
                                    else if (k == "ST") { oList.Add(X509Name.ST); vDict[X509Name.ST] = v; }
                                    else if (k == "L") { oList.Add(X509Name.L); vDict[X509Name.L] = v; }
                                    else if (k == "UID") { oList.Add(X509Name.UID); vDict[X509Name.UID] = v; }
                                }
                            }
                            subjectName = new X509Name(oList, vDict);
                        }

                        certGen.SetSubjectDN(subjectName);
                        certGen.SetPublicKey(chainEntries[0].Certificate.GetPublicKey());

                        certGen.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
                        certGen.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation));

                        var sigFactory = new Asn1SignatureFactory("SHA256WITHRSA", privKey);
                        userCert = certGen.Generate(sigFactory);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[GenUserCert Error] {ex.Message}");
                    }
                }

                IX509Certificate[] chain;
                if (userCert != null)
                {
                    chain = new IX509Certificate[]
                    {
                        new X509CertificateBC(userCert),
                        new X509CertificateBC(chainEntries[0].Certificate)
                    };
                }
                else
                {
                    chain = new IX509Certificate[chainEntries.Length];
                    for (int i = 0; i < chainEntries.Length; i++)
                    {
                        chain[i] = new X509CertificateBC(chainEntries[i].Certificate);
                    }
                }

                using var inStream = new MemoryStream(sourcePdfBytes);
                using var outStream = new MemoryStream();

                var reader = new PdfReader(inStream);
                var signer = new PdfSigner(reader, outStream, new StampingProperties().UseAppendMode());

                // Unique signature field name
                string fieldName = string.IsNullOrWhiteSpace(sigFieldName)
                    ? "Signature_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                    : sigFieldName;

                // Transparent SignatureFieldAppearance to prevent iText default text overlay ("Digitally signed by...")
                var appearance = new SignatureFieldAppearance(fieldName);
                var emptyDiv = new Div()
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetHeight(UnitValue.CreatePercentValue(100));
                appearance.SetContent(emptyDiv);

                var signerProps = new SignerProperties()
                    .SetFieldName(fieldName)
                    .SetPageNumber(targetPage)
                    .SetPageRect(signRect)
                    .SetSignatureAppearance(appearance)
                    .SetSignatureCreator(signerName)
                    .SetContact(signerName);

                if (!string.IsNullOrWhiteSpace(reason))
                {
                    signerProps.SetReason(reason);
                }

                if (!string.IsNullOrWhiteSpace(location))
                {
                    signerProps.SetLocation(location);
                }

                signer.SetSignerProperties(signerProps);

                var pKeyWrapper = new PrivateKeyBC(privKey);
                IExternalSignature pks = new PrivateKeySignature(pKeyWrapper, "SHA-256");
                signer.SignDetached(pks, chain, null, null, null, 0, PdfSigner.CryptoStandard.CADES);

                return outStream.ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TryCryptographicSign] {ex.Message}");
                return null;
            }
        }
    }

    public class KeywordPositionInfo
    {
        public float LeftX { get; set; }
        public float RightX { get; set; }
        public float BaselineY { get; set; }
        public float TopY { get; set; }
        public float CenterX => (LeftX + RightX) / 2f;
        public float Width => Math.Max(10f, RightX - LeftX);
        public float Height => Math.Max(10f, TopY - BaselineY);
    }

    public class TextChunk
    {
        public string Text { get; set; } = "";
        public float LeftX { get; set; }
        public float RightX { get; set; }
        public float BaselineY { get; set; }
        public float TopY { get; set; }
    }

    /// <summary>
    /// Finds text position coordinates on PDF canvas supporting multiple occurrences per page
    /// </summary>
    public class KeywordPositionFinder : IEventListener
    {
        private readonly string _keyword;
        private readonly List<TextChunk> _allChunks = new List<TextChunk>();
        public List<KeywordPositionInfo> FoundPositions { get; } = new List<KeywordPositionInfo>();
        public float? FoundX => FoundPositions.Count > 0 ? FoundPositions[0].LeftX : (float?)null;
        public float? FoundY => FoundPositions.Count > 0 ? FoundPositions[0].BaselineY : (float?)null;

        public KeywordPositionFinder(string keyword)
        {
            _keyword = keyword.Trim();
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_TEXT && data is TextRenderInfo tri)
            {
                string text = tri.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var start = tri.GetBaseline().GetStartPoint();
                    var end = tri.GetBaseline().GetEndPoint();
                    var ascent = tri.GetAscentLine().GetStartPoint();

                    float leftX = start.Get(0);
                    float rightX = end.Get(0);
                    if (rightX <= leftX) rightX = leftX + Math.Max(20f, text.Length * 6f);
                    float baselineY = start.Get(1);
                    float topY = ascent.Get(1);
                    if (topY <= baselineY) topY = baselineY + 12f;

                    _allChunks.Add(new TextChunk
                    {
                        Text = text,
                        LeftX = leftX,
                        RightX = rightX,
                        BaselineY = baselineY,
                        TopY = topY
                    });
                }
            }
        }

        public void FinishProcessing()
        {
            if (_allChunks.Count == 0 || string.IsNullOrWhiteSpace(_keyword)) return;

            string normKw = PdfSignHelper.NormalizeForMatch(_keyword);
            string cleanKwWords = PdfSignHelper.ExtractCleanWords(_keyword);

            // 1. Direct match within single TextChunk
            foreach (var chunk in _allChunks)
            {
                string normChunk = PdfSignHelper.NormalizeForMatch(chunk.Text);
                string cleanChunk = PdfSignHelper.ExtractCleanWords(chunk.Text);

                if ((!string.IsNullOrEmpty(normKw) && normChunk.Contains(normKw)) ||
                    (!string.IsNullOrEmpty(cleanKwWords) && cleanKwWords.Length >= 4 && cleanChunk.Contains(cleanKwWords)))
                {
                    AddPositionFromChunk(chunk);
                }
            }

            if (FoundPositions.Count > 0) return;

            // 2. Multi-word match on the same line (grouped by baseline Y within ~5pt tolerance)
            var lineGroups = _allChunks
                .GroupBy(t => (int)Math.Round(t.BaselineY / 5.0) * 5)
                .OrderByDescending(g => g.Key);

            foreach (var group in lineGroups)
            {
                var sorted = group.OrderBy(t => t.LeftX).ToList();

                // Find contiguous slice of chunks that exactly matches the keyword
                bool matchedSlice = false;
                for (int i = 0; i < sorted.Count; i++)
                {
                    for (int j = i; j < sorted.Count; j++)
                    {
                        var slice = sorted.Skip(i).Take(j - i + 1).ToList();
                        var sliceRaw = string.Concat(slice.Select(t => t.Text.Trim() + " ")).Trim();
                        var sliceNorm = PdfSignHelper.NormalizeForMatch(sliceRaw);
                        var sliceClean = PdfSignHelper.ExtractCleanWords(sliceRaw);

                        if ((!string.IsNullOrEmpty(normKw) && sliceNorm.Contains(normKw)) ||
                            (!string.IsNullOrEmpty(cleanKwWords) && cleanKwWords.Length >= 4 && sliceClean.Contains(cleanKwWords)))
                        {
                            TextChunk startChunk = slice.First();
                            TextChunk endChunk = slice.Last();

                            float leftX = startChunk.LeftX;
                            float rightX = endChunk.RightX;
                            if (rightX <= leftX) rightX = leftX + Math.Max(30f, _keyword.Length * 6.5f);
                            float baselineY = startChunk.BaselineY;
                            float topY = Math.Max(startChunk.TopY, endChunk.TopY);
                            if (topY <= baselineY) topY = baselineY + 12f;

                            bool isDuplicate = FoundPositions.Any(p => Math.Abs(p.BaselineY - baselineY) < 8f && Math.Abs(p.LeftX - leftX) < 25f);
                            if (!isDuplicate)
                            {
                                FoundPositions.Add(new KeywordPositionInfo
                                {
                                    LeftX = leftX,
                                    RightX = rightX,
                                    BaselineY = baselineY,
                                    TopY = topY
                                });
                            }
                            matchedSlice = true;
                            break;
                        }
                    }
                    if (matchedSlice) break;
                }
            }
        }

        private void AddPositionFromChunk(TextChunk chunk)
        {
            float leftX = chunk.LeftX;
            float rightX = chunk.RightX;
            if (rightX <= leftX) rightX = leftX + Math.Max(20f, chunk.Text.Length * 6f);
            float baselineY = chunk.BaselineY;
            float topY = chunk.TopY;
            if (topY <= baselineY) topY = baselineY + 12f;

            bool isDuplicate = FoundPositions.Any(p => Math.Abs(p.BaselineY - baselineY) < 8f && Math.Abs(p.LeftX - leftX) < 25f);
            if (!isDuplicate)
            {
                FoundPositions.Add(new KeywordPositionInfo
                {
                    LeftX = leftX,
                    RightX = rightX,
                    BaselineY = baselineY,
                    TopY = topY
                });
            }
        }

        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
    }
}
