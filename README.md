# 🛒 E-commerce API

A robust and scalable backend API for an e-commerce platform, built with **ASP.NET Core 10**, **Clean Architecture**, and **Domain-Driven Design (DDD)** principles. This project focuses on data integrity, secure transactions, and maintainable code structure.

## ✨ Key Features

- **Shopping Cart Management:** Add, update quantity, remove, and clear cart items seamlessly.
- **Real-time Stock Reservation:** Implements `ReserveStock` and `ReleaseStock` logic to prevent overselling and ensure accurate inventory tracking.
- **Order Checkout:** Secure checkout flow with ACID transactions and data snapshotting (recording price/product name at the time of purchase).
- **API Versioning:** Structured URL versioning strategy (`/api/v1/...`) for future scalability.
- **Robust Error Handling:** Utilizes the Result Pattern to provide consistent, predictable API responses without relying on exceptions for control flow.

## 🛠️ Tech Stack

- **Framework:** .NET 10, ASP.NET Core Web API
- **Database & ORM:** SQL Server, Entity Framework Core (Fluent API, SaveChanges Interceptors)
- **Architecture:** Clean Architecture, Domain-Driven Design (DDD), Repository Pattern, Unit of Work
- **Testing:** xUnit, Moq, FluentAssertions
- **Tools:** Docker, Git, Postman, Swagger/OpenAPI

## 🏗️ Architecture Highlights

This project strictly follows **Clean Architecture**, separating concerns into four distinct layers:
1. **Domain:** Contains rich domain entities (e.g., `Cart`, `ProductInventory`) that encapsulate core business logic and rules.
2. **Application:** Orchestrates use cases (e.g., `CartService`, `OrderService`) and defines interfaces.
3. **Infrastructure:** Handles external concerns like Database access (EF Core) and external services.
4. **API:** The entry point, handling HTTP requests, routing, and middleware.

## 🧪 Testing Strategy

Unit tests are implemented with a focus on **Core Business Logic** rather than simple CRUD operations.
- **`CartService` & `OrderService`** are thoroughly tested to ensure stock reservation, release, and transaction workflows behave correctly under various scenarios (e.g., insufficient stock, empty cart, quantity updates).
- This risk-based testing approach ensures the most critical and complex parts of the system are reliable and regression-free.


## 📂 Project Structure

```text
📁 EcommerceAPI
├── 📁 EcommerceAPI.Api                  # Presentation Layer (API Endpoints)
│   ├── 📁 Controllers                   # Cart, Order, Product, Auth Controllers
│   ├── 📁 Middleware                    # Global Exception & Error Handling
│   ├── 📁 Services                      # HTTP Context Current User Service
│   ├── Program.cs                       # App Entry Point, DI & Pipeline Config
│   └── appsettings.json                 # Configuration & DB Connection Strings
│
├── 📁 EcommerceAPI.Application          # Application Layer (Use Cases & Interfaces)
│   ├── 📁 DTOs                          # Request/Response Data Transfer Objects
│   ├── 📁 Interfaces                    # Repository & Service Contracts
│   ├── 📁 Services                      # Business Logic Orchestration (CartService, OrderService)
│   └── DependencyInjection.cs           # Application Service Registrations
│
├── 📁 EcommerceAPI.Domain               # Domain Layer (Core Business Entities)
│   ├── 📁 Common                        # BaseEntity, Result Pattern, ValueObject (Money)
│   ├── 📁 Entities                      # Rich Domain Models (Cart, Order, Product, Inventory)
│   └── 📁 Enums                         # Domain Enums (OrderStatus, ErrorCode)
│
├── 📁 EcommerceAPI.Infrastructure       # Infrastructure Layer (External Concerns)
│   ├── 📁 Persistence
│   │   ├── 📁 Configurations            # EF Core Fluent API Entity Configurations
│   │   ├── 📁 Interceptors              # SaveChanges Interceptors (Soft Delete, Auditing)
│   │   ├── 📁 Migrations                # Database Schema Migrations
│   │   ├── 📁 Repositories              # Concrete Repository Implementations
│   │   └── EcommerceDbContext.cs        # EF Core DbContext
│   └── DependencyInjection.cs           # Infrastructure & DB Service Registrations
│
└── 📁 EcommerceAPI.Tests                #  Unit Testing
    └── 📁 Services                      # Application Service Tests (Cart, Order)
