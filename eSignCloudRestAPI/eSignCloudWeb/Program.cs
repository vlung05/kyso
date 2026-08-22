using System.Text.Json;
using eSignCloudWeb.Models;
using eSignCloudWeb.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<HistoryService>();
builder.Services.AddSingleton<ESignCloudService>();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Configure large file uploads
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024; // 100 MB
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseDefaultFiles();
app.UseStaticFiles();

// =================== AUTH ENDPOINTS ===================
app.MapPost("/api/auth/login", async (LoginRequest req, ESignCloudService eSignService, ConfigService configService) =>
{
    if (string.IsNullOrWhiteSpace(req.Uid))
    {
        return Results.BadRequest(new { success = false, message = "Vui lòng nhập UID (agreementUUID)!" });
    }
    if (string.IsNullOrWhiteSpace(req.Passcode))
    {
        return Results.BadRequest(new { success = false, message = "Vui lòng nhập Passcode!" });
    }

    var config = configService.GetConfig();
    bool isAdmin = false;

    // Check if logging in with dedicated Admin username & password
    if (string.Equals(req.Uid.Trim(), config.AdminUsername, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(req.Passcode.Trim(), config.AdminPassword, StringComparison.Ordinal))
    {
        return Results.Ok(new
        {
            success = true,
            message = "Đăng nhập quyền Quản trị viên (Admin) thành công!",
            uid = config.DefaultAgreementUUID,
            signerName = "Quản Trị Viên (Admin)",
            isAdmin = true,
            serverTime = DateTime.UtcNow
        });
    }

    var result = await eSignService.GetCertificateDetailForSignCloudAsync(req.Uid.Trim(), req.Passcode.Trim());
    if (result.success)
    {
        // Automatically save / update customer account in Admin UID Directory
        try
        {
            config.Accounts ??= new List<SignerAccountRecord>();
            var existingAcc = config.Accounts.FirstOrDefault(a => a.AgreementUUID.Equals(req.Uid.Trim(), StringComparison.OrdinalIgnoreCase));
            
            string email = result.response?.email ?? result.response?.agreementDetails?.email ?? "";
            string phone = result.response?.mobileNo ?? result.response?.agreementDetails?.telephoneNumber ?? "";
            string dept = result.response?.agreementDetails?.organizationUnit ?? result.response?.agreementDetails?.organization ?? "";

            if (existingAcc != null)
            {
                if (!string.IsNullOrWhiteSpace(result.signerName)) existingAcc.SignerName = result.signerName;
                if (!string.IsNullOrWhiteSpace(email)) existingAcc.Email = email;
                if (!string.IsNullOrWhiteSpace(phone)) existingAcc.Phone = phone;
                if (!string.IsNullOrWhiteSpace(dept)) existingAcc.Department = dept;
                existingAcc.CertificateDN = result.response?.certificateDN ?? existingAcc.CertificateDN;
                existingAcc.CertificateSerialNumber = result.response?.certificateSerialNumber ?? existingAcc.CertificateSerialNumber;
                existingAcc.DefaultPasscode = req.Passcode.Trim();
                existingAcc.Status = "Hoạt động";
            }
            else
            {
                config.Accounts.Add(new SignerAccountRecord
                {
                    AgreementUUID = req.Uid.Trim(),
                    SignerName = string.IsNullOrWhiteSpace(result.signerName) ? req.Uid.Trim() : result.signerName,
                    Department = dept,
                    Email = email,
                    Phone = phone,
                    DefaultPasscode = req.Passcode.Trim(),
                    Status = "Hoạt động",
                    CreatedDate = DateTime.Now,
                    CertificateDN = result.response?.certificateDN,
                    CertificateSerialNumber = result.response?.certificateSerialNumber
                });
            }
            configService.SaveConfig(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoSaveAccount Error] {ex.Message}");
        }

        // Check if the authenticated UID belongs to Admin group
        if (config.AdminUids != null && config.AdminUids.Any(u => u.Equals(req.Uid.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            isAdmin = true;
        }

        return Results.Ok(new
        {
            success = true,
            message = "Đăng nhập & Xác thực chứng thư thành công!",
            uid = req.Uid.Trim(),
            signerName = result.signerName,
            certificateDN = result.response?.certificateDN,
            certificateSerialNumber = result.response?.certificateSerialNumber,
            isAdmin = isAdmin,
            serverTime = DateTime.UtcNow
        });
    }

    return Results.BadRequest(new
    {
        success = false,
        message = result.message
    });
});

app.MapPost("/api/auth/change-passcode", async (ChangePasscodeRequest req, ESignCloudService eSignService) =>
{
    if (string.IsNullOrWhiteSpace(req.Uid) || string.IsNullOrWhiteSpace(req.CurrentPasscode) || string.IsNullOrWhiteSpace(req.NewPasscode))
    {
        return Results.BadRequest(new { success = false, message = "Thiếu thông tin UID hoặc Passcode." });
    }

    var result = await eSignService.ChangePasscodeAsync(req.Uid, req.CurrentPasscode, req.NewPasscode);
    return result.success
        ? Results.Ok(new { success = true, message = result.message })
        : Results.BadRequest(new { success = false, message = result.message });
});

app.MapPost("/api/auth/forget-passcode", async (ForgetPasscodeRequest req, ESignCloudService eSignService) =>
{
    if (string.IsNullOrWhiteSpace(req.Uid))
    {
        return Results.BadRequest(new { success = false, message = "Thiếu thông tin UID." });
    }

    var result = await eSignService.ForgetPasscodeAsync(req.Uid);
    return result.success
        ? Results.Ok(new { success = true, message = result.message })
        : Results.BadRequest(new { success = false, message = result.message });
});

// =================== SIGNING ENDPOINTS ===================
app.MapPost("/api/sign/upload-and-sign", async (
    HttpRequest request,
    ESignCloudService eSignService,
    ConfigService configService) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { success = false, message = "Request phải là form-data." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    string uid = form["uid"].ToString();
    string passcode = form["passcode"].ToString();

    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { success = false, message = "Vui lòng chọn tài liệu cần ký (PDF hoặc Word)!" });
    }

    if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(passcode))
    {
        return Results.BadRequest(new { success = false, message = "UID và Passcode không được để trống!" });
    }

    // Custom metadata overrides
    var customMeta = new Dictionary<string, string>();
    string[] metaKeys = new[]
    {
        "ALIGNMENT", "PAGENO", "POSITIONIDENTIFIER", "RECTANGLEOFFSET", "RECTANGLESIZE",
        "VISIBLESIGNATURE", "SHOWSIGNERINFO", "SIGNERINFOPREFIX",
        "SHOWDATETIME", "DATETIMEPREFIX", "SHOWREASON", "SIGNREASON",
        "SHOWLOCATION", "LOCATION", "TEXTCOLOR", "TEXTDIRECTION"
    };

    foreach (var key in metaKeys)
    {
        if (form.ContainsKey(key))
        {
            customMeta[key] = form[key].ToString()?.Trim() ?? "";
        }
    }

    byte[] fileBytes;
    using (var ms = new MemoryStream())
    {
        await file.CopyToAsync(ms);
        fileBytes = ms.ToArray();
    }

    var signResult = await eSignService.SignFileAsync(uid, passcode, file.FileName, fileBytes, customMeta);

    if (signResult.success && signResult.record != null)
    {
        return Results.Ok(new
        {
            success = true,
            message = signResult.message,
            document = signResult.record
        });
    }

    return Results.BadRequest(new
    {
        success = false,
        message = signResult.message,
        responseCode = signResult.response?.responseCode,
        billCode = signResult.response?.billCode,
        authorizeCredential = signResult.response?.authorizeCredential
    });
});

