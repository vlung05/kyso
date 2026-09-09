using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using eSignCloudWeb.Models;
using Newtonsoft.Json;

namespace eSignCloudWeb.Services
{
    public class GoogleSheetSyncRequest
    {
        public string SheetUrl { get; set; } = "https://docs.google.com/spreadsheets/d/1HOlhpTboQragrjYgk7Iw3WqEsFXDDpx6/edit?gid=833688207#gid=833688207";
        public string? WebhookUrl { get; set; }
        public bool AutoSaveToAccounts { get; set; } = true;
    }

    public class PushWebhookRequest
    {
        public string WebhookUrl { get; set; } = "";
        public string Gid { get; set; } = "0";
        public List<object>? Updates { get; set; }
    }

    public class GoogleSheetRowResult
    {
        public int RowIndex { get; set; } // 1-based index (e.g. 2, 3...)
        public string Stt { get; set; } = "";
        public string Date { get; set; } = "";        // Ngày từ sheet (cột B cũ)
        public string NewDate { get; set; } = "";     // Ngày bắt đầu sử dụng từ RSSP (ValidFrom, dd/MM/yyyy)
        public string NewEndDate { get; set; } = "";  // Ngày kết thúc (ValidTo, dd/MM/yyyy)
        public long ValidFromMs { get; set; }          // Unix ms (để frontend format lại nếu cần)
        public long ValidToMs { get; set; }
        public string OldName { get; set; } = "";
        public string NewName { get; set; } = "";
        public string OldTaxId { get; set; } = "";
        public string NewTaxId { get; set; } = "";
        public string OldAddress { get; set; } = "";
        public string NewAddress { get; set; } = "";
        public string AgreementUUID { get; set; } = "";
        public string Passcode { get; set; } = "";
        public string Sale { get; set; } = "";
        public string Status { get; set; } = "";
        public string Amount { get; set; } = "";
        public string Note { get; set; } = "";
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = "";
        public bool HasChanges { get; set; }
    }

