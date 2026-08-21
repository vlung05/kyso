using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using eSignCloudWeb.Models;
using Newtonsoft.Json;

namespace eSignCloudWeb.Services
{
    public class ConfigService
    {
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

                // 1. Bảo vệ danh bạ Accounts: Không bao giờ để mất khi lưu cấu hình từ tab khác
                if (newConfig.Accounts == null || newConfig.Accounts.Count == 0)
                {
                    if (_currentConfig?.Accounts != null && _currentConfig.Accounts.Count > 0)
                    {
                        newConfig.Accounts = _currentConfig.Accounts;
                    }
                    else
                    {
                        var backupAccs = LoadAccountsBackup();
                        if (backupAccs != null && backupAccs.Count > 0)
                        {
                            newConfig.Accounts = backupAccs;
                        }
                    }
                }

                // 2. Bảo vệ AdminUids
                if (newConfig.AdminUids == null || newConfig.AdminUids.Count == 0)
                {
                    if (_currentConfig?.AdminUids != null && _currentConfig.AdminUids.Count > 0)
                    {
                        newConfig.AdminUids = _currentConfig.AdminUids;
                    }
                }

                // 3. Bảo vệ Admin Username & Password
                if (string.IsNullOrWhiteSpace(newConfig.AdminUsername) && !string.IsNullOrWhiteSpace(_currentConfig?.AdminUsername))
                {
                    newConfig.AdminUsername = _currentConfig.AdminUsername;
                }
                if (string.IsNullOrWhiteSpace(newConfig.AdminPassword) && !string.IsNullOrWhiteSpace(_currentConfig?.AdminPassword))
                {
                    newConfig.AdminPassword = _currentConfig.AdminPassword;
                }

                // 4. Bảo vệ Slides
                if ((newConfig.Slides == null || newConfig.Slides.Count == 0) && _currentConfig?.Slides != null && _currentConfig.Slides.Count > 0)
                {
                    newConfig.Slides = _currentConfig.Slides;
                }

                _currentConfig = newConfig;
                string json = JsonConvert.SerializeObject(_currentConfig, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);

                // Tự động sao lưu Accounts sang storage/accounts.json
                SaveAccountsBackup(_currentConfig.Accounts);
            }
        }

        public ESignCloudConfig ResetToDefault()
        {
            lock (_lock)
            {
                var existingAccounts = _currentConfig?.Accounts ?? LoadAccountsBackup();
                var existingAdminUids = _currentConfig?.AdminUids;

                _currentConfig = new ESignCloudConfig();

                // Giữ lại danh bạ người dùng & quyền admin khi reset cấu hình kết nối
                if (existingAccounts != null && existingAccounts.Count > 0)
                {
                    _currentConfig.Accounts = existingAccounts;
                }
                if (existingAdminUids != null && existingAdminUids.Count > 0)
                {
                    _currentConfig.AdminUids = existingAdminUids;
                }

                string json = JsonConvert.SerializeObject(_currentConfig, Formatting.Indented);
                File.WriteAllText(_configFilePath, json);
                SaveAccountsBackup(_currentConfig.Accounts);

                return _currentConfig;
            }
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
                    config = JsonConvert.DeserializeObject<ESignCloudConfig>(json);
                }
                catch (Exception)
                {
                    // Fall back to default
                }
            }

            config ??= new ESignCloudConfig();

            // Nếu trong config.json chưa có Accounts hoặc bị trống, nạp từ file backup storage/accounts.json
            if (config.Accounts == null || config.Accounts.Count == 0)
            {
                var backup = LoadAccountsBackup();
                if (backup != null && backup.Count > 0)
                {
                    config.Accounts = backup;
                }
            }
            else
            {
                SaveAccountsBackup(config.Accounts);
            }

            try
            {
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
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
                    return JsonConvert.DeserializeObject<List<SignerAccountRecord>>(json);
                }
            }
            catch { }
            return null;
        }

        private void SaveAccountsBackup(List<SignerAccountRecord>? accounts)
        {
            if (accounts == null || accounts.Count == 0) return;
            try
            {
                string dir = Path.GetDirectoryName(_accountsFilePath) ?? "";
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
                File.WriteAllText(_accountsFilePath, json);
            }
            catch { }
        }
    }
}