// =================== DOCUMENTS & HISTORY ENDPOINTS ===================
app.MapGet("/api/documents/history", (string? uid, HistoryService historyService) =>
{
    var records = historyService.GetHistoryByUid(uid ?? "");
    return Results.Ok(records);
});

app.MapGet("/api/documents/download/{id}", (string id, HistoryService historyService, IWebHostEnvironment env) =>
{
    var doc = historyService.GetDocumentById(id);
    if (doc == null)
    {
        return Results.NotFound(new { success = false, message = "Không tìm thấy tài liệu." });
    }

    string filePath = doc.SignedFilePath;
    if (!File.Exists(filePath))
    {
        string fileName = Path.GetFileName(doc.SignedFilePath);
        string safeUid = string.IsNullOrWhiteSpace(doc.AgreementUUID) ? "anonymous" : string.Join("_", doc.AgreementUUID.Trim().Split(Path.GetInvalidFileNameChars()));
        string altPath1 = Path.Combine(env.ContentRootPath, "storage", "signed", safeUid, fileName);
        string altPath2 = Path.Combine(env.ContentRootPath, "storage", "signed", fileName);
        if (File.Exists(altPath1)) filePath = altPath1;
        else if (File.Exists(altPath2)) filePath = altPath2;
    }

    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { success = false, message = "Không tìm thấy file đã ký trên máy chủ." });
    }

    var bytes = File.ReadAllBytes(filePath);
    return Results.File(bytes, doc.MimeType, doc.SignedFileName);
});

