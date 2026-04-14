using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CAS
{
    public class GamePadViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName]string propertyName = null)
           => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        // バインド用プロパティ 
        private bool cursorkufmanual = false;
        private bool iscabinetufmanual = true;
        private bool ismoduleufmanual = false;
        private uint selectlevelufmanual = 0;
        private uint seleccolorufmanual = 0;
        private uint tabselectindex = 0;
        private uint redgainindex = 0;
        private uint greengainindex = 0;
        private uint bluegainindex = 0;
        private uint whitegainindex = 0;
        private bool cursorgapcell = false;
        private bool iscabinetgapcell = true;
        private bool ismodulegapcell = false;
        private uint selectlevelgapcell = 0;
        private string gamepadstatus = "Hidden";
        private uint selectgamepadprofile = 0;

        // Uf Manual Cursor ON/OFF
        public bool CursorUfManual
        {
            get { return cursorkufmanual; }
            set
            {
                if (cursorkufmanual != value)
                {
                    cursorkufmanual = value;
                    RaisePropertyChanged();
                }
            }
        }
        // Uf Manual Cabinet/Module切り替え：Cabinet
        public bool IsCabinetUfManual
        {
            get { return iscabinetufmanual; }
            set
            {
                if (iscabinetufmanual != value)
                {
                    iscabinetufmanual = value;
                    RaisePropertyChanged();
                    if (value) IsModuleUfManual = false;
                }
            }
        }

        // Uf Manual Cabinet/Module切り替え：Module
        public bool IsModuleUfManual
        {
            get { return ismoduleufmanual; }
            set
            {
                if (ismoduleufmanual != value)
                {
                    ismoduleufmanual = value;
                    RaisePropertyChanged();
                    if (value) IsCabinetUfManual = false;
                }
            }
        }

        // Uf Manual Level
        public uint SelectLevelUfManual
        {
            get { return selectlevelufmanual; }
            set
            {
                if (selectlevelufmanual != value)
                {
                    selectlevelufmanual = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Uf Manual Color
        public uint SelectColorUfManual
        {
            get { return seleccolorufmanual; }
            set
            {
                if (seleccolorufmanual != value)
                {
                    seleccolorufmanual = value;
                    RaisePropertyChanged();
                }
            }
        }

        // TAB Select Index （選択中のタブを知りたいため）
        public uint TabSelectIndex
        {
            get { return tabselectindex; }
            set
            {
                if (tabselectindex != value)
                {
                    tabselectindex = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Uf Manual RED Step Vslue(Setp 1と5を切り替え）
        public uint RedGainIndex
        {
            get { return redgainindex; }
            set
            {
                if (redgainindex != value)
                {
                    redgainindex = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Uf Manual GREEN Step Vslue(Setp 1と5を切り替え）
        public uint GreenGainIndex
        {
            get { return greengainindex; }
            set
            {
                if (greengainindex != value)
                {
                    greengainindex = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Uf Manual BULUE Step Vslue(Setp 1と5を切り替え）
        public uint BlueGainIndex
        {
            get { return bluegainindex; }
            set
            {
                if (bluegainindex != value)
                {
                    bluegainindex = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Uf Manual WHITE Step Vslue(Setp 1と5を切り替え）
        public uint WhiteGainIndex
        {
            get { return whitegainindex; }
            set
            {
                if (whitegainindex != value)
                {
                    whitegainindex = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Gap Module Cursor ON/OFF
        public bool CursorGapCell
        {
            get { return cursorgapcell; }
            set
            {
                if (cursorgapcell != value)
                {
                    cursorgapcell = value;
                    RaisePropertyChanged();
                }
            }
        }
        // Gap Module Cabinet/Module切り替え：Cabinet
        public bool IsCabinetGapCell
        {
            get { return iscabinetgapcell; }
            set
            {
                if (iscabinetgapcell != value)
                {
                    iscabinetgapcell = value;
                    RaisePropertyChanged();
                    if (value) IsModuleGapCell = false;
                }
            }
        }

        // Gap Module Cabinet/Module切り替え：Module
        public bool IsModuleGapCell
        {
            get { return ismodulegapcell; }
            set
            {
                if (ismodulegapcell != value)
                {
                    ismodulegapcell = value;
                    RaisePropertyChanged();
                    if (value) IsCabinetGapCell = false;
                }
            }
        }

        // Gap Module Level
        public uint SelectLevelGapCell
        {
            get { return selectlevelgapcell; }
            set
            {
                if (selectlevelgapcell != value)
                {
                    selectlevelgapcell = value;
                    RaisePropertyChanged();
                }
            }
        }

        // Game PadのStatus表示用
        public string GamePadStatus
        {
            get { return gamepadstatus; }
            set
            {
                if (gamepadstatus != value)
                {
                    gamepadstatus = value;
                    RaisePropertyChanged();
                }
            }
        }

        // GamePad Profileの選択
        public uint SelectGamePadProfile
        {
            get { return selectgamepadprofile; }
            set
            {
                if (selectgamepadprofile != value)
                {
                    selectgamepadprofile = value;
                    RaisePropertyChanged();
                }
            }
        }
    }
}
