
# BarberSchool API

This project is a .NET Core-based RESTful API designed to manage services, reservations, and reviews for a barbershop. It supports basic CRUD operations, along with pagination and soft-delete functionality.

## Table of Contents

- [Features](#features)
- [Technology Stack](#technology-stack)
- [Setup and Installation](#setup-and-installation)
- [Database Migrations](#database-migrations)
- [API Endpoints](#api-endpoints)
- [Data Models](#data-models)
- [Soft Delete Strategy](#soft-delete-strategy)
- [Validation](#validation)
- [Usage Examples](#usage-examples)
- [Error Handling](#error-handling)
- [Contributing](#contributing)
- [License](#license)

## Features

- **Service Management:** Create, update, delete, and retrieve services.
- **Reservation Management:** Create, update, delete, and retrieve reservations linked to services.
- **Review Management:** Add and manage reviews for reservations.
- **Pagination:** Supports paginated listing of services, reservations, and reviews.
- **Soft Delete:** Uses an `IsDeleted` flag instead of permanently deleting data.
- **Automatic Validation:** Uses FluentValidation for input validation.

## Technology Stack

- **Backend:** ASP.NET Core 6+ with Entity Framework Core
- **Database:** PostgreSQL with Npgsql driver
- **Data Access:** Entity Framework Core with FluentValidation
- **Packages:** 
  - `Npgsql.EntityFrameworkCore.PostgreSQL`
  - `Microsoft.EntityFrameworkCore.Tools`
  - `FluentValidation`
  - `SharpGrip.FluentValidation.AutoValidation.Endpoints`

## Setup and Installation

### Prerequisites

- .NET 6 SDK or later
- PostgreSQL installed locally or accessible on a server

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-repository-url.git
   cd BarberSchool-API
   ```

2. **Setup database connection string**
   Update your `appsettings.json` with your PostgreSQL connection string.

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

## Database Migrations

1. **Add a new migration** (if you have changes to the database schema)
   ```bash
   dotnet ef migrations add <MigrationName>
   ```

2. **Apply the migrations**
   ```bash
   dotnet ef database update
   ```

## API Endpoints

Here is a brief overview of the API endpoints:

### Services

- `GET /api/services` - Retrieve all services (excluding those marked as deleted).
- `GET /api/services/{serviceId}` - Get a single service by ID (if not marked as deleted).
- `POST /api/services` - Create a new service.
- `PUT /api/services/{serviceId}` - Update an existing service.
- `DELETE /api/services/{serviceId}` - Mark a service as deleted and return the updated resource.

### Reservations

- `GET /api/services/{serviceId}/reservations` - Get all reservations for a specific service.
- `GET /api/services/{serviceId}/reservations/{reservationId}` - Get a single reservation by ID.
- `POST /api/services/{serviceId}/reservations` - Create a new reservation for a service.
- `PUT /api/services/{serviceId}/reservations/{reservationId}` - Update a specific reservation.
- `DELETE /api/services/{serviceId}/reservations/{reservationId}` - Mark a reservation as deleted.

### Reviews

- `GET /api/services/{serviceId}/reservations/{reservationId}/reviews` - Get all reviews for a specific reservation.
- `GET /api/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}` - Get a specific review.
- `POST /api/services/{serviceId}/reservations/{reservationId}/reviews` - Create a new review.
- `PUT /api/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}` - Update a review.
- `DELETE /api/services/{serviceId}/reservations/{reservationId}/reviews/{reviewId}` - Mark a review as deleted.

## Data Models

- **Service** - Contains properties like `Id`, `Name`, `Price`, and `IsDeleted`.
- **Reservation** - Includes fields such as `Id`, `Date`, `Status`, `ServiceId`, and `IsDeleted`.
- **Review** - Holds properties including `Id`, `Content`, `Rating`, `ReservationId`, and `IsDeleted`.

## Soft Delete Strategy

Instead of permanently deleting records, the API uses a soft-delete mechanism by setting the `IsDeleted` flag to `true`. All endpoints exclude deleted items by default.

## Validation

The project uses **FluentValidation** to ensure the correctness of incoming requests. It checks for required fields, valid formats, and correct ranges.

## Usage Examples

Here are some sample JSON payloads for the main API endpoints:

- **Create Service**:
  ```json
  {
    "name": "Haircut",
    "price": 20.00
  }
  ```

- **Update Reservation**:
  ```json
  {
    "status": "Confirmed"
  }
  ```

- **Create Review**:
  ```json
  {
    "content": "Great service!",
    "rating": 5
  }
  ```

## Error Handling

Errors are returned in a standardized format using **HTTP Problem Details** format. Common error status codes include:

- `400` - Bad Request (Invalid data or missing fields)
- `404` - Not Found (When an entity does not exist or has been soft-deleted)
- `422` - Unprocessable Entity (Validation errors)

## Contributing

Feel free to open issues or submit pull requests if you have suggestions or improvements. For major changes, please open an issue first to discuss what you would like to change.

## License

This project is licensed under the MIT License. See the `LICENSE` file for more information.
