@ECHO OFF
mode con:cols=220 lines=2500
for %%f in ("adb.exe") do set p=%%~$PATH:f
if not exist "%p%" set p=C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
if not exist "%p%" set p=C:\utils\editors\android_sdk\platform-tools\adb.exe
if not exist "%p%" set p=C:\Users\Igor\AppData\Local\Android\android-sdk\platform-tools\adb.exe
if exist "%p%" (
"%p%" -d logcat -s Unity
) else (
echo undefined adb.exe path
pause
)
