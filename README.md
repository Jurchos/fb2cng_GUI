# fb2cng_GUI

A graphical user interface (GUI) wrapper for the [fb2cng (fbc)](https://github.com/rupor-github/fb2cng) console converter. This application allows users to easily configure conversion settings for FB2 e-books and trigger the conversion process directly via a convenient drop-down menu.

> [!NOTE]
> **Project Background & Disclaimer:**  
> This project was created by a beginner/non-programmer for learning and code-understanding purposes, using **Gemini** as a development assistant. Because of this, the source code contains an abundance of descriptive comments written in **Ukrainian** (apologies in advance for any inconvenience!).

## Features
* **User-Friendly Interface**: No more command-line typing; manage everything via a clean GUI.
* **Flexible Configuration**: Easily adjust all conversion settings before processing.
* **Quick Action**: Run the conversion tool smoothly from the drop-down menu interface.
<details>
<summary><b>Detailed Description </b></summary>
 
### Overview
The primary purpose of this application is to allow users to right-click an `.fb2` file and convert the book directly via a context menu option.

### Key Features
* **Sleek and intuitive UI** with automatic display scaling.
* **Theme options** (Light and Dark modes).
* **Multi-language support** (EN, UK, RU).
* **Customizable output formats** for conversion.
* **Selectable output directory** for converted files.
* **Custom configuration file** support.
* **Customizable context menu entry name**.
* **One-click toggle** to add or remove the conversion option from the context menu.
* **Batch folder conversion** (including nested subfolders).
* **Post-conversion file management** (permanent deletion with confirmation, or auto-move to Recycle Bin).
* **Progress indicator** (essential for heavy formats; can be minimized or fully disabled for lighter jobs).
* **Easy toggle switches** (e.g., checkbox for overwriting previously converted files).
* **Error handling** with failure notifications.
* **Archive support**: starting from version 0.5, it converts not only standalone `.fb2` files, but also `.fb2.zip` and `.fb2` files stored within standard ZIP archives.
</details>

## System Requirements
* Windows OS
* .NET Framework / .NET Runtime (depending on your project version)
* The original `fb2cng` console tool

## Installation & Usage
1. Clone or download this repository.
2. Open the solution file `fb2cng_GUI.slnx` in Visual Studio and build the project.
3. Make sure the executable has access to the core `fb2cng` tool path.
4. Run the application, configure your settings, and convert your FB2 files.
