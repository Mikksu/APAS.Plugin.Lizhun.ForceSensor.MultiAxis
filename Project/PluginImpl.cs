using System;
using System.Configuration;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using APAS.Plugin.KEYTHLEY._2600;
using APAS.Plugin.LiZhun.ForceSensor.MultiAxis.Views;
using APAS.Plugin.Sdk.Base;
using APAS.ServiceContract.Wcf;
using Modbus.Device;

namespace APAS.Plugin.LiZhun.ForceSensor.MultiAxis
{
    /// <inheritdoc />
    public class PluginImpl : PluginMultiChannelMeasurableEquipment
    {
        #region Variables

        private const int MAX_CH = 3;
        
        private IModbusSerialMaster _modbusMaster;
        private SerialPort _serialPort;
        
        /// <summary>
        /// how long it takes to wait between the two sampling points.
        /// </summary>
        private readonly int _pollingIntervalMs = 200;

        /// <summary>
        /// Modbus从地址
        /// </summary>
        private int _slaveId;
        
        /// <summary>
        /// 起始寄存器地址
        /// </summary>
        private int _regStart;

        private Task _bgTask;
        private CancellationTokenSource _cts;
        private CancellationToken _ct;
        private bool _isInit;
        private readonly IProgress<(double force1, double force2, double force3)> _rtValuesUpdatedReporter;
        
        #endregion

        #region Constructors

        public PluginImpl(ISystemService apasService, string caption) 
            : base(Assembly.GetExecutingAssembly(), apasService, caption, MAX_CH, new[] {"CH1", "CH2", "CH3" })
        {
            var config = GetAppConfig();
            LoadConfigItem(config, "ReadIntervalMillisec", out _pollingIntervalMs, 200);
            
            UserView = new PluginView
            {
                DataContext = this
            };

            HasView = true;

            //! the progress MUST BE defined in the ctor since
            //! we operate the UI elements in the OnCommOneShot event.
            _rtValuesUpdatedReporter = new Progress<(double force1, double force2, double force3)>(values =>
            {
                Force1 = values.force1;
                Force2 = values.force2;
                Force3 = values.force3;
                
            });
        }

        #endregion

        #region Properties

        public override string ShortCaption => "压力传感器";

        public override string Description => "压力传感器控制插件";

        // public override string Usage =>
        //     "压力传感器控制插件。\n" +
        //     "Fetch(0)：CH1实时压力。\n" +
        //     "Fetch(1)：CH1实时压力。\n" +
        //     "Fetch(2)：CH2实时压力。\n" +
        //     "支持的命令: \n" +
        //     "无\n"

        public override bool IsInitialized
        {
            get => _isInit;
            protected set => SetProperty(ref _isInit, value);
        }

        private double _force1;
        public double Force1
        {
            get => _force1;
            private set => SetProperty(ref _force1, value);
        }
        
        private double _force2;
        public double Force2
        {
            get => _force2;
            private set => SetProperty(ref _force2, value);
        }
        
        private double _force3;
        public double Force3
        {
            get => _force3;
            private set => SetProperty(ref _force3, value);
        }
       
        #endregion

        #region Methods

        public sealed override async Task<object> Execute(object args)
        {
            return await Task.FromResult(default(object));
        }

        /// <summary>
        /// Switch to the specific channel.
        /// </summary>
        /// <param name="param">[int] The specific channel.</param>
        /// <returns></returns>
        public sealed override async Task Control(string param)
        {
            /*if (!IsInitialized)
                throw new InvalidOperationException("KEITHLEY 2600未初始化。");*/
            await Task.CompletedTask;
        }

        public override void Dispose()
        {
            _stopBackgroundTask();
            _modbusMaster.Dispose();
            _serialPort.Close();
            _serialPort.Dispose();
        }
        
        public override object Fetch(int channel)
        {
            if (channel >= 0 && channel < MaxChannel)
            {
                var result = FetchAll();
                return result[channel];
            }

            throw new ArgumentOutOfRangeException(nameof(channel));
        }

        public override object[] FetchAll()
        {
            var data = _modbusMaster.ReadHoldingRegisters((byte)_slaveId, (ushort)_regStart, 6);
            var force1 = ConvertUShortToFloat(new[] { data[0], data[1] });
            var force2 = ConvertUShortToFloat(new[] { data[2], data[3] });
            var force3 = ConvertUShortToFloat(new[] { data[4], data[5] });
            _rtValuesUpdatedReporter.Report((force1, force2, force3));
            return new object[] { force1, force2, force3 };
        }

