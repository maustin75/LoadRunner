@ECHO OFF
cd %~dp0

SET minorVersion="7.31"
SET majorVersion="0"

SET projectID=Viki.LoadRunner.Engine
SET project1="..\\..\src\\%projectID%\\%projectID%.csproj"

"C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe" "%project1%" /verbosity:m /target:Rebuild /tv:14.0 /p:GenerateBuildInfoConfigFile=false /p:VisualStudioVersion=14.0 /p:platform=AnyCPU /p:TargetFrameworkVersion="v4.5" /p:Configuration=Release /p:OutputPath="%cd%\pack\lib\net45" /p:DebugSymbols=false /p:DebugType=none /P:SignAssembly=False /p:DocumentationFile=%projectID%.xml

%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe EditNuspec.msbuild /p:File="%projectID%.nuspec" /p:projectID="%projectID%" /p:majorVersion=%majorVersion% /p:minorVersion=%minorVersion%

echo Try to clean up
del %projectID%.nupkg
echo Clean up finished

echo Try to remove necessary dll's


echo Necessary dll's removed

echo Creating package
..\NuGet.exe pack .\pack\%projectID%.nuspec

ren %projectID%.%majorVersion%.%minorVersion%.nupkg %projectID%.nupkg
echo Packet renamed to %projectID%.nupkg

echo Try to remove 'pack' folder
rd pack /S /Q
echo 'pack' folder removed

pause