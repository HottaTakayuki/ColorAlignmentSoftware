#カレントディレクトリをユーザーに変更
Set-Location -Path ~

#################################################
#変数定義等
$url_opencv = "https://www.nuget.org/api/v2/package/OpenCvSharp3-AnyCPU/4.0.0.20181129"
$CurrentPath       = (Split-Path $MyInvocation.MyCommand.Path) + "\"
$WorkingFolder     = "Working\"
$WorkingPath       = $CurrentPath + $WorkingFolder
$UnzipFolder       = "opencvsharp3-anycpu\"
$DLFile            = "opencvsharp3-anycpu.zip"
$DLFilePath        = $WorkingPath + $DLFile
$UnzipPath         = $WorkingPath + $UnzipFolder
$CopyFoler         = "C:\CAS_OSS\"

$LibFolder         = "lib\net40\"
$RuntimeFolder     = "runtimes\win10-x86\native\"
$OpenCVFiles = @(
       @{path = $UnzipPath + $LibFolder ; file="OpenCvSharp.dll"},
       @{path = $UnzipPath + $LibFolder ; file="OpenCvSharp.Blob.dll"},
       @{path = $UnzipPath + $RuntimeFolder ; file="OpenCvSharpExtern.dll"}
      )

#################################################
#メソッド
#エラーメッセージ表示(処理を中断する）
function ErrorFunction( $Messeage ){
    $ErrorMessage = $_.Exception.Message
    Write-Host "Error Messeage : $ErrorMessage"

    Write-Host $Messeage -ForeGroundColor Red
    read-host "Press ENTER to exit."
    exit
}

#INFOメッセージ表示
function InfoFunction( $Messeage ){
    Write-Host $Messeage -ForeGroundColor Green
}

#OpenCVファイルが存在するか確認するメソッド
function IsOpenCVFiles( $CheckFiles ){
    $FileList = Get-ChildItem $CopyFoler
    $isFile = $false
    foreach($CheckFile in $CheckFiles){
        $isFile = $false
        foreach($File in $FileList) {
            if($FIle.Name -eq $($CheckFile.file)) {#大小文字区別なし
                $isFile = $true
                break;
            }
        }
        if(!$isFile) {
            InfoFunction($($CheckFile.file) + " File does not exist.")
            break;
        }
    }
    if(!$isFile) {
        Remove-Item $CopyFoler -Recurse -Force
    }
    return $isFile
} 

#################################################
#メイン処理

#.NETのSystem.Windows.Formsをロード
Add-Type -AssemblyName System.Windows.Forms

#確認ダイアログ表示
$result = [System.Windows.Forms.MessageBox]::Show("Do you want to install the OpenCV file?","OpenCV file installation",[System.Windows.Forms.MessageBoxButtons]::OKCancel)
if($result -eq "Cancel"){
        Write-Host "Canceled execution." -ForeGroundColor Yellow
        read-host "Press ENTER to exit."
        exit
}

#ダウンロードファイルがあれば削除
if (Test-Path $DLFilePath) {
    Remove-Item $DLFilePath -Recurse -Force
    InfoFunction($DLFilePath + " Deleted")
}

#解凍フォルダがあれば削除
if (Test-Path $UnzipPath) {
    Remove-Item $UnzipPath -Recurse -Force
    InfoFunction($UnzipPath + " Deleted")
}

#すでに該当するOpenCVファイルが存在するか確認
if (Test-Path $CopyFoler) {
    if(IsOpenCVFiles($OpenCVFiles)) {
        #存在すれば正常終了
        Write-Host "It is already installed." -ForeGroundColor Yellow
        read-host "Press ENTER to exit."
        exit
    }
}

#作業フォルダがなければ作成
if (!(Test-Path $WorkingPath)) {
    try {
        New-Item $WorkingPath -ItemType Directory
        InfoFunction($WorkingPath + " Created a directory")
    }
    catch {
        #ディレクトリ作成失敗
        ErrorFunction("Error : failed to create directory " + $WorkingPath)
    }
}

#OpenCVのダウンロード
try {
    $ProtocolTypeOld = [Net.ServicePointManager]::SecurityProtocol
    if($ProtocolTypeOld -ne "Tls12") {#SecurityProtocolがTLS1.2以外の場合
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    }
    Invoke-WebRequest -Uri $url_opencv -outfile $DLFilePath
    InfoFunction($DLFilePath + " Download completed")
}
catch {
    #ダウンロード失敗（URLにアクセスできない等）
    ErrorFunction("Error : failed to download file " + $url_opencv)
}

#ダウンロードしたファイルを解凍
try {
    Add-Type -Assembly "system.io.compression.filesystem"
    [io.compression.zipfile]::ExtractToDirectory($DLFilePath, $UnzipPath)
    #Ver5.0以降でないと使用できない...。
    #Expand-Archive -Path $DLFilePath -DestinationPath $UnzipPath
    InfoFunction($DLFilePath + " Unzipped completed")
}
catch {
    #解凍失敗
    ErrorFunction("Error : failed to unzip " + $DLFilePath)
}

#配置先のディレクトリ作成
try {
    New-Item $CopyFoler -ItemType Directory
    InfoFunction($CopyFoler + " Created a directory")
}
catch {
    #ディレクトリ作成失敗
    ErrorFunction("Error : failed to create directory " + $CopyFoler)
}

#解凍したファイルを配置場所にコピーする
try {
    foreach($OpenCVFile in $OpenCVFiles){
        $Path = $($OpenCVFile.path) + $($OpenCVFile.file)
        Copy-Item -Path $Path -Destination $CopyFoler
        InfoFunction($Path + " Copied")
    }
}
catch {
    #コピー失敗
    ErrorFunction("Error : failed to copy files")
}

#作業フォルダ（ダウンロードファイル、解凍ファイル）を削除する
Remove-Item $WorkingPath -Recurse -Force

InfoFunction( "All completed!" )
read-host "Press ENTER to exit."
