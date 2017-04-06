﻿// Copyright (c) 2017 TrakHound Inc., All Rights Reserved.

// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrakHound.Api.v2;
using Requests = TrakHound.Api.v2.Requests;


namespace TrakHound.DeviceMonitor
{
    /// <summary>
    /// Interaction logic for DeviceItem.xaml
    /// </summary>
    public partial class DeviceItem : UserControl
    {
        private ManualResetEvent stop;
        private int interval = 30000;
        private string _deviceId;
        private bool previousConnected;
        private Requests.Samples.Stream samplesStream;
        private Requests.Activity.Stream activityStream;
        private Requests.Alarms.Stream alarmStream;

        private List<string> dataItemIds = new List<string>();


        private ObservableCollection<StatusItem> _statusItems;
        public ObservableCollection<StatusItem> StatusItems
        {
            get
            {
                if (_statusItems == null) _statusItems = new ObservableCollection<StatusItem>();
                return _statusItems;
            }
            set
            {
                _statusItems = value;
            }
        }


        public bool Connected
        {
            get { return (bool)GetValue(ConnectedProperty); }
            set { SetValue(ConnectedProperty, value); }
        }

        public static readonly DependencyProperty ConnectedProperty =
            DependencyProperty.Register("Connected", typeof(bool), typeof(DeviceItem), new PropertyMetadata(false));


        public string DeviceId
        {
            get { return (string)GetValue(DeviceIdProperty); }
            set { SetValue(DeviceIdProperty, value); }
        }

