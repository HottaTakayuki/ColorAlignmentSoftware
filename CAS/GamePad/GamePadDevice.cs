using System;
using System.Linq;
using System.Threading.Tasks;

//HID関連
using Windows.Devices.Enumeration;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Collections.Generic;
using System.Windows.Threading;

namespace CAS
{
    public class GamePadDevice
    {
        //HIDデバイス関連
        private static HidDevice device;
        private ushort usagepage = 0x0001;//デバイス検索パラ：Generic DispTop Control
        private ushort usageid = 0x0005;//デバイス検索パラ：Game Pad
        private int devicecount = 0;
        private int deviceindex = -1;
        private DeviceInformationCollection devices = null;
        private GamePadViewModel gamepadviewmodel = null;

        private static object lockData = new object(); //ロック処理に必要

        //プロパティ
        private GamePadData gamepadprofiledata = null;
        private uint gamepadkeyid = 0;
        public GamePadData GamePadProfileData
        {
            set { gamepadprofiledata = value; }
            get { return gamepadprofiledata; }
        }
        //Game PadのKeyID（MainWindow側のタイマーで参照される
        public uint GamePadKeyId
        {
            set
            {
                lock (lockData)
                {
                    gamepadkeyid = value;
                }
            }
            get
            {
                lock (lockData)
                {
                    return gamepadkeyid;
                }
            }
        }

