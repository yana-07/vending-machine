# Vending Machine Console Application

A console-based vending machine built with C# and .NET 9. The application allows users to view products, insert coins, make purchases, and receive change.

---

## Features
- Customers can:
  - View available products along with their prices and quantities
  - Insert coins
  - Make purchases
  - Receive calculated change
- Vendors can:
  - View product and coin inventory
  - Update product price
  - Update product quantity
  - Add products
  - Remove products
  - Deposit coins
  - Collect coins 
- Implements Dependency Injection and Repository patterns
- Rich console UI using [Spectre.Console](https://github.com/spectreconsole/spectre.console)

---

## Prerequisites

Before you can build and run the project, ensure the following are installed:

- [.NET 9 SDK (preview or latest)](https://dotnet.microsoft.com/download/dotnet/9.0)
- IDE such as [Visual Studio 2022 (v17.10+)](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/)
- Git

---

## Project Structure

VendingMachine
- `VendingMachine.ConsoleApp/` # Main console UI application
- `VendingMachine.Services/` # Business logic and services
- `VendingMachine.Data/` # Repositories and database context
- `VendingMachine.Common/` # Shared components: exceptions, attributes, helpers, etc.
- `VendingMachine.Tests/` # Unit tests

---

## How to Build

1. Clone the repository:
   ```bash
   git clone https://github.com/yana-07/vending-machine.git
   cd vending-machine

2. Restore dependencies and build the project:
   ```bash
   dotnet restore
   dotnet build

## How to Run
**Important:**  
> This application uses [Spectre.Console](https://spectreconsole.net/) to provide rich, interactive command-line interfaces.  
> To run the application successfully, please make sure to use an **interactive terminal** (e.g., PowerShell, Command Prompt, or a terminal in Visual Studio/VS Code).  

1. Navigate to the console app project:
   ```bash
   cd VendingMachine.ConsoleApp
   
2. Run the application:
   ```bash
   dotnet run

## How to Run Tests

   ```bash
   dotnet test
