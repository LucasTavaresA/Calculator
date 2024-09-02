#!/usr/bin/env sh
# shellcheck disable=SC2086
set -e

RUN=0
RELEASE=0
WINDOWS=0
LINUX=0
ANDROID=0
MACOS=0
PROGRAM="Calculator"
BUILD_FLAGS="-o build"
CONSTANTS=""
PWD="$(pwd)"

print_help() {
  printf \
    "%s [windows|linux|android|macos] [-r -R]

-r           run after building
-R           enable release mode
-h --help    show help\n" "$0"
}

main() {
  case "$1" in
  "android")
    ANDROID=1
    CONSTANTS="ANDROID"
    ;;
  "linux")
    LINUX=1
    CONSTANTS="LINUX"
    BUILD_FLAGS="$BUILD_FLAGS -f net8.0"
    ;;
  "windows")
    WINDOWS=1
    CONSTANTS="WINDOWS"
    BUILD_FLAGS="$BUILD_FLAGS -f net8.0-windows -r win-x64"
    ;;
  "macos")
    MACOS=1
    CONSTANTS="MACOS"
    BUILD_FLAGS="$BUILD_FLAGS -r osx-x64"
    ;;
  *)
    echo "'$1' is not a valid platform!"
    echo "You need to specify a platform to build!"
    print_help
    exit 1
    ;;
  esac

  shift

  while [ "$#" -gt 0 ]; do
    case "$1" in
    "-r")
      RUN=1
      ;;
    "-R")
      RELEASE=1
      BUILD_FLAGS="$BUILD_FLAGS -c Release"
      ;;
    "--help")
      print_help
      exit 0
      ;;
    "-h")
      print_help
      exit 0
      ;;
    *)
      echo "'$1' is not a valid argument!"
      print_help
      exit 1
      ;;
    esac

    shift
  done

  if [ "$RELEASE" = 0 ]; then
    CONSTANTS="$CONSTANTS DEBUG"
  fi

  if [ "$LINUX" = 1 ] || [ "$WINDOWS" = 1 ] || [ "$MACOS" = 1 ]; then
    dotnet publish $BUILD_FLAGS "${PROGRAM}Desktop" /p:DefineConstants="\"$CONSTANTS\""

    if [ "$RUN" = 1 ]; then
      ./build/${PROGRAM}Desktop
    fi
  elif [ "$ANDROID" = 1 ]; then
    rm -rf ./**/bin/ ./**/obj/ ./build/

    dotnet publish $BUILD_FLAGS "${PROGRAM}Android" /p:DefineConstants="\"$CONSTANTS\""

    if [ "$RUN" = 1 ]; then
			# FIXME(LucasTA): This weird name comes from Raylib_cs generated AssemblyManifest.xml
			# maybe just do my own stuff for raylib
			adb install -r build/com.lucasta.calculator-Signed.apk
			adb shell am start -n com.lucasta.calculator/crc6480e6caa1236d905b.MainActivity
    fi
  fi
}

if [ -d ./CalculatorUI/ ] && [ -d ./.git/ ]; then
  main "$@"
else
  echo "You are in ${PWD}"
  echo "You need to be in the repo directory to build!"
fi

./after.sh
