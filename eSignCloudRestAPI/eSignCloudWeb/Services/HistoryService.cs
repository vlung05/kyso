using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using eSignCloudWeb.Models;
using Newtonsoft.Json;

namespace eSignCloudWeb.Services
{
    public class HistoryService
    {
        private readonly string _historyFilePath;
        private readonly object _lock = new object();
        private List<SignedDocumentRecord> _records;

        public HistoryService(IWebHostEnvironment env)
        {
            string storageDir = Path.Combine(env.ContentRootPath, "storage");
            if (!Directory.Exists(storageDir))
            {
                Directory.CreateDirectory(storageDir);
            }
            _historyFilePath = Path.Combine(storageDir, "history.json");
            _records = LoadHistory();
        }

        public List<SignedDocumentRecord> GetHistoryByUid(string agreementUUID)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(agreementUUID))
                {
                    return _records.OrderByDescending(r => r.SignDate).ToList();
                }

                return _records
                    .Where(r => string.Equals(r.AgreementUUID, agreementUUID, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(r => r.SignDate)
                    .ToList();
            }
        }

        public int GetSignedCountByUid(string agreementUUID)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(agreementUUID)) return 0;
                return _records.Count(r => string.Equals(r.AgreementUUID, agreementUUID, StringComparison.OrdinalIgnoreCase));
            }
        }

        public Dictionary<string, int> GetAllSignedCounts()
        {
            lock (_lock)
            {
                return _records
                    .Where(r => !string.IsNullOrWhiteSpace(r.AgreementUUID))
                    .GroupBy(r => r.AgreementUUID.Trim(), StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
            }
        }

        public SignedDocumentRecord? GetDocumentById(string id)
        {
            lock (_lock)
            {
                return _records.FirstOrDefault(r => r.Id == id);
            }
        }

        public void AddRecord(SignedDocumentRecord record)
        {
            lock (_lock)
            {
                _records.Add(record);
                SaveHistory();
            }
        }

        public bool DeleteRecord(string id)
        {
            lock (_lock)
            {
                var item = _records.FirstOrDefault(r => r.Id == id);
                if (item != null)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(item.OriginalFilePath) && File.Exists(item.OriginalFilePath))
                        {
                            File.Delete(item.OriginalFilePath);
                        }
                        if (!string.IsNullOrEmpty(item.SignedFilePath) && File.Exists(item.SignedFilePath))
                        {
                            File.Delete(item.SignedFilePath);
                        }
                    }
                    catch { }

                    _records.Remove(item);
                    SaveHistory();
                    return true;
                }
                return false;
            }
        }

        private List<SignedDocumentRecord> LoadHistory()
        {
            if (File.Exists(_historyFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_historyFilePath);
                    var records = JsonConvert.DeserializeObject<List<SignedDocumentRecord>>(json);
                    if (records != null) return records;
                }
                catch { }
            }
            return new List<SignedDocumentRecord>();
        }

        private void SaveHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_records, Formatting.Indented);
                File.WriteAllText(_historyFilePath, json);
            }
            catch { }
        }
    }
}
