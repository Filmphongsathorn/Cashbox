# 📊 Cashbox Analyzer 

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![WPF](https://img.shields.io/badge/WPF-UI-0078D4?style=for-the-badge&logo=windows)
![C#](https://img.shields.io/badge/C%23-Language-239120?style=for-the-badge&logo=c-sharp)
![License](https://img.shields.io/badge/License-MIT-green.svg)

**Cashbox Analyzer** is a robust, production-ready desktop application meticulously engineered to streamline financial management for small businesses, food stalls, and independent vendors. 

By migrating from traditional, error-prone paper bookkeeping to a secure digital environment, this software empowers business owners to maintain pristine financial records while extracting actionable insights through real-time data visualization.

---

## 🎯 Executive Summary & Problem Statement

Historically, small business owners have relied on manual paper ledgers to track daily income and expenses. This archaic approach introduces several critical pain points:
1. **Data Vulnerability:** Paper ledgers are easily damaged, lost, or degraded over time.
2. **Operational Inefficiency:** Manually calculating monthly profits, reconciling daily margins, and tracking cash flow is incredibly time-consuming and highly susceptible to human error.
3. **Lack of Strategic Insights:** Without immediate data visualization, owners cannot easily identify overhead leaks, peak sales trends, or true profit margins.

**Cashbox Analyzer** was developed to bridge this gap. By combining senior-level accounting principles with high-performance software architecture, it delivers an intuitive, fast, and frictionless tool that transforms raw daily transactions into strategic financial intelligence.

---

## ✨ Key Features

- 💰 **Rapid Data Entry System:** A highly optimized, large-button UI/UX designed specifically for fast-paced retail and restaurant environments where time is of the essence.
- 📈 **Advanced Analytics Dashboard:**
  - **Monthly Profit & Loss:** Instantly visualize net profit trends month-over-month.
  - **Daily Cash Flow:** Compare daily revenue against expenditures to identify peak performance days.
  - **Expense Breakdown:** Identify exactly where your capital is going with dynamic cost-proportion charts.
- ⚙️ **Dynamic Categorization:** Fully customizable revenue and expense categories tailored to your specific business model (e.g., Raw Materials, Utilities, Marketing).
- 💾 **Secure Local Storage & Backup:** 100% offline functionality ensures your financial data remains entirely on your local machine, completely private. Features a one-click Backup and Restore mechanism to safeguard against hardware failure.

---

## 💻 Tech Stack & Architecture

Built with modern, enterprise-grade technologies to ensure maximum stability and performance:
- **Framework:** .NET 10.0 (Windows Desktop)
- **UI Architecture:** Windows Presentation Foundation (WPF) featuring a modern, squircle-based minimalist design language.
- **Data Visualization:** LiveChartsCore (SkiaSharp) for hardware-accelerated, fluid chart rendering.
- **Design Pattern:** Strict MVVM (Model-View-ViewModel) architecture ensuring separation of concerns, testability, and long-term maintainability.
- **Persistence:** High-speed Local JSON/SQLite storage for zero-latency, offline-first data operations.

---

## 🚀 Installation Guide (For End Users)

You do **not** need to install any frameworks, Docker, or development tools to use this software. 

1. Go to the [Releases](../../releases) page of this repository.
2. Download the latest **`CashboxAnalyzer_Setup.exe`**.
3. Run the installer and follow the standard on-screen instructions.
4. Launch **Cashbox Analyzer** from your Desktop or Start Menu and begin managing your finances immediately!

---

## 🛠️ Build from Source (For Developers)

If you wish to contribute or build the software from source:

### Prerequisites
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2022 or JetBrains Rider

### Build Instructions
```bash
# Clone the repository
git clone https://github.com/Filmphongsathorn/Cashbox.git

# Navigate into the project directory
cd Cashbox

# Build the project
dotnet build -c Release

# Run the application
dotnet run -c Release
```

---

> *"Because numbers never lie. A meticulous accounting system is the foundational cornerstone of sustainable wealth."*
> — Senior Software Engineer & Financial Analyst
