#!/usr/bin/env sh
set -e

ABI="$1"
ROOT="$(pwd)"

case "$ABI" in
	arm64)
		ANDROID_ABI="arm64-v8a"
		;;
	arm)
		ANDROID_ABI="armeabi-v7a"
		;;
	x86)
		ANDROID_ABI="x86"
		;;
	x86_64)
		ANDROID_ABI="x86_64"
		;;
	*)
		echo "unknown ABI: $ABI"
		exit 1
		;;
esac

BUILD_DIR="/tmp/raylib-android-builds/$ABI"

mkdir -p "$BUILD_DIR"
cp -r raylib/src "$BUILD_DIR/src"

cd "$BUILD_DIR/src"

make TARGET_PLATFORM=PLATFORM_ANDROID \
	ANDROID_NDK="$ANDROID_HOME/ndk/27.0.11902837" \
	ANDROID_ARCH="$ABI" \
	ANDROID_API_VERSION=21 \
	RAYLIB_LIBTYPE=SHARED \
	-j"$(nproc)"

cp libraylib.6.0.0.so "$ROOT/CalculatorAndroid/Native/$ANDROID_ABI/libraylib.so"
