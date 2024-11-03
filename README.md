# Calculator

Crossplatform calculator

https://github.com/user-attachments/assets/1c413f65-0729-42fa-8684-5183473134a3

## Installation

[<img src="https://github.com/machiav3lli/oandbackupx/blob/034b226cea5c1b30eb4f6a6f313e4dadcbb0ece4/badge_github.png"
    alt="Get it on GitHub"
    height="80">](https://github.com/lucastavaresa/calculator/releases)

## Build

```sh
git clone https://github.com/LucasTavaresA/Calculator.git
cd Calculator
./build.sh
```

### Build with docker

This will build calculator, get you the executable and delete everything

Install docker then run

```sh
docker build -t calc .
docker run --name calc-container calc
docker cp calc-container:/Calculator/build/CalculatorDesktop Calculator
docker rm -f calc-container
docker rmi -f calc
```

## Credits

Made with [raylib](https://www.raylib.com/), used [Raylib-cs](https://github.com/ChrisDill/Raylib-cs) bindings

Font: [Iosevka](https://github.com/be5invis/Iosevka)

Icons: [Google Material Icons](https://fonts.google.com/icons)
