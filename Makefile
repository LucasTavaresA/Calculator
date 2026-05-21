.DEFAULT_GOAL := help
CONTAINER ?= docker
BUILD_DIR ?= ./build/
NPROC ?= $$(nproc)

help:
	@echo "Usage:"
	@echo "  make release-linux run"
	@echo ""
	@echo "  [debug|release]-[linux|macos|windows|android]"
	@echo "  run"
	@echo "  run-android          Install and run APK on connected device"
	@echo "  clean                Remove build files"

run:
	$(BUILD_DIR)CalculatorDesktop

run-wine:
	wine $(BUILD_DIR)CalculatorDesktop.exe

clean:
	rm -rf ./**/bin/ ./**/obj/ $(BUILD_DIR)

# Linux
raylib-linux:
	make -C raylib/src clean
	make -C raylib/src PLATFORM=PLATFORM_DESKTOP RAYLIB_LIBTYPE=SHARED -j$(NPROC)
	cp ./raylib/src/libraylib.so.6.0.0 ./Raylib-cs/native/linux-x64/libraylib.so

debug-linux:
	dotnet build -o $(BUILD_DIR) -f net10.0 CalculatorDesktop /p:DEBUG="1"

release-linux: clean
	dotnet publish -o $(BUILD_DIR) -f net10.0 -c Release CalculatorDesktop

docker-linux: clean
	$(CONTAINER) build -t calc .
	$(CONTAINER) run --name calc-container calc
	$(CONTAINER) cp calc-container:/Calculator/build/CalculatorDesktop Calculator
	$(CONTAINER) rm -f calc-container
	$(CONTAINER) rmi -f calc

# macos
debug-macos:
	dotnet build -o $(BUILD_DIR) -f net10.0-macos -r osx-x64 CalculatorDesktop /p:DEBUG="1"

release-macos: clean
	dotnet publish -o $(BUILD_DIR) -f net10.0-macos -r osx-x64 -c Release CalculatorDesktop

# Windows
raylib-windows-mingw:
	make -C raylib/src clean
	cd raylib/src && \
	x86_64-w64-mingw32-windres raylib.dll.rc -O coff -o raylib.dll.rc.data
	make -C raylib/src PLATFORM=PLATFORM_DESKTOP RAYLIB_LIBTYPE=SHARED \
		OS=Windows_NT CC=x86_64-w64-mingw32-gcc AR=x86_64-w64-mingw32-ar \
		LDFLAGS="-static -static-libgcc -static-libstdc++" -j$(NPROC)
	cp ./raylib/src/raylib.dll ./Raylib-cs/native/win-x64/raylib.dll

debug-windows:
	dotnet build -o $(BUILD_DIR) -f net10.0-windows -r win-x64 CalculatorDesktop /p:DEBUG="1"

release-windows: clean
	dotnet restore CalculatorDesktop/CalculatorDesktop.csproj -r win-x64 -p:TargetFramework=net10.0-windows
	dotnet publish -o $(BUILD_DIR) -f net10.0-windows -r win-x64 -c Release --no-restore CalculatorDesktop

# Android
raylib-android:
	rm -rf /tmp/raylib-android-builds
	mkdir -p /tmp/raylib-android-builds
	patch -N ./raylib/src/platforms/rcore_android.c <android.patch || true
	make -C raylib/src clean
	./build-raylib.sh arm64 > /dev/null & \
	./build-raylib.sh arm > /dev/null & \
	./build-raylib.sh x86 > /dev/null & \
	./build-raylib.sh x86_64 > /dev/null & \
	wait
	patch -R ./raylib/src/platforms/rcore_android.c <android.patch

debug-android: clean
	dotnet build -o $(BUILD_DIR) CalculatorAndroid /p:DEBUG="1"

release-android: clean
	dotnet publish -o $(BUILD_DIR) -c Release CalculatorAndroid

run-android:
	adb install -r $(BUILD_DIR)com.lucasta.calculator-Signed.apk
	adb shell am start -n com.lucasta.calculator/com.lucasta.calculator.MainActivity
