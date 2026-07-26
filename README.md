# fb2cng_GUI

A graphical user interface (GUI) wrapper for the [fb2cng (fbc)](https://github.com/rupor-github/fb2cng) console converter. This application allows users to easily configure conversion settings for FB2 e-books and trigger the conversion process directly via a convenient drop-down menu.

> [!NOTE]
> **Project Background & Disclaimer:**  
> This project was created by a beginner/non-programmer for learning and code-understanding purposes, using **Gemini** as a development assistant. Because of this, the source code contains an abundance of descriptive comments written in **Ukrainian** (apologies in advance for any inconvenience!).

## 🚀 Features
* **User-Friendly Interface**: No more command-line typing; manage everything via a clean GUI.
* **Flexible Configuration**: Easily adjust all conversion settings before processing.
* **Quick Action**: Run the conversion tool smoothly from the drop-down menu interface.
<details>
<summary><b>Detailed Description </b></summary>
 
### 📜 Overview
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

## 📦 Installation & Quick Start

### Option 1: Download Ready-to-Run (Recommended)
1. Go to the [Releases](../../releases) page of this repository.
2. Download the standalone executable.
3. Place it in your toolset folder along with the original fb2cng console utility, and run it.

### Option 2: Build from Source
1. **Clone the repository:**
   ```bash
   git clone https://github.com/Jurchos/fb2cng_GUI.git
   ```
2. **Open & Build:**
   Open the solution file in **Visual Studio 2026 / VS Code** (with .NET 10 SDK installed) and build the project in `Release` mode.

---

## 🛠️ Built With

* **C# 14** 
* **.NET 10 (Modern .NET Runtime)**
* **Windows Forms (WinForms)**

---

## 📜 License

This project is licensed under the [MIT License](LICENSE) — feel free to use, modify, and distribute it in your own workflows.