# Calculator

Crossplatform calculator
![Calculator](https://github.com/user-attachments/assets/ee04ecc2-901e-450a-bf72-b695008c6995)

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