app.MapGet("/api/documents/preview/{id}", (string id, HistoryService historyService, IWebHostEnvironment env) =>
{
    var doc = historyService.GetDocumentById(id);
    if (doc == null)
    {
        return Results.NotFound(new { success = false, message = "Không tìm thấy tài liệu." });
    }

    string filePath = doc.SignedFilePath;
    if (!File.Exists(filePath))
    {
        string fileName = Path.GetFileName(doc.SignedFilePath);
        string safeUid = string.IsNullOrWhiteSpace(doc.AgreementUUID) ? "anonymous" : string.Join("_", doc.AgreementUUID.Trim().Split(Path.GetInvalidFileNameChars()));
        string altPath1 = Path.Combine(env.ContentRootPath, "storage", "signed", safeUid, fileName);
        string altPath2 = Path.Combine(env.ContentRootPath, "storage", "signed", fileName);
        if (File.Exists(altPath1)) filePath = altPath1;
        else if (File.Exists(altPath2)) filePath = altPath2;
    }

    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { success = false, message = "Không tìm thấy file để xem trước." });
    }

    var bytes = File.ReadAllBytes(filePath);
    return Results.File(bytes, doc.MimeType, enableRangeProcessing: true);
});

app.MapDelete("/api/documents/{id}", (string id, HistoryService historyService) =>
{
    bool deleted = historyService.DeleteRecord(id);
    return deleted
        ? Results.Ok(new { success = true, message = "Đã xóa bản ghi thành công." })
        : Results.NotFound(new { success = false, message = "Không tìm thấy bản ghi." });
});

// =================== CONFIGURATION ENDPOINTS ===================
app.MapGet("/api/config", (ConfigService configService) =>
{
    var config = configService.GetConfig();
    return Results.Ok(config);
});

app.MapPost("/api/config", (ESignCloudConfig newConfig, ConfigService configService) =>
{
    configService.SaveConfig(newConfig);
    return Results.Ok(new { success = true, message = "Đã lưu cấu hình thành công!", config = configService.GetConfig() });
});

app.MapPost("/api/config/reset", (ConfigService configService) =>
{
    var resetConfig = configService.ResetToDefault();
    return Results.Ok(new { success = true, message = "Đã khôi phục cấu hình mặc định!", config = resetConfig });
});

app.MapPost("/api/config/test-connection", (ESignCloudService eSignService) =>
{
    var testResult = eSignService.TestKeystoreConnection();
    return Results.Ok(new { success = testResult.isValid, message = testResult.message });
});

