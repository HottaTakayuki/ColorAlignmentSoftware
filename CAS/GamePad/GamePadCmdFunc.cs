using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CAS
{
    public class GamePadCmdFunc
    {
        //ボタン押下タイプ
        enum PushType
        {
            NONE = 0,
            SINGLE,
            LONG,
            CONTINUE,
            LONGCONTINUE,
        }

        //CommandID
        enum CommandFunc
        {
            UNIFORMITY_MANUAL = 200,
            UNI_CURSORONOFF,
            UNI_CABINET_MODULE,
            UNI_CURSOR_UP,
            UNI_CURSOR_DOWN,
            UNI_CURSOR_RIGHT,
            UNI_CURSOR_LEFT,
            UNI_SELECT,
            UNI_LEVEL,
            UNI_COLOR,
            RED_STEPUP,
            RED_STEPUP5,
            RED_STEPDOWN,
            RED_STEPDOWN5,
            GREEN_STEPUP,
            GREEN_STEPUP5,
            GREEN_STEPDOWN,
            GREEN_STEPDOWN5,
            BLUE_STEPUP,
            BLUE_STEPUP5,
            BLUE_STEPDOWN,
            BLUE_STEPDOWN5,
            WHITE_STEPUP,
            WHITE_STEPUP5,
            WHITE_STEPDOWN,
            WHITE_STEPDOWN5,
            TESTCOMMANDA = 296,
            TESTCOMMANDB,
            TESTCOMMANDC,
            TESTCOMMANDD,
            GAP_CORRECTION_MODULE = 500,
            GAP_CURSORONOFF,
            GAP_CABINET_MODULE,
            GAP_CURSOR_UP,
            GAP_CURSOR_DOWN,
            GAP_CURSOR_RIGHT,
            GAP_CURSOR_LEFT,
            GAP_SELECT,
            GAP_LEVEL,
            GAP_TOPLEFT_UP,
            GAP_TOPLEFT_DOWN,
            GAP_TOPRIGHT_UP,
            GAP_TOPRIGHT_DOWN,
            GAP_RIGHTTOP_UP,
            GAP_RIGHTTOP_DOWN,
            GAP_RIGHTBOTTOM_UP,
            GAP_RIGHTBOTTOM_DOWN,
            GAP_BOTTOMRIGHT_UP,
            GAP_BOTTOMRIGHT_DOWN,
            GAP_BOTTOMLEFT_UP,
            GAP_BOTTOMLEFT_DOWN,
            GAP_LEFTBOTTOM_UP,
            GAP_LEFTBOTTOM_DOWN,
            GAP_LEFTTOP_UP,
            GAP_LEFTTOP_DOWN,
        }

        //コマンド実行を確定とする処理用
        private uint validKeyId = 0;
        private int keyIdCount = 0;

        //ボタン長押しでのコマンド実行処理用
        private uint waitKeyID = 0;
        private int waitCount = 0;

        //連続コマンド実行処理用
        private uint execKeyId = 0;
        private uint execCmdId = 0;

        //定数
        //Adjustment levelのステップ(selectIndex)
        private const uint stepLow = 0;
        private const uint stepHigh = 4;
        //プロファイル関連
        private const char splitChar = '+';//XMLのButtonの区切り文字
        private const string ProFileName = "GamePadCmdConf.xml";// EXEと同じ場所に置く
        //Tab別のコマンド識別用（Tab*100+CommandId）
        private const uint tabCmdIdChangeValue = 100;

        //プロパティ
        private GamePadCmdData gamepadcmdprofiledata = null;
        private GamePadViewModel gamepadviewmodel = null;
        private MainWindow windowdata = null;
        //長押し時間（1Countは10ms程度)
        private uint longpushtime = 200;
        //連続押し間隔（1Countは10ms程度)
        private uint continuepushtime = 50;
        //ボタン押下を確定するまでの時間（1Countは10ms程度)
        //複数押下の場合に同時に離さないと意図しない別コマンドを実行してしまう場合がある
        //ボタン押下を敏感に反応させないように押下確定までの時間調整用
        private uint validpushtime = 0;
        //コマンドプロファイル
        public GamePadCmdData GamePadCmdProfileData
        {
            set { gamepadcmdprofiledata = value; }
            get { return gamepadcmdprofiledata; }
        }
        //コマンド→GUI反映用のViewModel
        public GamePadViewModel GamePadViewModelData
        {
            set { gamepadviewmodel = value; }
            get { return gamepadviewmodel; }
        }
        //MainWindow（GUIボタン押下イベント発生用）
        private MainWindow WindowView
        {
            set { windowdata = value; }
            get { return windowdata; }
        }
        //長押しとする時間
        public uint LongPushTime
        {
            set { longpushtime = value; }
            get { return longpushtime; }
        }
        //連続実行間隔
        public uint ContinuePushTime
        {
            set { continuepushtime = value; }
            get { return continuepushtime; }
        }
        //ボタン押下確定時間
        public uint ValidPushTime
        {
            set { validpushtime = value; }
            get { return validpushtime; }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public GamePadCmdFunc(GamePadViewModel view, MainWindow winow)
        {
            //プロパティにセット 
            GamePadViewModelData = view;
            WindowView = winow;
            //初期で無効にしてComboboxリストが作成されれば有効
            WindowView.cmbxGamePadProfile.IsEnabled = false;
            //各プロファイル読み込み
            GamePadCmdProfileData = GamePadProfile.GetGamePadCmdProfile(ProFileName);
            if (GamePadCmdProfileData != null)
            {
                //前回起動時に選択されているプロファイルでComboboxの初期選択設定値（バインド）セット
                GamePadViewModelData.SelectGamePadProfile = GamePadCmdProfileData.SelectProFileIndex;
                //ConfigurationタブのGamePadProfileComboboxの選択項目生成
                createSelectGamePadCombobox(WindowView.cmbxGamePadProfile);
            }
        }

        /// <summary>
        /// GamePad用プロファイルパス取得
        /// </summary>
        /// <returns></returns>
        public string getGamePadProfilePath()
        {
            string filename = "";

            // 選択されているプロファイルを取得する
            if(GamePadCmdProfileData != null & GamePadCmdProfileData.ProFiles.Count() > 0)
            {
                if(GamePadCmdProfileData.ProFiles.Count() > GamePadCmdProfileData.SelectProFileIndex)
                {
                    filename = GamePadCmdProfileData.ProFiles[(int)GamePadCmdProfileData.SelectProFileIndex].FileName;
                }
            }
            return filename;
        }

        /// <summary>
        /// SelectProFileIndexに値をSetする
        /// </summary>
        /// <param name="selectIndex"></param>
        public void setGamePadProfileIndex(uint selectIndex)
        {
            GamePadCmdProfileData.SelectProFileIndex = selectIndex;
            GamePadProfile.SetSelecGamePadtProfileIndex(ProFileName,selectIndex);
        }

        /// <summary>
        /// コマンド実行初期設定
        /// </summary>
        /// <param name="gamePadData"></param>
        public void commandInit(GamePadData gamePadData)
        {
            LongPushTime = gamePadData.GamePadPara.LongPushTime;
            ContinuePushTime = gamePadData.GamePadPara.ContinuePushTime;
            ValidPushTime = gamePadData.GamePadPara.ValidPushTime;
            setGamePadCmdKeyId(gamePadData);
        }

        /// <summary>
        /// GamePadCmdデータのKeyIDに値を設定する
        /// GmaePadデータを参照してボタン押下設定内容に合わせてKeyIDを設定する
        /// </summary>
        /// <param name="gamePadData"></param>
        private void setGamePadCmdKeyId(GamePadData gamePadData)
        {
            if (gamePadData != null && GamePadCmdProfileData != null)
            {
                foreach (CommandTab tabdata in GamePadCmdProfileData.CommandTabs)
                {
                    foreach (Command cmddata in tabdata.Commands)
                    {
                        uint keyId = 0;
                        var buttonArr = cmddata.Button.Split(splitChar);
                        foreach (string bname in buttonArr)
                        {
                            foreach (ButtonData bdata in gamePadData.Buttons)
                            {
                                if (bdata.name == bname)
                                {
                                    keyId += Convert.ToUInt32(bdata.keyid, 16);
                                }
                            }
                            cmddata.KeyId = keyId;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// KeyIDからコマンドを実行する
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="longPushTime"></param>
        /// <param name="continuePushTime"></param>
        public void execCommand(uint keyId)
        {
            if (isValidKeyId(keyId))
            {
                //ボタン押下確定
                //確定したボタン押下からコマンドID取得
                uint cmdId = getCommandId(keyId);
                if (cmdId > 0)
                {
                    if (selectCommand(cmdId))
                    {
                        execCmdId = cmdId;
                        execKeyId = keyId;
                    }
                }
                if (execKeyId != keyId)
                {
                    //ボタン押下が変化したのでリセット
                    execCmdId = 0;
                }
                if (waitKeyID != keyId)
                {
                    //ボタン押下が変化したのでリセット
                    waitCount = 0;
                }
            }
        }

        /// <summary>
        /// GamePadProfileのCombobox選択項目の生成
        /// </summary>
        /// <param name="comboBoxitem"></param>
        private void createSelectGamePadCombobox(ComboBox comboBoxitem)
        {
            foreach (ProFile profile in GamePadCmdProfileData.ProFiles)
            {
                comboBoxitem.Items.Add(profile.name);
            }
            comboBoxitem.IsEnabled = (comboBoxitem.Items.Count > 0)? true : false;
        }

        /// <summary>
        /// ボタン押下(KeyID)が有効か判断
        /// ValidPushTimeカウントになるまで押下を有効としない
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        private bool isValidKeyId(uint keyId)
        {
            bool result = false;
            if (validKeyId != keyId)
            {
                //ボタン押下が変化したのでリセット
                validKeyId = keyId;
                keyIdCount = 0;
            }
            if (ValidPushTime <= keyIdCount)
            {
                result = true;
            }
            else
            {
                keyIdCount++;
            }
            return result;
        }

        /// <summary>
        /// KeyIDからコマンドIDを取得
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="longPushTime"></param>
        /// <param name="continuePushTime"></param>
        /// <returns></returns>
        private uint getCommandId(uint keyId)
        {
            uint result = 0;

            //コマンドID検索
            Command command = searchCommand(keyId);
            //押下タイプ確認
            PushType type = getPushType(command);
            switch (type)
            {
                case PushType.SINGLE://単発実行
                    if (command.id != execCmdId)
                    {
                        result = command.id;
                    }
                    break;
                case PushType.LONG://ボタン長押し実行
                    if (isLongPushButton(keyId, LongPushTime, false))
                    {
                        if (command.id != execCmdId)
                        {
                            result = command.id;
                        }
                    }
                    break;
                case PushType.CONTINUE://連続実行
                    if (command.id != execCmdId)
                    {
                        result = command.id;
                    }
                    else
                    {
                        if (isLongPushButton(keyId, ContinuePushTime, true))
                        {
                            result = command.id;
                        }
                    }
                    break;
                case PushType.LONGCONTINUE://長押しからの連続実行
                    if (command.id != execCmdId)
                    {
                        if (isLongPushButton(keyId, LongPushTime, true))
                        {
                            result = command.id;
                        }
                    }
                    else
                    {
                        if (isLongPushButton(keyId, ContinuePushTime, true))
                        {
                            result = command.id;
                        }
                    }
                    break;
                default:
                    break;
            }
            return result;
        }

        /// <summary>
        /// KeyIDからCommandを検索
        /// </summary>
        /// <param name="keyId"></param>
        /// <returns></returns>
        private Command searchCommand(uint keyId)
        {
            foreach (CommandTab commandTab in GamePadCmdProfileData.CommandTabs)
            {
                if (GamePadViewModelData.TabSelectIndex == commandTab.index)
                {
                    foreach (Command command in commandTab.Commands)
                    {
                        if (keyId == command.KeyId)
                        {
                            return command; 
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 長押しボタン押下
        /// </summary>
        /// <param name="keyId"></param>
        /// <param name="longPushTime"></param>
        /// <param name="isContinue"></param>
        /// <returns></returns>
        private bool isLongPushButton(uint keyId, uint longPushTime, bool isContinue)
        {
            bool result = false;

            waitKeyID = keyId;
            if (waitCount == longPushTime)
            {
                result = true;
                if (isContinue)
                {
                    waitCount = 0;
                }
            }
            if (waitCount <= longPushTime)
            {
                waitCount++;
            }
            return result;
        }

        /// <summary>
        /// ボタン押下タイプ取得
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private PushType getPushType(Command command)
        {
            PushType type = PushType.NONE;
            if(command != null)
            {
                bool isContinue = toBoolean(command.isContinuePush);
                bool isLongPush = toBoolean(command.isLongPush);
                if (isContinue || isLongPush)
                {
                    if (isContinue && isLongPush)
                    {
                        type = PushType.LONGCONTINUE;
                    }
                    else
                    {
                        type = (isContinue) ? PushType.CONTINUE : PushType.LONG;
                    }
                }
                else
                {
                    type = PushType.SINGLE;
                }
            }
            return type;
        }

        /// <summary>
        /// 文字列→Bool
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool toBoolean(string value)
        {
            bool result = false;

            if (!string.IsNullOrEmpty(value))
            {
                try
                {
                   result = Convert.ToBoolean(value);
                }
                catch (FormatException)
                {
                }
            }
            return result;
        }

        /// <summary>
        /// コマンドIDのコマンドを実行
        /// </summary>
        /// <param name="commandId"></param>
        /// <returns></returns>
        private bool selectCommand(uint commandId)
        {
            bool result = true;
            uint maxindex = 0;
            commandId += GamePadViewModelData.TabSelectIndex * tabCmdIdChangeValue;
            CommandFunc func = (CommandFunc)commandId;

            switch (func)
            {
                //--------------------------------------------------------------------------
                // UNIFORMITY_MANUAL 200
                //--------------------------------------------------------------------------
                case CommandFunc.UNI_CURSORONOFF:
                    GamePadViewModelData.CursorUfManual = (GamePadViewModelData.CursorUfManual == false) ? true : false;
                    break;
                case CommandFunc.UNI_CABINET_MODULE:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        if (GamePadViewModelData.IsCabinetUfManual)
                        {
                            GamePadViewModelData.IsModuleUfManual = true;
                        }
                        else
                        {
                            GamePadViewModelData.IsCabinetUfManual = true;
                        }
                    }
                    break;
                case CommandFunc.UNI_CURSOR_UP:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        WindowView.btnCursorUpUfManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.UNI_CURSOR_DOWN:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        WindowView.btnCursorDownUfManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.UNI_CURSOR_RIGHT:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        WindowView.btnCursorRightUfManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.UNI_CURSOR_LEFT:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        WindowView.btnCursorLeftUfManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.UNI_SELECT:
                    if (GamePadViewModelData.CursorUfManual)
                    {
                        WindowView.btnSelectUfManual.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.UNI_LEVEL:
                    maxindex = (uint)WindowView.cmbxLevelUfManual.Items.Count - 1;
                    if(GamePadViewModelData.SelectLevelUfManual >= maxindex)
                    {
                        GamePadViewModelData.SelectLevelUfManual = 0;
                    }
                    else
                    {
                        GamePadViewModelData.SelectLevelUfManual ++;
                    }
                    break;
                case CommandFunc.UNI_COLOR:
                    maxindex = (uint)WindowView.cmbxColorUfManual.Items.Count - 1;
                    if (GamePadViewModelData.SelectColorUfManual >= maxindex)
                    {
                        GamePadViewModelData.SelectColorUfManual = 0;
                    }
                    else
                    {
                        GamePadViewModelData.SelectColorUfManual++;
                    }
                    break;
                case CommandFunc.RED_STEPUP:
                case CommandFunc.RED_STEPUP5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.RedGainIndex = (func == CommandFunc.RED_STEPUP) ? stepLow : stepHigh;
                        WindowView.btnRedUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.RED_STEPDOWN:
                case CommandFunc.RED_STEPDOWN5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.RedGainIndex = (func == CommandFunc.RED_STEPDOWN) ? stepLow : stepHigh;
                        WindowView.btnRedDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GREEN_STEPUP:
                case CommandFunc.GREEN_STEPUP5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.GreenGainIndex = (func == CommandFunc.GREEN_STEPUP) ? stepLow : stepHigh;
                        WindowView.btnGreenUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GREEN_STEPDOWN:
                case CommandFunc.GREEN_STEPDOWN5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.GreenGainIndex = (func == CommandFunc.GREEN_STEPDOWN) ? stepLow : stepHigh;
                        WindowView.btnGreenDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.BLUE_STEPUP:
                case CommandFunc.BLUE_STEPUP5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.BlueGainIndex = (func == CommandFunc.BLUE_STEPUP) ? stepLow : stepHigh;
                        WindowView.btnBlueUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.BLUE_STEPDOWN:
                case CommandFunc.BLUE_STEPDOWN5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.BlueGainIndex = (func == CommandFunc.BLUE_STEPDOWN) ? stepLow : stepHigh;
                        WindowView.btnBlueDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.WHITE_STEPUP:
                case CommandFunc.WHITE_STEPUP5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.WhiteGainIndex = (func == CommandFunc.WHITE_STEPUP) ? stepLow : stepHigh;
                        WindowView.btnWhiteUp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.WHITE_STEPDOWN:
                case CommandFunc.WHITE_STEPDOWN5:
                    if (WindowView.gdAdjLevel.IsEnabled)
                    {
                        GamePadViewModelData.WhiteGainIndex = (func == CommandFunc.WHITE_STEPDOWN) ? stepLow : stepHigh;
                        WindowView.btnWhiteDown.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                //--------------------------------------------------------------------------
                // GAP_CORRECTION_MODULE 500
                //--------------------------------------------------------------------------
                case CommandFunc.GAP_CURSORONOFF:
                    GamePadViewModelData.CursorGapCell = (GamePadViewModelData.CursorGapCell == false) ? true : false;
                    break;
                case CommandFunc.GAP_CABINET_MODULE:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        if (GamePadViewModelData.IsCabinetGapCell)
                        {
                            GamePadViewModelData.IsModuleGapCell = true;
                        }
                        else
                        {
                            GamePadViewModelData.IsCabinetGapCell = true;
                        }
                    }
                    break;
                case CommandFunc.GAP_CURSOR_UP:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        WindowView.btnCursorUpGapCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_CURSOR_DOWN:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        WindowView.btnCursorDownGapCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_CURSOR_RIGHT:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        WindowView.btnCursorRightGapCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_CURSOR_LEFT:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        WindowView.btnCursorLeftGapCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_SELECT:
                    if (GamePadViewModelData.CursorGapCell)
                    {
                        WindowView.btnSelectGapCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_LEVEL:
                    maxindex = (uint)WindowView.cmbxLevelGapCell.Items.Count - 1;
                    if (GamePadViewModelData.SelectLevelGapCell >= maxindex)
                    {
                        GamePadViewModelData.SelectLevelGapCell = 0;
                    }
                    else
                    {
                        GamePadViewModelData.SelectLevelGapCell++;
                    }
                    break;
                case CommandFunc.GAP_TOPLEFT_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapTopLeftUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_TOPLEFT_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapTopLeftDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_TOPRIGHT_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapTopRightUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_TOPRIGHT_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapTopRightDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_RIGHTTOP_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapRightTopUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_RIGHTTOP_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapRightTopDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_RIGHTBOTTOM_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapRightBottomUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_RIGHTBOTTOM_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapRightBottomDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_BOTTOMRIGHT_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapBottomRightUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_BOTTOMRIGHT_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapBottomRightDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_BOTTOMLEFT_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapBottomLeftUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_BOTTOMLEFT_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapBottomLeftDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_LEFTBOTTOM_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapLeftBottomUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_LEFTBOTTOM_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapLeftBottomDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_LEFTTOP_UP:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapLeftTopUpCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                case CommandFunc.GAP_LEFTTOP_DOWN:
                    if (WindowView.gdGapCellCorrectValue.IsEnabled)
                    {
                        WindowView.btnGapLeftTopDownCell.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    }
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }
    }
}
