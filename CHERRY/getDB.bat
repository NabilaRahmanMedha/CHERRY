@echo off
echo Pulling cherry.db3 from emulator...
adb exec-out run-as com.companyname.cherry cat /data/user/0/com.companyname.cherry/files/cherry.db3 > cherry.db3
echo Done. File saved as cherry.db3
pause