app.MapPost("/api/config/upload-keystore", async (HttpRequest request, ConfigService configService, IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { success = false, message = "Yêu cầu định dạng form-data." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("keystoreFile");

    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { success = false, message = "Vui lòng chọn file chứng thư (.p12 / .pfx)!" });
    }

    string fileDir = Path.Combine(env.ContentRootPath, "file");
    Directory.CreateDirectory(fileDir);
    string destPath = Path.Combine(fileDir, file.FileName);

    using (var stream = new FileStream(destPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    var config = configService.GetConfig();
    config.RelyingPartyKeyStore = Path.Combine("file", file.FileName);
    configService.SaveConfig(config);

    return Results.Ok(new { success = true, message = $"Đã tải lên và cập nhật đường dẫn keystore: file/{file.FileName}", filePath = config.RelyingPartyKeyStore });
});

app.MapGet("/api/slides", (ConfigService configService) =>
{
    var config = configService.GetConfig();
    return Results.Ok(config.Slides);
});

// =================== ADMIN UID DIRECTORY MANAGEMENT ===================
app.MapGet("/api/admin/uids", (ConfigService configService) =>
{
    var config = configService.GetConfig();
    return Results.Ok(config.Accounts ?? new List<SignerAccountRecord>());
});

app.MapPost("/api/admin/uids", (SignerAccountRecord account, ConfigService configService) =>
{
    if (string.IsNullOrWhiteSpace(account.AgreementUUID))
    {
        return Results.BadRequest(new { success = false, message = "AgreementUUID không được để trống!" });
    }

    var config = configService.GetConfig();
    config.Accounts ??= new List<SignerAccountRecord>();

    var existing = config.Accounts.FirstOrDefault(a => a.AgreementUUID.Equals(account.AgreementUUID.Trim(), StringComparison.OrdinalIgnoreCase));
    if (existing != null)
    {
        existing.SignerName = account.SignerName;
        existing.Department = account.Department;
        existing.Email = account.Email;
        existing.Phone = account.Phone;
        existing.DefaultPasscode = account.DefaultPasscode;
        existing.Status = account.Status;
    }
    else
    {
        account.AgreementUUID = account.AgreementUUID.Trim();
        config.Accounts.Add(account);
    }

    configService.SaveConfig(config);
    return Results.Ok(new { success = true, message = "Đã lưu tài khoản UID thành công!", accounts = config.Accounts });
});

app.MapDelete("/api/admin/uids/{uuid}", (string uuid, ConfigService configService) =>
{
    var config = configService.GetConfig();
    config.Accounts ??= new List<SignerAccountRecord>();

    var item = config.Accounts.FirstOrDefault(a => a.AgreementUUID.Equals(uuid.Trim(), StringComparison.OrdinalIgnoreCase));
    if (item != null)
    {
        config.Accounts.Remove(item);
        configService.SaveConfig(config);
        return Results.Ok(new { success = true, message = "Đã xóa UID khỏi danh bạ.", accounts = config.Accounts });
    }

    return Results.NotFound(new { success = false, message = "Không tìm thấy UID trong danh bạ." });
});

app.MapPost("/api/admin/uids/verify", async (LoginRequest req, ESignCloudService eSignService) =>
{
    if (string.IsNullOrWhiteSpace(req.Uid))
    {
        return Results.BadRequest(new { success = false, message = "Thiếu UID cần kiểm tra." });
    }

    var result = await eSignService.GetCertificateDetailForSignCloudAsync(req.Uid.Trim(), req.Passcode ?? "12345678");
    return Results.Ok(new
    {
        success = result.success,
        message = result.message,
        signerName = result.signerName,
        certificateDN = result.response?.certificateDN,
        serialNumber = result.response?.certificateSerialNumber,
        validFrom = result.response?.validFrom,
        validTo = result.response?.validTo
    });
});

app.MapPost("/api/admin/slides/upload-image", async (HttpRequest request, IWebHostEnvironment env) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { success = false, message = "Request phải có định dạng multipart/form-data!" });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { success = false, message = "Vui lòng chọn file ảnh hợp lệ!" });
    }

    var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".webp", ".svg", ".gif", ".jfif", ".bmp" };
    string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
    {
        return Results.BadRequest(new { success = false, message = "Định dạng file không hỗ trợ! Chỉ chấp nhận PNG, JPG, JPEG, WEBP, SVG, GIF, JFIF." });
    }

    string wwwRoot = !string.IsNullOrEmpty(env.WebRootPath) && Directory.Exists(env.WebRootPath)
        ? env.WebRootPath
        : Path.Combine(env.ContentRootPath, "wwwroot");

    string bannersDir = Path.Combine(wwwRoot, "assets", "banners");
    Directory.CreateDirectory(bannersDir);

    string fileName = $"banner_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..6]}{ext}";
    string filePath = Path.Combine(bannersDir, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    // Also mirror to base directory wwwroot if running published
    try
    {
        string altRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot", "assets", "banners");
        if (!string.Equals(Path.GetFullPath(bannersDir), Path.GetFullPath(altRoot), StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(altRoot);
            File.Copy(filePath, Path.Combine(altRoot, fileName), true);
        }
    }
    catch { }

    string relativeUrl = $"/assets/banners/{fileName}";
    return Results.Ok(new
    {
        success = true,
        message = "Tải ảnh banner thành công!",
        imageUrl = relativeUrl
    });
}).DisableAntiforgery();

