@echo off
setlocal
echo ===============================================================================
echo   JOSAVE TUNER - Build and Test Suite
echo ===============================================================================

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo [ERROR] C# Compiler not found at %CSC%
    exit /b 1
)

echo [1/3] Compiling JOSAVE TUNER GUI (JosaveTuner.exe)...
"%CSC%" /nologo /target:winexe /win32icon:app.ico /out:JosaveTuner.exe /r:FTD2XX_NET.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll HondaFlashCore.cs EcmDatabase.cs EcuMapDefinition.cs TuningControls.cs HondaFlashForm.cs
if %ERRORLEVEL% neq 0 (
    echo [ERROR] GUI Compilation failed!
    exit /b %ERRORLEVEL%
)

echo [2/3] Compiling JOSAVE TUNER CLI (HondaFlashCLI.exe)...
"%CSC%" /nologo /target:exe /win32icon:app.ico /out:HondaFlashCLI.exe /r:FTD2XX_NET.dll /r:System.Drawing.dll HondaFlashCore.cs EcmDatabase.cs EcuMapDefinition.cs HondaFlashCLI.cs
if %ERRORLEVEL% neq 0 (
    echo [ERROR] CLI Compilation failed!
    exit /b %ERRORLEVEL%
)

echo [3/3] Compiling and Running Test Suite (TestRunner.exe)...
"%CSC%" /nologo /target:exe /out:TestRunner.exe /r:FTD2XX_NET.dll /r:System.Drawing.dll HondaFlashCore.cs EcmDatabase.cs EcuMapDefinition.cs TestRunner.cs
if %ERRORLEVEL% neq 0 (
    echo [ERROR] TestRunner Compilation failed!
    exit /b %ERRORLEVEL%
)

echo Running 21 Automated Tests...
TestRunner.exe
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Tests failed!
    exit /b %ERRORLEVEL%
)

echo ===============================================================================
echo   BUILD COMPLETED SUCCESSFULLY! All Binaries and Tests Ready.
echo ===============================================================================
