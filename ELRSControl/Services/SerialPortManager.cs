using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;

namespace ELRSControl.Services
{
    public class SerialPortManager
    {
        private SerialPort _port;

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

        /// <summary>
        /// Открывает порт для коммуникации
        /// </summary>
        public bool OpenPort(string portName, int baudRate = 115200)
        {
            try
            {
                if (_port != null && _port.IsOpen)
                    _port.Close();

                _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    Handshake = Handshake.None
                };

                _port.Open();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при открытии порта: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Закрывает открытый порт
        /// </summary>
        public void ClosePort()
        {
            try
            {
                if (_port != null && _port.IsOpen)
                {
                    _port.Close();
                    _port.Dispose();
                    _port = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при закрытии порта: {ex.Message}");
            }
        }

        /// <summary>
        /// Отправляет CRSF пакет с данными управления
        /// </summary>
        public bool SendCRSFPacket(byte address, ushort roll, ushort pitch, ushort yaw, ushort throttle)
        {
            if (_port == null || !_port.IsOpen)
                return false;

            try
            {
                var packet = BuildCRSFPacket(address, roll, pitch, yaw, throttle);
                _port.Write(packet, 0, packet.Length);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка при отправке по UART: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Формирует CRSF пакет управления
        /// Формат: [Адрес][Длина][Тип][Ch0:11bits][Ch1:11bits]...[CRC]
        /// </summary>
        private byte[] BuildCRSFPacket(byte address, ushort roll, ushort pitch, ushort yaw, ushort throttle)
        {
            ushort ch0 = ScaleChannel(roll);
            ushort ch1 = ScaleChannel(pitch);
            ushort ch2 = ScaleChannel(yaw);
            ushort ch3 = ScaleChannel(throttle);
            byte[] packet = new byte[14];
            packet[0] = address; 
            packet[1] = 10; 
            packet[2] = 0x16; 
            uint packed = 0;
            packed |= ((uint)(ch0 & 0x7FF) << 0);
            packed |= ((uint)(ch1 & 0x7FF) << 11);
            packed |= ((uint)(ch2 & 0x7FF) << 22);

            packet[3] = (byte)(packed & 0xFF);
            packet[4] = (byte)((packed >> 8) & 0xFF);
            packet[5] = (byte)((packed >> 16) & 0xFF);
            packet[6] = (byte)((ch3 & 0x7FF) >> 3);
            packet[7] = (byte)(((ch3 & 0x7FF) << 5) & 0xFF);
            for (int i = 8; i < 12; i++)
            {
                packet[i] = 0;
            }
            ushort crc = 0;
            for (int i = 1; i < 12; i++)
            {
                crc += packet[i];
            }
            packet[12] = (byte)(crc & 0xFF);
            packet[13] = (byte)((crc >> 8) & 0xFF);

            return packet;
        }

        /// <summary>
        /// Масштабирует канал с диапазона 1000-2000 на CRSF диапазон 988-2047
        /// </summary>
        private ushort ScaleChannel(ushort value)
        {
            if (value < 1000) value = 1000;
            if (value > 2000) value = 2000;
            return (ushort)(988 + (value - 1000) * 1.059f);
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
