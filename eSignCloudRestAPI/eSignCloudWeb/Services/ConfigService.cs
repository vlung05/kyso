using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using eSignCloudWeb.Models;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;

namespace eSignCloudWeb.Services
{
    public class ConfigService
    {
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = Formatting.Indented
        };

        private readonly string _configFilePath;
        private readonly string _accountsFilePath;
        private readonly object _lock = new object();
        private ESignCloudConfig _currentConfig;

        public ConfigService(IWebHostEnvironment env)
        {
            _configFilePath = Path.Combine(env.ContentRootPath, "config.json");
            _accountsFilePath = Path.Combine(env.ContentRootPath, "storage", "accounts.json");
            _currentConfig = LoadConfig();
        }

        public ESignCloudConfig GetConfig()
        {
            lock (_lock)
            {
                return _currentConfig;
            }
        }

        public void SaveConfig(ESignCloudConfig newConfig)
        {
            lock (_lock)
            {
                if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));

                newConfig.Accounts ??= new List<SignerAccountRecord>();
                newConfig.AdminUids ??= new List<string>();

                // Deduplicate accounts and adminUids
                newConfig.Accounts = newConfig.Accounts
                    .Where(a => !string.IsNullOrWhiteSpace(a.AgreementUUID))
                    .GroupBy(a => a.AgreementUUID.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                newConfig.AdminUids = newConfig.AdminUids
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (newConfig.Slides != null)
                {
                    for (int i = 0; i < newConfig.Slides.Count; i++)
                    {
                        newConfig.Slides[i].Id = i + 1;
                    }
                }
                else if (_currentConfig?.Slides != null)
                {
                    newConfig.Slides = _currentConfig.Slides;
                }

                // Protect Admin Username & Password
                if (string.IsNullOrWhiteSpace(newConfig.AdminUsername) && !string.IsNullOrWhiteSpace(_currentConfig?.AdminUsername))
                {
                    newConfig.AdminUsername = _currentConfig.AdminUsername;
                }
                if (string.IsNullOrWhiteSpace(newConfig.AdminPassword) && !string.IsNullOrWhiteSpace(_currentConfig?.AdminPassword))
                {
                    newConfig.AdminPassword = _currentConfig.AdminPassword;
                }

                _currentConfig = newConfig;
                string json = JsonConvert.SerializeObject(_currentConfig, JsonSettings);
                File.WriteAllText(_configFilePath, json);

                // Đồng bộ danh bạ sang storage/accounts.json
                SaveAccountsBackup(_currentConfig.Accounts);
            }
        }

        public ESignCloudConfig ResetToDefault()
        {
            lock (_lock)
            {
                var existingAccounts = _currentConfig?.Accounts ?? new List<SignerAccountRecord>();
                var existingAdminUids = _currentConfig?.AdminUids ?? new List<string>();

                _currentConfig = new ESignCloudConfig();
                _currentConfig.Accounts = existingAccounts;
                _currentConfig.AdminUids = existingAdminUids;
                _currentConfig.Slides = GetDefaultSlides();

                string json = JsonConvert.SerializeObject(_currentConfig, JsonSettings);
                File.WriteAllText(_configFilePath, json);
                SaveAccountsBackup(_currentConfig.Accounts);

                return _currentConfig;
            }
        }

        public static List<SlideItem> GetDefaultSlides()
        {
            return new List<SlideItem>
            {
                new SlideItem
                {
                    Id = 1,
                    Tag = "DỊCH VỤ CHỮ KÝ SỐ CLOUD",
                    Title = "Ký Số Từ Xa Mobile-ID RSSP",
                    Description = "Ký số mọi lúc, mọi nơi trên đa thiết bị không cần USB Token. Đạt chuẩn bảo mật eIDAS & TT 16/2019/BTTTT.",
                    Gradient = "linear-gradient(135deg, #1e3c72 0%, #2a5298 100%)",
                    Icon = "shield-check",
                    ButtonText = "Khám phá ngay",
                    ButtonLink = "#"
                },
                new SlideItem
                {
                    Id = 2,
                    Tag = "TÍNH NĂNG NỔI BẬT",
                    Title = "Tương Thích PDF & Microsoft Word",
                    Description = "Hỗ trợ định dạng tài liệu PDF, DOC, DOCX. Tùy biến vị trí ký linh hoạt, chữ ký đồ họa chuẩn pháp lý.",
                    Gradient = "linear-gradient(135deg, #0ba360 0%, #3cba92 100%)",
                    Icon = "file-signature",
                    ButtonText = "Ký thử tài liệu",
                    ButtonLink = "#"
                },
                new SlideItem
                {
                    Id = 3,
                    Tag = "BẢO MẬT TUYỆT ĐỐI",
                    Title = "Xác Thực Passcode & OTP 2 Lớp",
                    Description = "Bảo vệ giao dịch ký an toàn với xác thực PassCode cá nhân hóa và mã OTP tức thời từ Mobile-ID.",
                    Gradient = "linear-gradient(135deg, #7928ca 0%, #ff0080 100%)",
                    Icon = "lock",
                    ButtonText = "Xem tài liệu API",
                    ButtonLink = "#"
                }
            };
        }

        public string ResolveKeyStorePath(string contentRootPath)
        {
            var config = GetConfig();
            string keyStorePath = config.RelyingPartyKeyStore;

            if (Path.IsPathRooted(keyStorePath) && File.Exists(keyStorePath))
            {
                return keyStorePath;
            }

            string fullPath = Path.Combine(contentRootPath, keyStorePath);
            if (File.Exists(fullPath)) return fullPath;

            string webRootPath = Path.Combine(contentRootPath, "wwwroot", keyStorePath);
            if (File.Exists(webRootPath)) return webRootPath;

            string baseDirPath = Path.Combine(AppContext.BaseDirectory, keyStorePath);
            if (File.Exists(baseDirPath)) return baseDirPath;

            // Check standard fallback locations
            string[] fallbacks = new[]
            {
                Path.Combine(contentRootPath, "filehsm", "BK.p12"),
                Path.Combine(contentRootPath, "wwwroot", "filehsm", "BK.p12"),
                Path.Combine(contentRootPath, "file", "BK.p12"),
                Path.Combine(contentRootPath, "file", "rssp.p12"),
                Path.Combine(AppContext.BaseDirectory, "filehsm", "BK.p12"),
                Path.Combine(AppContext.BaseDirectory, "file", "rssp.p12"),
                Path.Combine(contentRootPath, "..", "eSignCloudRestAPI", "bin", "Debug", "file", "rssp.p12")
            };

            foreach (var fb in fallbacks)
            {
                if (File.Exists(fb)) return Path.GetFullPath(fb);
            }

            return fullPath;
        }

        private ESignCloudConfig LoadConfig()
        {
            ESignCloudConfig? config = null;

            if (File.Exists(_configFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_configFilePath);
                    config = JsonConvert.DeserializeObject<ESignCloudConfig>(json, JsonSettings);
                }
                catch (Exception)
                {
                    // Fall back to default
                }
            }

            config ??= new ESignCloudConfig();
            config.Accounts ??= new List<SignerAccountRecord>();
            config.AdminUids ??= new List<string>();
            config.Slides ??= new List<SlideItem>();

            // Deduplicate
            config.Accounts = config.Accounts
                .Where(a => !string.IsNullOrWhiteSpace(a.AgreementUUID))
                .GroupBy(a => a.AgreementUUID.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            config.AdminUids = config.AdminUids
                .Where(u => !string.IsNullOrWhiteSpace(u))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (config.Slides.Count > 0)
            {
                for (int i = 0; i < config.Slides.Count; i++)
                {
                    config.Slides[i].Id = i + 1;
                }
            }

            // Sync with backup file if accounts file exists
            if (File.Exists(_accountsFilePath))
            {
                var backup = LoadAccountsBackup();
                if (backup != null)
                {
                    var cleanBackup = backup
                        .Where(a => !string.IsNullOrWhiteSpace(a.AgreementUUID))
                        .GroupBy(a => a.AgreementUUID.Trim(), StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToList();

                    if (config.Accounts.Count == 0 && cleanBackup.Count > 0)
                    {
                        config.Accounts = cleanBackup;
                    }
                }
            }

            // Auto-extract TaxId and Address from CertificateDN if empty
            foreach (var acc in config.Accounts)
            {
                if (string.IsNullOrWhiteSpace(acc.TaxId) && !string.IsNullOrWhiteSpace(acc.CertificateDN))
                {
                    acc.TaxId = ESignCloudService.ExtractTaxIdFromDN(acc.CertificateDN);
                }
                if (string.IsNullOrWhiteSpace(acc.Address) && !string.IsNullOrWhiteSpace(acc.CertificateDN))
                {
                    acc.Address = ESignCloudService.ExtractAddressFromDN(acc.CertificateDN);
                }
            }

            SaveAccountsBackup(config.Accounts);

            try
            {
                string json = JsonConvert.SerializeObject(config, JsonSettings);
                File.WriteAllText(_configFilePath, json);
            }
            catch { }

            return config;
        }

        private List<SignerAccountRecord>? LoadAccountsBackup()
        {
            try
            {
                if (File.Exists(_accountsFilePath))
                {
                    string json = File.ReadAllText(_accountsFilePath);
                    return JsonConvert.DeserializeObject<List<SignerAccountRecord>>(json, JsonSettings);
                }
            }
            catch { }
            return null;
        }

        private void SaveAccountsBackup(List<SignerAccountRecord>? accounts)
        {
            try
            {
                string dir = Path.GetDirectoryName(_accountsFilePath) ?? "";
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                var list = (accounts ?? new List<SignerAccountRecord>())
                    .Where(a => !string.IsNullOrWhiteSpace(a.AgreementUUID))
                    .GroupBy(a => a.AgreementUUID.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();

                string json = JsonConvert.SerializeObject(list, JsonSettings);
                File.WriteAllText(_accountsFilePath, json);
            }
            catch { }
        }
    }
}
