using System;
using System.Collections.ObjectModel;
using System.IO.Ports;

namespace ELRSControl.Services
{
    public class SerialPortManager
    {
        /// <summary>
        /// Получает список доступных COM портов с описанием
        /// </summary>
        public static ObservableCollection<SerialPortInfo> GetAvailablePorts()
        {
            var ports = new ObservableCollection<SerialPortInfo>();

            try
            {
                var portNames = SerialPort.GetPortNames();

                foreach (var portName in portNames)
                {
                    ports.Add(new SerialPortInfo { PortName = portName, Description = portName });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при получении портов: {ex.Message}");
            }

            return ports;
        }

        /// <summary>
        /// Проверяет, доступен ли порт для открытия
        /// </summary>
        public static bool IsPortAvailable(string portName)
        {
            try
            {
                using (var port = new SerialPort(portName))
                {
                    port.Open();
                    port.Close();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Информация о COM порте
    /// </summary>
    public class SerialPortInfo
    {
        public string PortName { get; set; }
        public string Description { get; set; }

        public override string ToString() => $"{PortName} - {Description}";
    }
}
