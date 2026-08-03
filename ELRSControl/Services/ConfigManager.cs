using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.Json;

namespace ELRSControl.Services
{
    public class ConfigManager
    {
        private const string ConfigFileName = "crsf_config.json";
        private readonly string _configPath;

        public ConfigManager()
        {
            _configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ELRSControl",
                ConfigFileName
            );
        }

        /// <summary>
        /// Загружает пользовательские адреса из конфигурации
        /// </summary>
        public ObservableCollection<string> LoadCustomAddresses()
        {
            var addresses = new ObservableCollection<string>();

            try
            {
                if (!File.Exists(_configPath))
                    return addresses;

                var json = File.ReadAllText(_configPath);
                var config = JsonSerializer.Deserialize<ConfigData>(json);

                if (config?.CustomAddresses != null)
                {
                    foreach (var address in config.CustomAddresses)
                    {
                        addresses.Add(address);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке конфигурации: {ex.Message}");
            }

            return addresses;
        }

        /// <summary>
        /// Сохраняет пользовательские адреса в конфигурацию
        /// </summary>
        public void SaveCustomAddresses(ObservableCollection<string> addresses)
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var config = new ConfigData
                {
                    CustomAddresses = new List<string>(addresses)
                };

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Добавляет новый адрес и сохраняет конфигурацию
        /// </summary>
        public void AddAddress(ObservableCollection<string> addresses, string address)
        {
            if (!string.IsNullOrWhiteSpace(address) && !addresses.Contains(address))
            {
                addresses.Add(address);
                SaveCustomAddresses(addresses);
            }
        }

        /// <summary>
        /// Удаляет адрес и сохраняет конфигурацию
        /// </summary>
        public void RemoveAddress(ObservableCollection<string> addresses, string address)
        {
            if (addresses.Contains(address))
            {
                addresses.Remove(address);
                SaveCustomAddresses(addresses);
            }
        }

        private class ConfigData
        {
            public List<string> CustomAddresses { get; set; } = new List<string>();
        }
    }
}
