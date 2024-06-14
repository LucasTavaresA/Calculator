#!/usr/bin/env sh
# shellcheck disable=SC2086
set -e

RUN=0
RELEASE=0
WINDOWS=0
LINUX=0
ANDROID=0
PROGRAM="Calculator"
BUILD_FLAGS="-o build"
BUILD_SWITCHES=""
PWD="$(pwd)"

print_help() {
  printf \
    "%s [windows|linux|android] [-r -R]

-r           run after building
-R           enable release mode
-h --help    show help\n" "$0"
}

main() {
  case "$1" in
  "android")
    ANDROID=1
    BUILD_SWITCHES="$BUILD_SWITCHES /p:DefineConstants=ANDROID"
    ;;
  "linux")
    LINUX=1
    BUILD_SWITCHES="$BUILD_SWITCHES /p:DefineConstants=LINUX"
    ;;
  "windows")
    WINDOWS=1
    BUILD_SWITCHES="$BUILD_SWITCHES /p:DefineConstants=WINDOWS"
    BUILD_FLAGS="$BUILD_FLAGS -r win-x64"
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
    BUILD_SWITCHES="$BUILD_SWITCHES /p:DefineConstants=DEBUG"
  fi

  if [ "$LINUX" = 1 ] || [ "$WINDOWS" = 1 ]; then
    dotnet publish $BUILD_FLAGS "${PROGRAM}Desktop" $BUILD_SWITCHES

    if [ "$RUN" = 1 ]; then
      ./build/${PROGRAM}Desktop
    fi
  elif [ "$ANDROID" = 1 ]; then
    if [ "$RELEASE" = 1 ]; then
      rm -rf ./**/bin/ ./**/obj/ ./build/
    fi

    dotnet publish $BUILD_FLAGS "${PROGRAM}Android" $BUILD_SWITCHES

    if [ "$RUN" = 1 ]; then
      echo "Can't run after building on android!"
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
