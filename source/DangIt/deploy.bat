﻿
set H=R:\KSP_1.2.2_dev
echo %H%

copy /Y "bin\Debug\DangIt.dll" "..\..\GameData\DangIt\Plugins"
copy /Y ..\DangIt.version ..\..\GameData\DangIt

cd ..\..\GameData
mkdir "%H%\GameData\DangIt"
xcopy /y /s DangIt "%H%\GameData\DangIt"
