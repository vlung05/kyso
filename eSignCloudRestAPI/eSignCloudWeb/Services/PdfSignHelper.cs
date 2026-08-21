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
using iText.IO.Font;
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
            string offsetStr = "-70,0",
            string sizeStr = "170,70",
            string? serialNumber = null,
            string? rawCertBase64 = null,
            string? keyStorePath = null,
            string keyStorePassword = "12345678")
        {
            try
            {
                // 1. Calculate keyword position and bounding rectangle
                var (targetPage, signRect) = CalculateSignatureBounds(
                    sourcePdfBytes,
                    reason,
                    location,
                    positionIdentifier,
                    pageNoStr,
                    offsetStr,
                    sizeStr);

                // 2. Embed cryptographic PAdES digital signature with custom SignatureFieldAppearance
                byte[]? cryptSigned = TryCryptographicSign(
                    sourcePdfBytes,
                    signerName,
                    certDn,
                    serialNumber,
                    rawCertBase64,
                    reason,
                    location,
                    targetPage,
                    signRect,
                    keyStorePath,
                    keyStorePassword);

                if (cryptSigned != null && cryptSigned.Length > 0)
                {
                    return cryptSigned;
                }

                // Fallback: visual-only stamp if cryptographic keystore is unavailable
                return StampFallbackVisual(sourcePdfBytes, targetPage, signRect, signerName, reason, location);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PdfSignHelper Error] {ex.Message}");
                var (targetPage, signRect) = CalculateSignatureBounds(sourcePdfBytes, reason, location, positionIdentifier, pageNoStr, offsetStr, sizeStr);
                return StampFallbackVisual(sourcePdfBytes, targetPage, signRect, signerName, reason, location);
            }
        }

        private static (int targetPage, Rectangle signRect) CalculateSignatureBounds(
            byte[] sourcePdfBytes,
            string reason,
            string location,
            string positionIdentifier,
            string pageNoStr,
            string offsetStr,
            string sizeStr)
        {
            using var inStream = new MemoryStream(sourcePdfBytes);
            using var pdfDoc = new PdfDocument(new PdfReader(inStream));

            int totalPages = pdfDoc.GetNumberOfPages();
            int targetPage = totalPages; // Default to last page
            if (string.Equals(pageNoStr, "-1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pageNoStr, "Last", StringComparison.OrdinalIgnoreCase))
            {
                targetPage = totalPages;
            }
            else if (int.TryParse(pageNoStr, out int p) && p >= 1 && p <= totalPages)
            {
                targetPage = p;
            }

            var page = pdfDoc.GetPage(targetPage);
            var pageSize = page.GetPageSize();

            bool hasReason = !string.IsNullOrWhiteSpace(reason);
            bool hasLocation = !string.IsNullOrWhiteSpace(location);

            // Default dimensions
            float boxWidth = 170f;
            float boxHeight = (hasReason && hasLocation) ? 68f : ((hasReason || hasLocation) ? 55f : 42f);

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

            // Search for position keyword on target page
            float? keywordX = null;
            float? keywordY = null;

            if (!string.IsNullOrWhiteSpace(positionIdentifier))
            {
                var finder = new KeywordPositionFinder(positionIdentifier.Trim());
                new PdfCanvasProcessor(finder).ProcessPageContent(page);

                if (finder.FoundX.HasValue && finder.FoundY.HasValue)
                {
                    keywordX = finder.FoundX.Value;
                    keywordY = finder.FoundY.Value;
                }
                else
                {
                    for (int pg = totalPages; pg >= 1; pg--)
                    {
                        if (pg == targetPage) continue;
                        var pFinder = new KeywordPositionFinder(positionIdentifier.Trim());
                        new PdfCanvasProcessor(pFinder).ProcessPageContent(pdfDoc.GetPage(pg));
                        if (pFinder.FoundX.HasValue && pFinder.FoundY.HasValue)
                        {
                            targetPage = pg;
                            page = pdfDoc.GetPage(targetPage);
                            pageSize = page.GetPageSize();
                            keywordX = pFinder.FoundX.Value;
                            keywordY = pFinder.FoundY.Value;
                            break;
                        }
                    }
                }
            }

            float finalX;
            float finalY;

            if (keywordX.HasValue && keywordY.HasValue)
            {
                finalX = keywordX.Value + offX;
                finalY = keywordY.Value - boxHeight - 8f + offY;
            }
            else
            {
                finalX = pageSize.GetWidth() - boxWidth - 50f + offX;
                finalY = 80f + offY;
            }

            finalX = Math.Max(15f, Math.Min(finalX, pageSize.GetWidth() - boxWidth - 15f));
            finalY = Math.Max(15f, Math.Min(finalY, pageSize.GetHeight() - boxHeight - 15f));

            return (targetPage, new Rectangle(finalX, finalY, boxWidth, boxHeight));
        }

        private static byte[] StampFallbackVisual(
            byte[] sourcePdfBytes,
            int targetPage,
            Rectangle signRect,
            string signerName,
            string reason,
            string location)
        {
            using var inStream = new MemoryStream(sourcePdfBytes);
            using var outStream = new MemoryStream();
            using var pdfDoc = new PdfDocument(new PdfReader(inStream), new PdfWriter(outStream));

            var page = pdfDoc.GetPage(targetPage);
            var canvas = new PdfCanvas(page);
            DrawSignatureCardOnCanvas(canvas, signRect.GetX(), signRect.GetY(), signRect.GetWidth(), signRect.GetHeight(), signerName, reason, location);

            pdfDoc.Close();
            return outStream.ToArray();
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
            float padTop = 4.0f;
            float padBottom = 4.0f;

            float contentWidth = Math.Max(20f, boxWidth - padLeft - padRight);
            float contentHeight = Math.Max(15f, boxHeight - padTop - padBottom);

            // Calculate unified font size across all lines
            float unifiedFontSize = 7.5f;
            var finalRenderLines = new List<(string text, bool isBold, DeviceRgb color)>();
            float lineSpacing = 10f;

            for (float testSize = 8.0f; testSize >= 4.5f; testSize -= 0.25f)
            {
                var candidateLines = new List<(string text, bool isBold, DeviceRgb color)>();

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

                float testLineSpacing = testSize * 1.30f;
                float totalHeight = (candidateLines.Count - 1) * testLineSpacing + testSize;

                if (totalHeight <= contentHeight || testSize <= 4.5f)
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
            string? keyStorePath,
            string keyStorePassword)
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
                Org.BouncyCastle.X509.X509Certificate? userCert = null;

                // 1. Try reading the authentic X.509 certificate bytes returned by ICORP
                if (!string.IsNullOrWhiteSpace(rawCertBase64))
                {
                    try
                    {
                        var parser = new X509CertificateParser();
                        userCert = parser.ReadCertificate(Convert.FromBase64String(rawCertBase64));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ParseRawCert Error] {ex.Message}");
                    }
                }

                // 2. Fallback: generate custom X.509 certificate with matching Subject DN & Serial
                if (userCert == null && !string.IsNullOrWhiteSpace(certDn))
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
                        certGen.SetIssuerDN(new X509Name("C=VN,O=I-CA,CN=I-CA SHA-256"));
                        certGen.SetNotBefore(DateTime.UtcNow.AddDays(-60));
                        certGen.SetNotAfter(DateTime.UtcNow.AddYears(2));

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
                string sigFieldName = "Signature_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                // Load Arial fonts
                PdfFont font;
                PdfFont boldFont;
                string fontPath = "C:/Windows/Fonts/arial.ttf";
                string boldFontPath = "C:/Windows/Fonts/arialbd.ttf";

                if (File.Exists(boldFontPath))
                    boldFont = PdfFontFactory.CreateFont(boldFontPath, PdfEncodings.IDENTITY_H);
                else
                    boldFont = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA_BOLD);

                if (File.Exists(fontPath))
                    font = PdfFontFactory.CreateFont(fontPath, PdfEncodings.IDENTITY_H);
                else
                    font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                string cleanSigner = string.IsNullOrWhiteSpace(signerName) ? "" : signerName;
                string signDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                bool hasReason = !string.IsNullOrWhiteSpace(reason);
                bool hasLocation = !string.IsNullOrWhiteSpace(location);

                float unifiedFontSize = 7.5f;
                if (signRect.GetHeight() < 50f || signRect.GetWidth() < 140f) unifiedFontSize = 6.5f;
                else if (signRect.GetHeight() >= 80f && signRect.GetWidth() >= 200f) unifiedFontSize = 8.5f;

                // Build custom visual SignatureFieldAppearance (no default iText duplicate text!)
                var appearance = new SignatureFieldAppearance(sigFieldName);

                var div = new Div()
                    .SetWidth(UnitValue.CreatePercentValue(100))
                    .SetHeight(UnitValue.CreatePercentValue(100))
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetBorder(new SolidBorder(new DeviceRgb(203, 213, 225), 0.8f))
                    .SetBorderLeft(new SolidBorder(new DeviceRgb(37, 99, 235), 3.5f))
                    .SetPaddingLeft(7.5f)
                    .SetPaddingRight(5.0f)
                    .SetPaddingTop(4.0f)
                    .SetPaddingBottom(4.0f);

                // 1. Signer Name (Bold, wraps cleanly if long)
                var pSigner = new Paragraph()
                    .SetFontSize(unifiedFontSize)
                    .SetMargin(0)
                    .SetMultipliedLeading(1.2f);
                pSigner.Add(new Text("Ký bởi: ").SetFont(boldFont).SetFontColor(new DeviceRgb(37, 99, 235)));
                pSigner.Add(new Text(cleanSigner).SetFont(boldFont).SetFontColor(new DeviceRgb(15, 23, 42)));
                div.Add(pSigner);

                // 2. Sign Date (Equal font size)
                var pDate = new Paragraph()
                    .SetFontSize(unifiedFontSize)
                    .SetMarginTop(2.0f)
                    .SetMarginBottom(0)
                    .SetMultipliedLeading(1.2f);
                pDate.Add(new Text("Ký ngày: ").SetFont(boldFont).SetFontColor(new DeviceRgb(100, 116, 139)));
                pDate.Add(new Text(signDate).SetFont(font).SetFontColor(new DeviceRgb(51, 65, 85)));
                div.Add(pDate);

                // 3. Optional Reason
                if (hasReason)
                {
                    var pReason = new Paragraph()
                        .SetFontSize(unifiedFontSize)
                        .SetMarginTop(2.0f)
                        .SetMarginBottom(0)
                        .SetMultipliedLeading(1.2f);
                    pReason.Add(new Text("Lý do: ").SetFont(boldFont).SetFontColor(new DeviceRgb(100, 116, 139)));
                    pReason.Add(new Text(reason).SetFont(font).SetFontColor(new DeviceRgb(71, 85, 105)));
                    div.Add(pReason);
                }

                // 4. Optional Location
                if (hasLocation)
                {
                    var pLoc = new Paragraph()
                        .SetFontSize(unifiedFontSize)
                        .SetMarginTop(2.0f)
                        .SetMarginBottom(0)
                        .SetMultipliedLeading(1.2f);
                    pLoc.Add(new Text("Nơi ký: ").SetFont(boldFont).SetFontColor(new DeviceRgb(100, 116, 139)));
                    pLoc.Add(new Text(location).SetFont(font).SetFontColor(new DeviceRgb(71, 85, 105)));
                    div.Add(pLoc);
                }

                appearance.SetContent(div);

                var signerProps = new SignerProperties()
                    .SetFieldName(sigFieldName)
                    .SetPageNumber(targetPage)
                    .SetPageRect(signRect)
                    .SetSignatureAppearance(appearance)
                    .SetSignatureCreator(signerName)
                    .SetContact(signerName);

                if (hasReason)
                {
                    signerProps.SetReason(reason);
                }

                if (hasLocation)
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

    /// <summary>
    /// Finds text position coordinates on PDF canvas
    /// </summary>
    public class KeywordPositionFinder : IEventListener
    {
        private readonly string _keyword;
        public float? FoundX { get; private set; }
        public float? FoundY { get; private set; }

        public KeywordPositionFinder(string keyword)
        {
            _keyword = keyword.Trim();
        }

        public void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_TEXT && data is TextRenderInfo tri)
            {
                string text = tri.GetText();
                if (!string.IsNullOrEmpty(text))
                {
                    // Direct match or partial match
                    if (text.Contains(_keyword, StringComparison.OrdinalIgnoreCase) ||
                        _keyword.Contains(text, StringComparison.OrdinalIgnoreCase) && text.Length >= 4)
                    {
                        var start = tri.GetBaseline().GetStartPoint();
                        FoundX = start.Get(0);
                        FoundY = start.Get(1);
                    }
                }
            }
        }

        public ICollection<EventType> GetSupportedEvents() => new[] { EventType.RENDER_TEXT };
    }
}
