using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        /// Загружает полную конфигурацию из файла crsf_config.json
        /// </summary>
        public ConfigData LoadConfig()
        {
            try
            {
                if (!File.Exists(_configPath))
                    return new ConfigData();

                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<ConfigData>(json) ?? new ConfigData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при загрузке конфигурации: {ex.Message}");
                return new ConfigData();
            }
        }

        /// <summary>
        /// Сохраняет полную конфигурацию в файл
        /// </summary>
        public void SaveConfig(ConfigData config)
        {
            try
            {
                var directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при сохранении конфигурации: {ex.Message}");
            }
        }

        /// <summary>
        /// Сохраняет список адресов
        /// </summary>
        public void SaveCustomAddresses(ObservableCollection<string> addresses)
        {
            var config = LoadConfig();
            config.CustomAddresses = new List<string>(addresses);
            SaveConfig(config);
        }

        /// <summary>
        /// Сохраняет последний выбранный Baudrate
        /// </summary>
        public void SaveBaudRate(string baudRate)
        {
            var config = LoadConfig();
            config.LastBaudRate = baudRate;
            SaveConfig(config);
        }

        /// <summary>
        /// Сохраняет последний выбранный адрес
        /// </summary>
        public void SaveAddress(string address)
        {
            var config = LoadConfig();
            config.LastAddress = address;
            SaveConfig(config);
        }

        public void AddAddress(ObservableCollection<string> addresses, string address)
        {
            if (!string.IsNullOrWhiteSpace(address) && !addresses.Contains(address))
            {
                addresses.Add(address);
                SaveCustomAddresses(addresses);
            }
        }

        public void RemoveAddress(ObservableCollection<string> addresses, string address)
        {
            if (addresses.Contains(address))
            {
                addresses.Remove(address);
                SaveCustomAddresses(addresses);
            }
        }
    }

    public class ConfigData
    {
        public List<string> CustomAddresses { get; set; } = new List<string>();
        public string LastBaudRate { get; set; } = "115200";
        public string LastAddress { get; set; } = "FF";
    }
}