app.MapGet("/api/debug/inspect-sample", (string? file) =>
{
    string samplePath = string.IsNullOrWhiteSpace(file)
        ? @"e:\project\eSignCloudRestAPI\eSignCloudRestAPI\bin\Debug\file\sample.signed.pdf"
        : file;

    if (!File.Exists(samplePath))
    {
        samplePath = Path.Combine(Directory.GetCurrentDirectory(), samplePath);
    }

    if (!File.Exists(samplePath))
    {
        return Results.NotFound(new { error = $"File {samplePath} not found" });
    }

    using var reader = new iText.Kernel.Pdf.PdfReader(samplePath);
    using var pdfDoc = new iText.Kernel.Pdf.PdfDocument(reader);
    var signUtil = new iText.Signatures.SignatureUtil(pdfDoc);
    var sigNames = signUtil.GetSignatureNames();

    var sigs = new List<object>();
    foreach (var name in sigNames)
    {
        var pkcs7 = signUtil.ReadSignatureData(name);
        var cert = pkcs7.GetSigningCertificate();
        sigs.Add(new
        {
            fieldName = name,
            signerName = pkcs7.GetSignName(),
            signDate = pkcs7.GetSignDate(),
            reason = pkcs7.GetReason(),
            location = pkcs7.GetLocation(),
            certSubject = cert?.GetSubjectDN()?.ToString(),
            certIssuer = cert?.GetIssuerDN()?.ToString()
        });
    }

    var pages = new List<object>();
    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
    {
        var page = pdfDoc.GetPage(i);
        var annots = page.GetAnnotations();
        var annotList = new List<object>();
        foreach (var ann in annots)
        {
            var rect = ann.GetRectangle().ToRectangle();
            annotList.Add(new
            {
                subtype = ann.GetSubtype()?.ToString(),
                rect = $"{rect.GetX()},{rect.GetY()},{rect.GetWidth()},{rect.GetHeight()}"
            });
        }
        pages.Add(new { page = i, pageSize = $"{page.GetPageSize().GetWidth()}x{page.GetPageSize().GetHeight()}", annotations = annotList });
    }

    return Results.Ok(new { sigs, pages });
});

Console.WriteLine("==================================================");
Console.WriteLine("  eSignCloud Web Portal Server is running!       ");
Console.WriteLine("==================================================");

app.Run();
