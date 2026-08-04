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
        public bool SendCRSFPacket(byte address, ushort roll, ushort pitch, ushort yaw, ushort throttle, ushort[] chanels)
        {
            if (_port == null || !_port.IsOpen)
                return false;

            try
            {
                var packet = CrsfBuilder.PackChannels(roll, pitch, yaw, throttle, address, chanels);
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
        /// </summary>
        public static class CrsfBuilder
        {
            public const byte CRSF_FRAMETYPE_RC_CHANNELS_PACKED = 0x16;
            public const int CRSF_CHANNEL_VALUE_1000 = 191;
            public const int CRSF_CHANNEL_VALUE_2000 = 1792;
            public const int CRSF_CHANNELS = 16;

            /// <summary>
            /// Масштабирует float 0.0..1.0 в CRSF внутреннее значение (191..1792).
            /// </summary>
            public static int CrsfScale(double val)
            {
                double v = Math.Max(0.0, Math.Min(1.0, val));
                return (int)Math.Round(v * (CRSF_CHANNEL_VALUE_2000 - CRSF_CHANNEL_VALUE_1000) + CRSF_CHANNEL_VALUE_1000);
            }

            /// <summary>
            /// Преобразует PWM в микросекундах (1000..2000) в CRSF внутреннее значение.
            /// </summary>
            public static int UsToCrsf(int us)
            {
                int u = Math.Max(1000, Math.Min(2000, us));
                double ratio = (u - 1000) / 1000.0;
                return (int)Math.Round(ratio * (CRSF_CHANNEL_VALUE_2000 - CRSF_CHANNEL_VALUE_1000) + CRSF_CHANNEL_VALUE_1000);
            }

            /// <summary>
            /// Упаковывает 16 значений (каждое 0..2047) по 11 бит подряд.
            /// </summary>
            public static byte[] Pack11Bits(int[] chValues)
            {
                if (chValues.Length != CRSF_CHANNELS)
                    throw new ArgumentException($"chValues must be length {CRSF_CHANNELS}");

                uint bits = 0;
                int bitLen = 0;
                var outList = new System.Collections.Generic.List<byte>();

                foreach (int ch in chValues)
                {
                    uint val = (uint)(ch & 0x07FF);
                    bits |= (val << bitLen);
                    bitLen += 11;
                    while (bitLen >= 8)
                    {
                        outList.Add((byte)(bits & 0xFF));
                        bits >>= 8;
                        bitLen -= 8;
                    }
                }

                if (bitLen > 0)
                    outList.Add((byte)(bits & 0xFF));
                var result = new byte[22];
                int copyLen = Math.Min(22, outList.Count);
                for (int i = 0; i < copyLen; i++)
                    result[i] = outList[i];

                return result;
            }

            /// <summary>
            /// </summary>
            public static byte Crc8(byte[] data)
            {
                byte crc = 0;
                foreach (byte b in data)
                {
                    crc ^= b;
                    for (int i = 0; i < 8; i++)
                    {
                        crc = (crc & 0x80) != 0
                            ? (byte)((crc << 1) ^ 0xD5)
                            : (byte)(crc << 1);
                    }
                }
                return crc;
            }

            /// <summary>
            /// Создаёт полный CRSF кадр для отправки на полётный контроллер.
            /// </summary>
            public static byte[] PackChannels(int roll, int pitch, int yaw, int throttle, byte deviceAddr, ushort[] chanels)
            {
                int[] chVals = new int[CRSF_CHANNELS];
                chVals[0] = UsToCrsf(throttle);
                chVals[1] = UsToCrsf(yaw);
                chVals[2] = UsToCrsf(roll);
                chVals[3] = UsToCrsf(pitch);

                for (int i = 0; i < chanels.Length; i++)
                    chVals[i + 4] = UsToCrsf(chanels[i]);

                byte[] payload = Pack11Bits(chVals);
                int frameSize = payload.Length + 2; 
                byte frameType = CRSF_FRAMETYPE_RC_CHANNELS_PACKED;
                var crcInput = new byte[1 + payload.Length];
                crcInput[0] = frameType;
                Array.Copy(payload, 0, crcInput, 1, payload.Length);
                byte crc = Crc8(crcInput);

                var frame = new byte[3 + payload.Length + 1];
                frame[0] = deviceAddr;
                frame[1] = (byte)frameSize;
                frame[2] = frameType;
                Array.Copy(payload, 0, frame, 3, payload.Length);
                frame[frame.Length - 1] = crc;

                return frame;
            }
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
