# GH-Toolkit-NET

A library aimed at making it easier to work with the files from Neversoft's Guitar Hero games.

This is a refactor of the original GH-Toolkit written in Python, which can be found [here](https://github.com/AddyMills/Addys-Guitar-Hero-Toolkit). For now, the original GH-Toolkit is still the main version, and this is a work in progress until all the features of the original are implemented here.

Having said that, this version is written in C# and is much faster than the original, and will be the main version once it's finished.

There are already some features present in this library that are not in the original, such as the ability to read and write PS2 archive files for custom disc creation.

## Usage

This repo is not intended to be "used" as an executable, it is instead intended to be a library for other programs to use. If you want to use this library in your own program, I recommend building it yourself and adding a reference to the DLL in your project. Once the library is in a more complete state, I will start releasing compiled versions of the DLL.

## Building

This library targets the NET 8.0 framework, and was built using Visual Studio 2022.

Python 3.9 is also required to build parts of the library, as some of the compressed files are compressed using scripts written in Python.

This currently affects the following files:
* QBDebug/debug.txt

To build the .dbg file the program expects, simply drag the debug.txt file onto the CompressDebug.py script in the QBDebug folder.

## Contributing

If you want to contribute to this project, feel free to fork the repo and submit a pull request. If you have any questions, feel free to open an issue.

## License

This project is licensed under the GPL v3 license. See the `LICENSE` file for more information.