        public override object Fetch()
        {
                var ret = FetchAll();
                if(ret.Length > 1)
                    return ret[0];

                return null;
        }
        
        public override object DirectFetch()
        {
            return Fetch();
        }

        public override bool Init()
        {
            try
            {
                _stopBackgroundTask();

                IsInitialized = false;
                IsEnabled = false;

                InitSensor();

                IsInitialized = true;
                IsEnabled = true;

                _startBackgroundTask(_rtValuesUpdatedReporter);

                return true;
            }
            catch (Exception)
            {
                throw;
            }

        }

        public override void StartBackgroundTask()
        {
            // Do nothing}
        }

        public override void StopBackgroundTask()
        {
            // Do nothing
        }

        #endregion

        #region Private Methods

        private void InitSensor()
        {
            var config = GetAppConfig();
            LoadConfigItem(config, "Port", out var port, "COM1");
            LoadConfigItem(config, "BaudRate", out var baudRate, 9600);
            LoadConfigItem(config, "SlaveId", out _slaveId, 1);
            LoadConfigItem(config, "RegStart", out _regStart, 256);
            
            _serialPort = new SerialPort(port, baudRate, Parity.None, 8, StopBits.One);
            _modbusMaster = ModbusSerialMaster.CreateRtu(_serialPort);
            _modbusMaster.Transport.ReadTimeout = 100;
            _modbusMaster.Transport.WriteTimeout = 100;
            _modbusMaster.Transport.Retries = 3;
            _modbusMaster.Transport.WaitToRetryMilliseconds = 10;
        }

        private float ConvertUShortToFloat(ushort[] ushortArray, bool isSwapByte = false)
        {
            // 创建一个字节数组来存储四个字节（float是32位，4字节）
            var byteArray = new byte[4];

            if (isSwapByte)
            {
                // 将ushort转换为字节并存储到字节数组中（小端序）
                byteArray[0] = (byte)((ushortArray[1] >> 8) & 0xFF); // 低位字节
                byteArray[1] = (byte)(ushortArray[1] & 0xFF); // 高位字节
                byteArray[2] = (byte)((ushortArray[0] >> 8) & 0xFF); // 低位字节
                byteArray[3] = (byte)(ushortArray[0] & 0xFF); // 高位字节
            }
            else
            {
                // 将ushort转换为字节并存储到字节数组中（小端序）
                byteArray[0] = (byte)(ushortArray[1] & 0xFF); // 低位字节
                byteArray[1] = (byte)((ushortArray[1] >> 8) & 0xFF); // 高位字节
                byteArray[2] = (byte)(ushortArray[0] & 0xFF); // 低位字节
                byteArray[3] = (byte)((ushortArray[0] >> 8) & 0xFF); // 高位字节
            }

            // 将字节数组转换为float
            var result = BitConverter.ToSingle(byteArray, 0);
            return result;
        }
        
        private void _startBackgroundTask(IProgress<(double force1, double force2, double force3)> progress = null)
        {
            if (_bgTask == null || _bgTask.IsCompleted)
            {
                _cts = new CancellationTokenSource();
                _ct = _cts.Token;

                _bgTask = Task.Run(() =>
                {
                    // wait for 2s to ensure the UI is initialized completely.
                    Thread.Sleep(2000);

                    while (true)
                    {
                        try
                        {
                            Fetch();
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        if (_ct.IsCancellationRequested)
                            return;

                        Thread.Sleep(_pollingIntervalMs);

                        if (_ct.IsCancellationRequested)
                            return;
                    }
                }, _ct);
            }
        }

        private void _stopBackgroundTask()
        {
            if (_bgTask != null)
            {
                // 结束背景线程
                _cts?.Cancel();

                //! 延时，确保背景线程正确退出
                Thread.Sleep(500);

                _bgTask = null;
            }

            IsInitialized = false;
            IsEnabled = false;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Re-connect to the keithley 2602B
        /// </summary>
        public RelayCommand ReConnCommand
        {
            get
            {
                return new RelayCommand(x =>
                {
                    try
                    {
                        _stopBackgroundTask();
                        Init();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"无法连接2606B，{ex.Message}", "错误",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
        }
        
        #endregion
    }
}