        //GUI表示用のViewModel(Game Pad Status表示のみ）
        public GamePadViewModel GamePadViewModelData
        {
            set { gamepadviewmodel = value; }
            get { return gamepadviewmodel; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GamePadDevice(string gamePadProfilePath, GamePadViewModel view)
        {
            //プロパティにセット 
            GamePadViewModelData = view;
            //各プロファイル読み込み
            GamePadProfileData = GamePadProfile.GetGamePadProfile(gamePadProfilePath);
        }

        // タイマーのインスタンス
        private DispatcherTimer hidDeviceTimer = null;
        // 間隔(ms)
        private const ushort hidDevicePollingTime = 5000;

        /// <summary>
        /// タイマの設定と開始
        /// </summary>
        public void startHidDeviceTimer()
        {
            if(hidDeviceTimer == null)
            {
                // タイマーのインスタンスを生成
                hidDeviceTimer = new DispatcherTimer(); // 優先度はDispatcherPriority.Background
                // インターバルを設定
                hidDeviceTimer.Interval = TimeSpan.FromMilliseconds(hidDevicePollingTime);
                // タイマメソッドを設定
                hidDeviceTimer.Tick += new EventHandler(hidDevice);
            }
            // タイマを開始
            hidDeviceTimer.Start();

        }

        /// <summary>
        /// インターバル経過したら呼び出される
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hidDevice(object sender, EventArgs e)
        {
            Task task = connectHIDInput();
        }

        /// <summary>
        /// 対象デバイス状態を確認する（再接続処理あり）
        /// </summary>
        /// <returns></returns>
        public async Task connectHIDInput()
        {
            await Task.Run(() => serchHIDDevice());
            int index = getDeviceIndex();
            if (index == -1)
            {
                //該当のデバイスが見つからない
                if (deviceindex >= 0)
                {
                    //接続が切れたのでDispose
                    disposeDevice();
                }
                deviceindex = -1;
                // Game Pad Status 非表示
                isDispGamePadStatus(false);
            }
            else
            {
                await Task.Run(() => startHIDInput(index));
            }
        }

        /// <summary>
        /// HIDデバイス検索
        /// </summary>
        /// <returns></returns>
        public async Task serchHIDDevice()
        {
            // 接続されているGamePadの検索
            string selector = HidDevice.GetDeviceSelector(usagepage, usageid);
            devices = await DeviceInformation.FindAllAsync(selector);
            devicecount = (devices != null) ? devices.Count() : 0;
        }

        /// <summary>
        /// ProfileのPIDとVIDに一致するHIDデバイスのIndexを取得
        /// </summary>
        /// <returns></returns>
        private int getDeviceIndex()
        {
            int index = -1;
            if (GamePadProfileData != null)
            {
                if(devicecount > 0)
                {
                    if(GamePadProfileData.GamePadInfo.ProductIds.Count() == 0 ||
                       string.IsNullOrEmpty(GamePadProfileData.GamePadInfo.VendorId))
                    {
                        //ProfileにProductID、VenderIDの指定が無い場合は最初に見つけたものを使用する
                        index = 0;
                    }
                    else
                    {
                        //ProductID、VenderIDの一致するデバイスを検索
                        string chkVid = GamePadProfileData.GamePadInfo.VendorId;
                        for (int i = 0; i < devices.Count(); i++)
                        {
                            for(int j = 0; j < GamePadProfileData.GamePadInfo.ProductIds.Count(); j++)
                            {
                                string chkPid = GamePadProfileData.GamePadInfo.ProductIds[j];
                                if (devices[i].Id.Contains(chkPid) && devices[i].Id.Contains(chkVid))
                                {
                                    index = i;
                                    break;
                                }
                            }
                            if(index > -1)
                            {
                                break;
                            }
                        }
                    }
                }
            }
            return index;
        }

        /// <summary>
        /// reStart HID device input
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public async Task startHIDInput(int index)
        {
            if (devicecount > 0)
            {
                if (deviceindex < 0)
                {
                    try
                    {
                        //現在未接続状態なので接続する
                        deviceindex = index;
                        //deviceオープン
                        device = await HidDevice.FromIdAsync(devices.ElementAt(deviceindex).Id, FileAccessMode.Read);
                        //InputReportイベントハンドラ
                        device.InputReportReceived += deviceReceived;
                        // Game Pad Status 表示
                        isDispGamePadStatus(true);
                    }
                    catch (Exception)
                    {
                        // 失敗したので未接続状態
                        deviceindex = -1;
                        // Game Pad Status 非表示
                        isDispGamePadStatus(false);
                    }
                }
            }
        }

        /// <summary>
        /// Game Pad Statusの表示/非表示
        /// </summary>
        /// <param name="IsDisp"></param>
        private void isDispGamePadStatus(bool IsDisp)
        {
            GamePadViewModelData.GamePadStatus = (IsDisp) ? "Visible" : "Hidden";
        }

        /// <summary>
        /// デバイスのボタン押下に呼び出される（イベントハンドラ登録）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="arg"></param>
        public void deviceReceived(HidDevice sender, HidInputReportReceivedEventArgs arg)
        {
            //InputReport取得
            HidInputReport inputReport = arg.Report;
            if(inputReport.Data.Length > 0)
            {
                var bytes = new byte[inputReport.Data.Length];
                DataReader reader = DataReader.FromBuffer(inputReport.Data);
                reader.ReadBytes(bytes);

                GamePadKeyId = getButtonStatus(bytes, inputReport.Data.Length);
            }
        }

        /// <summary>
        /// ボタンの押下状態取得（複数押下）
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private uint getButtonStatus(byte[] bytes, uint size)
        {
            uint keyId = 0;
            if (GamePadProfileData != null)
            {
                foreach (ButtonData bdata in GamePadProfileData.Buttons)
                {
                    if (isPushButton(bdata.Datas, bytes, size))
                    {
                        keyId += Convert.ToUInt32(bdata.keyid, 16);
                    }
                }
            }
            return keyId;
        }

        /// <summary>
        /// 対象のボタンが押されているかを確認
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="bytes"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private bool isPushButton(List<Data> datas, byte[] bytes, uint size)
        {
            bool isOK = true;

            //チェック対象がすべて一致でTrueとなる
            foreach (Data data in datas)
            {
                if (data.Number < size)
                {
                    byte chkData;
                    //チェックするデータのMask（Mask指定がない場合はそのままの値）
                    chkData = (string.IsNullOrEmpty(data.Mask)) ? bytes[data.Number] : (byte)(bytes[data.Number] & Convert.ToByte(data.Mask, 16));

                    if (data.Value.IndexOf("0x", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        //16進数指定なのでビット比較する
                        if ((Convert.ToByte(data.Value, 16) & chkData) == 0)
                        {
                            isOK = false;
                            break;
                        }
                    }
                    else
                    {
                        //10進数指定なので一致チェックする
                        if (Convert.ToByte(data.Value, 10) != chkData)
                        {
                            isOK = false;
                            break;
                        }
                    }
                }
            }
            return isOK;
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        public void dispose()
        {
            timerStop();
            disposeDevice();
        }

        /// <summary>
        /// タイマー停止
        /// </summary>
        private void timerStop()
        {
            if(hidDeviceTimer != null)
            {
                hidDeviceTimer.Stop();
            }
        }

        /// <summary>
        /// HIDデバイスを閉じる
        /// </summary>
        private void disposeDevice()
        {
            try
            {
                if(device != null)
                {
                    //InputReportイベントハンドラ削除
                    device.InputReportReceived -= deviceReceived;
                    device.Dispose();
                    device = null;
                }
                devicecount = 0;
                deviceindex = -1;
                // Game Pad Status 非表示
                isDispGamePadStatus(false);
            }
            catch (Exception)
            {
            }
        }
    }

}