        public static readonly DependencyProperty DeviceIdProperty =
            DependencyProperty.Register("DeviceId", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Address
        {
            get { return (string)GetValue(AddressProperty); }
            set { SetValue(AddressProperty, value); }
        }

        public static readonly DependencyProperty AddressProperty =
            DependencyProperty.Register("Address", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Port
        {
            get { return (string)GetValue(PortProperty); }
            set { SetValue(PortProperty, value); }
        }

        public static readonly DependencyProperty PortProperty =
            DependencyProperty.Register("Port", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string PhysicalAddress
        {
            get { return (string)GetValue(PhysicalAddressProperty); }
            set { SetValue(PhysicalAddressProperty, value); }
        }

        public static readonly DependencyProperty PhysicalAddressProperty =
            DependencyProperty.Register("PhysicalAddress", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string DeviceName
        {
            get { return (string)GetValue(DeviceNameProperty); }
            set { SetValue(DeviceNameProperty, value); }
        }

        public static readonly DependencyProperty DeviceNameProperty =
            DependencyProperty.Register("DeviceName", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Manufacturer
        {
            get { return (string)GetValue(ManufacturerProperty); }
            set { SetValue(ManufacturerProperty, value); }
        }

        public static readonly DependencyProperty ManufacturerProperty =
            DependencyProperty.Register("Manufacturer", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Model
        {
            get { return (string)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string Serial
        {
            get { return (string)GetValue(SerialProperty); }
            set { SetValue(SerialProperty, value); }
        }

        public static readonly DependencyProperty SerialProperty =
            DependencyProperty.Register("Serial", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public ImageSource DeviceImage
        {
            get { return (ImageSource)GetValue(DeviceImageProperty); }
            set { SetValue(DeviceImageProperty, value); }
        }

        public static readonly DependencyProperty DeviceImageProperty =
            DependencyProperty.Register("DeviceImage", typeof(ImageSource), typeof(DeviceItem), new PropertyMetadata(null));


        public ImageSource DeviceLogo
        {
            get { return (ImageSource)GetValue(DeviceLogoProperty); }
            set { SetValue(DeviceLogoProperty, value); }
        }
        public static readonly DependencyProperty DeviceLogoProperty =
            DependencyProperty.Register("DeviceLogo", typeof(ImageSource), typeof(DeviceItem), new PropertyMetadata(null));


        public string EmergencyStop
        {
            get { return (string)GetValue(EmergencyStopProperty); }
            set { SetValue(EmergencyStopProperty, value); }
        }

        public static readonly DependencyProperty EmergencyStopProperty =
            DependencyProperty.Register("EmergencyStop", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public string DeviceStatus
        {
            get { return (string)GetValue(DeviceStatusProperty); }
            set { SetValue(DeviceStatusProperty, value); }
        }

        public static readonly DependencyProperty DeviceStatusProperty =
            DependencyProperty.Register("DeviceStatus", typeof(string), typeof(DeviceItem), new PropertyMetadata(null));


        public DeviceItem()
        {
            Init();
        }

        public DeviceItem(Api.v2.Data.ConnectionDefinition connection)
        {
            Init();
            DeviceId = connection.DeviceId;
            _deviceId = connection.DeviceId;

            Address = connection.Address;
            Port = connection.Port.ToString();
            PhysicalAddress = connection.PhysicalAddress;
        }

        private void Init()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Start()
        {
            stop = new ManualResetEvent(false);

            var thread = new Thread(new ThreadStart(Worker));
            thread.Start();
        }

        public void Stop()
        {          
            if (stop != null) stop.Set();
            if (samplesStream != null) samplesStream.Stop();
            if (activityStream != null) activityStream.Stop();
            if (alarmStream != null) alarmStream.Stop();
        }

        private void Worker()
        {
            var model = Requests.Model.Get(MainWindow._apiUrl, _deviceId, MainWindow._apiToken);
            UpdateModel(model);

            do
            {
                var status = Requests.Status.Get(MainWindow._apiUrl, _deviceId, MainWindow._apiToken);
                if (status != null)
                {
                    if (!stop.WaitOne(0, true) && status.Connected && !previousConnected)
                    {
                        StartActivityStream(null);
                        StartAlarmStream(null);
                        StartSamplesStream(null);
                    }

                    UpdateStatus(status);
                    previousConnected = status.Connected;
                }
            } while (!stop.WaitOne(interval, true));
        }

  
        public void UpdateStatus(Api.v2.Data.Status status)
        {
            if (status != null)
            {
                Dispatcher.BeginInvoke(new Action(() => 
                {
                    Connected = status.Connected;

                    foreach (var statusItem in StatusItems)
                    {
                        statusItem.Connected = status.Connected;
                    }
                }));
            }
        }

        public void UpdateModel(Api.v2.Data.DeviceModel model)
        {
            if (model != null)
            {
                byte[] imgBytes = null;
                byte[] logoBytes = null;

                // Download Images using the TrakHound Images Api
                if (!string.IsNullOrEmpty(model.Manufacturer))
                {
                    logoBytes = DownloadImage("http://images.trakhound.com/device-image?manufacturer=" + model.Manufacturer);
                    if (!string.IsNullOrEmpty(model.Model)) imgBytes = DownloadImage("http://images.trakhound.com/device-image?manufacturer=" + model.Manufacturer + "&model=" + model.Model);
                }

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var img = BitmapFromBytes(imgBytes);
                    if (img != null) DeviceImage = SetImageSize(img, 50, 40);

                    var logo = BitmapFromBytes(logoBytes);
                    if (logo != null) DeviceLogo = SetImageSize(logo, 0, 17);
                    else Manufacturer = model.Manufacturer;

                    DeviceName = model.Name;
                    Model = model.Model;
                    Serial = model.SerialNumber;

                    var dataItems = model.GetDataItems();

                    Api.v2.Data.DataItem execution = null;
                    Api.v2.Data.DataItem controllerMode = null;

                    bool multipleStatuses = false;

                    var estop = model.GetDataItems().Find(o => o.Type == "EMERGENCY_STOP");

                    // Add Alarm Conditions
                    dataItemIds.AddRange(model.GetDataItems().FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

                    // Get Paths
                    var paths = model.GetComponents().FindAll(o => o.Type == "Path");
                    if (paths != null)
                    {
                        foreach (var path in paths)
                        {
                            if (path.DataItems.Exists(o => o.Type == "EXECUTION"))
                            {
                                multipleStatuses = true;

                                // Create a new StatusItem control
                                var statusItem = new StatusItem(path);
                                if (estop != null) statusItem.EmergencyStopId = estop.Id;
                                dataItemIds.AddRange(statusItem.GetIds());
                                StatusItems.Add(statusItem);
                            }
                        }
                    }

                    if (!multipleStatuses)
                    {
                        execution = model.GetDataItems().Find(o => o.Type == "EXECUTION");
                        controllerMode = model.GetDataItems().Find(o => o.Type == "CONTROLLER_MODE");

                        if (execution != null || controllerMode != null)
                        {
                            var statusItem = new StatusItem();

                            var obj = model.GetDataItems().Find(o => o.Type == "EXECUTION");
                            if (obj != null) statusItem.ExecutionId = obj.Id;

                            obj = model.GetDataItems().Find(o => o.Type == "CONTROLLER_MODE");
                            if (obj != null) statusItem.ControllerModeId = obj.Id;

                            obj = model.GetDataItems().Find(o => o.Type == "MESSAGE");
                            if (obj != null) statusItem.MessageId = obj.Id;

                            // Get Alarm Condition IDs
                            statusItem.AlarmIds.AddRange(model.GetDataItems().FindAll(o => o.Category == "CONDITION").Select(o => o.Id));

                            var programItem = new ProgramItem();

                            // Get Program ID
                            obj = model.GetDataItems().Find(o => o.Type == "PROGRAM");
                            if (obj != null) programItem.ProgramId = obj.Id;

                            // Get Feedrate Override ID
                            obj = model.GetDataItems().Find(o => o.Type == "PATH_FEEDRATE_OVERRIDE" || (o.Type == "PATH_FEEDRATE" && o.Units == "PERCENT"));
                            if (obj != null) programItem.FeedrateOverrideId = obj.Id;

                            paths = model.GetComponents().FindAll(o => o.Type == "Path");
                            foreach (var path in paths)
                            {
                                programItem.PathItems.Add(new PathItem(path));
                            }

                            statusItem.ProgramItem = programItem;
                            if (estop != null) statusItem.EmergencyStopId = estop.Id;
                            dataItemIds.AddRange(statusItem.GetIds());
                            StatusItems.Add(statusItem);
                        } 
                    }


                    if (samplesStream != null) samplesStream.DataItemIds = dataItemIds.ToArray();
                }));
            }
        }

        private void StartActivityStream(object o)
        {
            activityStream = new Requests.Activity.Stream(MainWindow._apiUrl, _deviceId, 1000, MainWindow._apiToken);
            activityStream.ActivityReceived += Stream_ActivityReceived;
            activityStream.Start();
        }

        private void Stream_ActivityReceived(Api.v2.Data.ActivityItem activity)
        {
            if (activity.Events != null)
            {
                // Get EmergencyStop
                var estop = activity.Events.Find(o => o.Name == "Emergency Stop");
                if (estop != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => { EmergencyStop = estop.Value; }));
                }

                // Get Status
                var status = activity.Events.Find(o => o.Name == "Status");
                if (status != null)
                {
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        DeviceStatus = status.Value;

                        foreach (var statusItem in StatusItems)
                        {
                            statusItem.DeviceStatus = status.Value;
                            statusItem.StatusTimestamp = status.Timestamp;
                        }
                    }));
                }
            }
        }

        private void StartSamplesStream(object o)
        {
            samplesStream = new Requests.Samples.Stream(MainWindow._apiUrl, _deviceId, 500, dataItemIds.ToArray(), MainWindow._apiToken);
            samplesStream.SampleReceived += Stream_SampleReceived;
            samplesStream.Start();
        }

        private void Stream_SampleReceived(Api.v2.Data.Sample sample)
        {
            Dispatcher.BeginInvoke(new Action(() => {

                foreach (var statusItem in StatusItems) statusItem.Update(sample);
            }));
        }

        private void StartAlarmStream(object o)
        {
            alarmStream = new Requests.Alarms.Stream(MainWindow._apiUrl, _deviceId, 2000, MainWindow._apiToken);
            alarmStream.AlarmReceived += Stream_AlarmReceived;
            alarmStream.Start();
        }

        private void Stream_AlarmReceived(Api.v2.Data.Alarm alarm)
        {
            Dispatcher.BeginInvoke(new Action(() => {

                foreach (var statusItem in StatusItems)
                {
                    if (statusItem.AlarmIds.Exists(o => o == alarm.DataItemId))
                    {
                        statusItem.Update(alarm);
                    }
                }
            }));
        }

        private byte[] DownloadImage(string url)
        {
            try
            {
                var client = new RestClient(url);
                var request = new RestRequest();
                return client.DownloadData(request);   
            }
            catch (Exception ex)
            {
                
            }            

            return null;
        }

        private BitmapImage BitmapFromBytes(byte[] bytes)
        {
            if (!bytes.IsNullOrEmpty())
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = stream;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                    return img;
                }
            }

            return null;
        }

        public static BitmapImage SetImageSize(ImageSource src, int width, int height)
        {
            if (src != null)
            {
                var encoder = new PngBitmapEncoder();
                var stream = new MemoryStream();
                var img = new BitmapImage();
                var bmp = src as BitmapSource;
                if (bmp != null)
                {
                    encoder.Frames.Add(BitmapFrame.Create(bmp));
                    encoder.Save(stream);

                    img.BeginInit();
                    if (width > 0) img.DecodePixelWidth = width;
                    if (height > 0) img.DecodePixelHeight = height;
                    img.StreamSource = new MemoryStream(stream.ToArray());
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();

                    stream.Close();

                    return img;
                }
            }

            return null;
        }
    }
}