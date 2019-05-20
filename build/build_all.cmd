@echo off

cd "F:\Studia\II_stopien\II_semestr\mpw\fileBox\serwer"
dotnet publish -c Debug -r win10-x64

copy /y "F:\Studia\II_stopien\II_semestr\mpw\fileBox\serwer\bin\Debug\netcoreapp2.2\win10-x64\Common.dll" "F:\Studia\II_stopien\II_semestr\mpw\fileBox\serwer\bin\Debug\netcoreapp2.2\Common.dll"
copy /y "F:\Studia\II_stopien\II_semestr\mpw\fileBox\serwer\bin\Debug\netcoreapp2.2\win10-x64\Serwer.dll" "F:\Studia\II_stopien\II_semestr\mpw\fileBox\serwer\bin\Debug\netcoreapp2.2\Serwer.dll"

cd "F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient"
dotnet publish -c Debug -r win10-x64

copy /y "F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win10-x64\Common.dll" "F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\Common.dll"
copy /y "F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\win10-x64\Klient.dll" "F:\Studia\II_stopien\II_semestr\mpw\fileBox\klient\bin\Debug\netcoreapp2.2\Klient.dll"

cd "F:\Studia\II_stopien\II_semestr\mpw\fileBox\build"