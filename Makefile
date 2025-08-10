.DEFAULT_GOAL := help

help:
	@echo "Usage:"
	@echo "  make release-linux run"
	@echo ""
	@echo "  [debug|release]-[linux|macos|windows|android]"
	@echo "  run"
	@echo "  run-android          Install and run APK on connected device"
	@echo "  clean                Remove build files"

run:
	./build/CalculatorDesktop

clean:
	rm -rf ./**/bin/ ./**/obj/ ./build/

debug-linux:
	dotnet build -o build -f net8.0 CalculatorDesktop /p:DEBUG="1"

release-linux: clean
	dotnet publish -o build -f net8.0 -c Release CalculatorDesktop

debug-macos:
	dotnet build -o build -f net8.0-macos -r osx-x64 CalculatorDesktop /p:DEBUG="1"

release-macos: clean
	dotnet publish -o build -f net8.0-macos -r osx-x64 -c Release CalculatorDesktop

debug-windows:
	dotnet build -o build -f net8.0-windows -r win-x64 CalculatorDesktop /p:DEBUG="1"

release-windows: clean
	dotnet publish -o build -f net8.0-windows -r win-x64 -c Release CalculatorDesktop

# Android

debug-android: clean
	dotnet build -o build CalculatorAndroid /p:DEBUG="1"

release-android: clean
	dotnet publish -o build -c Release CalculatorAndroid

# FIXME(LucasTA): This weird name comes from Raylib_cs generated AssemblyManifest.xml
# maybe just do my own stuff for raylib
run-android:
	adb install -r build/com.lucasta.calculator-Signed.apk
	adb shell am start -n com.lucasta.calculator/crc6480e6caa1236d905b.MainActivity