    public class GoogleSheetSyncResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string SpreadsheetId { get; set; } = "";
        public string Gid { get; set; } = "";
        public int TotalRows { get; set; }
        public int ProcessedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int ErrorCount { get; set; }
        public List<GoogleSheetRowResult> Rows { get; set; } = new List<GoogleSheetRowResult>();
        public bool WebhookSent { get; set; }
        public string? WebhookMessage { get; set; }
    }

    public class GoogleSheetSyncService
    {
        private readonly HttpClient _httpClient;
        private readonly ESignCloudService _eSignService;
        private readonly ConfigService _configService;

        public GoogleSheetSyncService(HttpClient httpClient, ESignCloudService eSignService, ConfigService configService)
        {
            _httpClient = httpClient;
            _eSignService = eSignService;
            _configService = configService;
        }

        public (string spreadsheetId, string gid) ParseSheetUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return ("", "");

            string spreadsheetId = "";
            string gid = "0";

            var matchId = Regex.Match(url, @"/d/([a-zA-Z0-9-_]+)", RegexOptions.IgnoreCase);
            if (matchId.Success)
            {
                spreadsheetId = matchId.Groups[1].Value;
            }

            var matchGid = Regex.Match(url, @"[#&?]gid=([0-9]+)", RegexOptions.IgnoreCase);
            if (matchGid.Success)
            {
                gid = matchGid.Groups[1].Value;
            }

            return (spreadsheetId, gid);
        }

        public async Task<string> FetchCsvFromGoogleSheetAsync(string sheetUrl)
        {
            var (spreadsheetId, gid) = ParseSheetUrl(sheetUrl);
            if (string.IsNullOrEmpty(spreadsheetId))
            {
                throw new ArgumentException("Không thể nhận diện Spreadsheet ID từ đường link Google Sheet đã cung cấp.");
            }

            string exportUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/gviz/tq?tqx=out:csv&gid={gid}";

            var response = await _httpClient.GetAsync(exportUrl);
            if (!response.IsSuccessStatusCode)
            {
                // Fallback URL
                string fallbackUrl = $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={gid}";
                response = await _httpClient.GetAsync(fallbackUrl);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Lỗi kết nối tải dữ liệu từ Google Sheet (HTTP Status: {response.StatusCode}). Hãy chắc chắn rằng Google Sheet đã được mở quyền xem (Anyone with the link can view).");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<GoogleSheetSyncResponse> SyncFromGoogleSheetAsync(GoogleSheetSyncRequest request)
        {
            var response = new GoogleSheetSyncResponse();
            var (spreadsheetId, gid) = ParseSheetUrl(request.SheetUrl);
            response.SpreadsheetId = spreadsheetId;
            response.Gid = gid;

            string csvContent;
            try
            {
                csvContent = await FetchCsvFromGoogleSheetAsync(request.SheetUrl);
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
                return response;
            }

            var rows = ParseCsv(csvContent);
            if (rows.Count < 2)
            {
                response.Success = false;
                response.Message = "Bảng tính Google Sheet không có đủ dữ liệu hàng để xử lý.";
                return response;
            }

            var config = _configService.GetConfig();
            config.Accounts ??= new List<SignerAccountRecord>();
            bool accountsModified = false;

            var results = new List<GoogleSheetRowResult>();
            var updatesForWebhook = new List<object>();

            // Header row is rows[0]
            for (int i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
                    continue;

                int sheetRowNumber = i + 1; // 1-based index in Google Sheets
                string stt = row.Count > 0 ? row[0].Trim() : "";
                string date = row.Count > 1 ? row[1].Trim() : "";
                string currentName = row.Count > 2 ? row[2].Trim() : "";
                string currentTaxId = row.Count > 3 ? row[3].Trim() : "";
                string currentAddress = row.Count > 4 ? row[4].Trim() : "";
                string rawUuidCol = row.Count > 5 ? row[5].Trim() : "";
                string rawPassCol = row.Count > 6 ? row[6].Trim() : "";
                string sale = row.Count > 7 ? row[7].Trim() : "";
                string status = row.Count > 8 ? row[8].Trim() : "";
                string amount = row.Count > 9 ? row[9].Trim() : "";
                string note = row.Count > 10 ? row[10].Trim() : "";

                // Look for agreementUUID
                string uuid = "";
                var matchUuid = Regex.Match(rawUuidCol, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                if (matchUuid.Success)
                {
                    uuid = matchUuid.Value;
                }
                else
                {
                    // Check note or other columns
                    var m2 = Regex.Match(note, @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}");
                    if (m2.Success) uuid = m2.Value;
                }

                if (string.IsNullOrEmpty(uuid))
                {
                    // Row without UUID
                    continue;
                }

                // Look for passcode
                string passcode = rawPassCol.Trim();
                if (string.IsNullOrEmpty(passcode))
                {
                    var matchPass = Regex.Match(rawUuidCol, @"PassCode[:\s]+([0-9a-zA-Z]+)", RegexOptions.IgnoreCase);
                    if (matchPass.Success)
                    {
                        passcode = matchPass.Groups[1].Value.Trim();
                    }
                }
                if (string.IsNullOrEmpty(passcode))
                {
                    passcode = "12345678";
                }

                var rowRes = new GoogleSheetRowResult
                {
                    RowIndex = sheetRowNumber,
                    Stt = stt,
                    Date = date,
                    OldName = currentName,
                    OldTaxId = currentTaxId,
                    OldAddress = currentAddress,
                    AgreementUUID = uuid,
                    Passcode = passcode,
                    Sale = sale,
                    Status = status,
                    Amount = amount,
                    Note = note
                };

                // Query RSSP
                try
                {
                    var verifyResult = await _eSignService.GetCertificateDetailForSignCloudAsync(uuid, passcode);
                    if (verifyResult.success)
                    {
                        string extractedName = !string.IsNullOrWhiteSpace(verifyResult.signerName)
                            ? verifyResult.signerName
                            : currentName;

                        string extractedTaxId = ESignCloudService.ExtractTaxId(verifyResult.response, verifyResult.response?.certificateDN);
                        if (string.IsNullOrEmpty(extractedTaxId)) extractedTaxId = currentTaxId;

                        string extractedAddress = ESignCloudService.ExtractAddress(verifyResult.response, verifyResult.response?.certificateDN);
                        if (string.IsNullOrEmpty(extractedAddress)) extractedAddress = currentAddress;

                        // Compute ValidFrom date string (dd/MM/yyyy) from Unix ms
                        long validFromMs = verifyResult.response?.validFrom ?? 0;
                        string extractedDate = "";
                        if (validFromMs > 0)
                        {
                            var dtUtc = DateTimeOffset.FromUnixTimeMilliseconds(validFromMs).UtcDateTime;
                            // Convert to Vietnam time (UTC+7)
                            var dtVn = dtUtc.AddHours(7);
                            extractedDate = dtVn.ToString("dd/MM/yyyy");
                        }

                        long validToMs = verifyResult.response?.validTo ?? 0;
                        string extractedEndDate = "";
                        if (validToMs > 0)
                        {
                            var dtUtc = DateTimeOffset.FromUnixTimeMilliseconds(validToMs).UtcDateTime;
                            var dtVn = dtUtc.AddHours(7);
                            extractedEndDate = dtVn.ToString("dd/MM/yyyy");
                        }

                        rowRes.NewName = extractedName;
                        rowRes.NewTaxId = extractedTaxId;
                        rowRes.NewAddress = extractedAddress;
                        rowRes.NewDate = extractedDate;
                        rowRes.NewEndDate = extractedEndDate;
                        rowRes.ValidFromMs = validFromMs;
                        rowRes.ValidToMs = validToMs;
                        rowRes.IsSuccess = true;
                        rowRes.Message = "Tra cứu RSSP thành công";

                        bool hasChanges = (currentName != extractedName && !string.IsNullOrEmpty(extractedName)) ||
                                          (currentTaxId != extractedTaxId && !string.IsNullOrEmpty(extractedTaxId)) ||
                                          (currentAddress != extractedAddress && !string.IsNullOrEmpty(extractedAddress)) ||
                                          !string.IsNullOrEmpty(extractedDate);

                        rowRes.HasChanges = hasChanges;

                        // Save to accounts
                        if (request.AutoSaveToAccounts)
                        {
                            var existingAcc = config.Accounts.FirstOrDefault(a => a.AgreementUUID.Equals(uuid, StringComparison.OrdinalIgnoreCase));
                            if (existingAcc != null)
                            {
                                if (!string.IsNullOrWhiteSpace(extractedName)) existingAcc.SignerName = extractedName;
                                if (!string.IsNullOrWhiteSpace(extractedTaxId)) existingAcc.TaxId = extractedTaxId;
                                if (!string.IsNullOrWhiteSpace(extractedAddress)) existingAcc.Address = extractedAddress;
                                if (!string.IsNullOrWhiteSpace(passcode)) existingAcc.DefaultPasscode = passcode;
                                if (!string.IsNullOrWhiteSpace(verifyResult.response?.certificateDN)) existingAcc.CertificateDN = verifyResult.response.certificateDN;
                                if (!string.IsNullOrWhiteSpace(verifyResult.response?.certificateSerialNumber)) existingAcc.CertificateSerialNumber = verifyResult.response.certificateSerialNumber;
                                if (verifyResult.response?.validFrom > 0) existingAcc.ValidFrom = verifyResult.response.validFrom;
                                if (verifyResult.response?.validTo > 0) existingAcc.ValidTo = verifyResult.response.validTo;
                            }
                            else
                            {
                                config.Accounts.Add(new SignerAccountRecord
                                {
                                    AgreementUUID = uuid,
                                    SignerName = extractedName,
                                    TaxId = extractedTaxId,
                                    Address = extractedAddress,
                                    DefaultPasscode = passcode,
                                    Status = "Hoạt động",
                                    CertificateDN = verifyResult.response?.certificateDN,
                                    CertificateSerialNumber = verifyResult.response?.certificateSerialNumber,
                                    ValidFrom = verifyResult.response?.validFrom ?? 0,
                                    ValidTo = verifyResult.response?.validTo ?? 0
                                });
                            }
                            accountsModified = true;
                        }

                        updatesForWebhook.Add(new
                        {
                            row = sheetRowNumber,
                            date = extractedDate,      // Cột B – Ngày bắt đầu sử dụng
                            name = extractedName,      // Cột C – HKD
                            taxId = extractedTaxId,    // Cột D – MST
                            address = extractedAddress, // Cột E – Địa chỉ
                            uuid = uuid,
                            passcode = passcode
                        });
                    }
                    else
                    {
                        rowRes.IsSuccess = false;
                        rowRes.Message = verifyResult.message ?? "Lỗi tra cứu chứng thư";
                        rowRes.NewName = currentName;
                        rowRes.NewTaxId = currentTaxId;
                        rowRes.NewAddress = currentAddress;
                    }
                }
                catch (Exception ex)
                {
                    rowRes.IsSuccess = false;
                    rowRes.Message = "Exception: " + ex.Message;
                    rowRes.NewName = currentName;
                    rowRes.NewTaxId = currentTaxId;
                    rowRes.NewAddress = currentAddress;
                }

                results.Add(rowRes);
            }

            if (accountsModified)
            {
                _configService.SaveConfig(config);
            }

            response.Success = true;
            response.TotalRows = rows.Count - 1;
            response.ProcessedCount = results.Count;
            response.UpdatedCount = results.Count(r => r.IsSuccess);
            response.ErrorCount = results.Count(r => !r.IsSuccess);
            response.Rows = results;
            response.Message = $"Đã xử lý {results.Count} tài khoản UID từ Google Sheet ({response.UpdatedCount} thành công, {response.ErrorCount} lỗi).";

            // Push to Webhook if provided
            if (!string.IsNullOrWhiteSpace(request.WebhookUrl) && updatesForWebhook.Count > 0)
            {
                try
                {
                    var pushResult = await PushUpdatesToWebhookAsync(request.WebhookUrl, gid, updatesForWebhook);
                    response.WebhookSent = pushResult.success;
                    response.WebhookMessage = pushResult.message;
                }
                catch (Exception ex)
                {
                    response.WebhookSent = false;
                    response.WebhookMessage = "Lỗi gửi Webhook: " + ex.Message;
                }
            }

            return response;
        }

        public async Task<(bool success, string message)> PushUpdatesToWebhookAsync(string webhookUrl, string gid, List<object> updates)
        {
            if (string.IsNullOrWhiteSpace(webhookUrl))
                return (false, "Webhook URL không được để trống.");

            var payload = new
            {
                action = "updateSheet",
                gid = gid,
                updates = updates
            };

            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync(webhookUrl, content);
            string respContent = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
            {
                return (true, $"Đã gửi cập nhật {updates.Count} dòng lên Google Sheet Webhook thành công!");
            }

            return (false, $"Máy chủ Webhook phản hồi lỗi: {resp.StatusCode} - {respContent}");
        }

        public string GenerateUpdatedCsv(List<GoogleSheetRowResult> results)
        {
            var rows = new List<List<string>>();

            // Header matching user's Google Sheet
            rows.Add(new List<string>
            {
                "STT", "Ngày", "HKD", "MST", "Địa chỉ", "agreementUUID", "Passcode ", "Sale", "Tình trạng", "Số tiền", "Ghi chú"
            });

            foreach (var r in results)
            {
                rows.Add(new List<string>
                {
                    r.Stt,
                    r.Date,
                    !string.IsNullOrEmpty(r.NewName) ? r.NewName : r.OldName,
                    !string.IsNullOrEmpty(r.NewTaxId) ? r.NewTaxId : r.OldTaxId,
                    !string.IsNullOrEmpty(r.NewAddress) ? r.NewAddress : r.OldAddress,
                    r.AgreementUUID,
                    r.Passcode,
                    r.Sale,
                    r.Status,
                    r.Amount,
                    r.Note
                });
            }

            return BuildCsv(rows);
        }

        public static List<List<string>> ParseCsv(string csvText)
        {
            var rows = new List<List<string>>();
            if (string.IsNullOrEmpty(csvText)) return rows;

            var currentRow = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csvText.Length; i++)
            {
                char c = csvText[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < csvText.Length && csvText[i + 1] == '"')
                        {
                            currentField.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                    }
                    else if (c == '\r')
                    {
                        if (i + 1 < csvText.Length && csvText[i + 1] == '\n')
                        {
                            i++;
                        }
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                    }
                    else if (c == '\n')
                    {
                        currentRow.Add(currentField.ToString());
                        currentField.Clear();
                        rows.Add(currentRow);
                        currentRow = new List<string>();
                    }
                    else
                    {
                        currentField.Append(c);
                    }
                }
            }

            if (currentField.Length > 0 || currentRow.Count > 0)
            {
                currentRow.Add(currentField.ToString());
                rows.Add(currentRow);
            }

            return rows;
        }

        public static string BuildCsv(List<List<string>> rows)
        {
            var sb = new StringBuilder();
            foreach (var row in rows)
            {
                var formattedFields = row.Select(field =>
                {
                    if (string.IsNullOrEmpty(field)) return "\"\"";
                    string escaped = field.Replace("\"", "\"\"");
                    return $"\"{escaped}\"";
                });
                sb.AppendLine(string.Join(",", formattedFields));
            }
            return sb.ToString();
        }
    }
}
