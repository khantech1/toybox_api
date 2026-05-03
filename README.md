# ToyBox API — .NET 8 / MSSQL Backend

## Tech Stack
- **ASP.NET Core 8** Web API
- **Entity Framework Core 8** (Code-First, MSSQL)
- **JWT Bearer** Authentication
- **BCrypt** Password Hashing
- **Swagger UI** (served at `/` in development)

---

## Project Structure

```
ToyBoxApi/
├── Controllers/          # HTTP layer — route handlers only, no business logic
│   ├── AuthController.cs
│   ├── CategoriesController.cs
│   ├── ToysController.cs
│   ├── ExchangeRequestsController.cs
│   ├── ReviewsController.cs
│   └── ProfileController.cs
├── Services/             # Business logic
│   ├── AuthService.cs
│   ├── CategoryService.cs
│   ├── ToyService.cs
│   ├── ExchangeRequestService.cs
│   ├── ReviewService.cs
│   └── ProfileService.cs
├── Entities/             # EF Core entity classes (mirror DB diagram exactly)
│   ├── User.cs
│   ├── Category.cs
│   ├── Toy.cs
│   ├── ToyImage.cs
│   ├── Contact.cs
│   ├── SharedToy.cs
│   ├── ExchangeRequest.cs
│   ├── ExchangeRequestToy.cs
│   └── Review.cs
├── DTOs/                 # Request/Response data shapes
│   ├── Auth/
│   ├── Toys/
│   ├── Exchange/
│   ├── Reviews/
│   └── Profile/
├── Data/
│   └── AppDbContext.cs   # EF DbContext + model configuration + category seed
├── Helpers/
│   └── JwtHelper.cs      # Token generation + ClaimsPrincipal extension
├── Middleware/
│   └── ExceptionMiddleware.cs   # Global error handler → clean JSON responses
├── Migrations/
│   └── 20240101000000_InitialCreate.cs
├── Program.cs            # App bootstrap: DI, JWT, CORS, Swagger, EF migrate
└── appsettings.json      # Connection string, JWT config, file storage base URL
```

---

## Quick Start

### 1. Configure `appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ToyBoxDb;User Id=sa;Password=YOUR_PASS;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "ToyBoxApi",
    "Audience": "ToyBoxApp",
    "ExpiryInDays": 30
  },
  "FileStorage": {
    "UploadPath": "wwwroot/uploads",
    "BaseUrl": "http://YOUR_SERVER_IP:5000"
  }
}
```

### 2. Restore packages & run migrations

```bash
cd ToyBoxApi
dotnet restore
dotnet run          # auto-runs db.Database.Migrate() on startup
```

Or run migrations manually:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Open Swagger UI
Navigate to `http://localhost:5000` — full interactive API docs with JWT support.

---

## Complete API Reference

All protected routes require: `Authorization: Bearer <token>`

### Auth  `(no token required)`
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register — email, phone_no, password |
| POST | `/api/auth/login` | Login — email, password → returns token |
| PUT  | `/api/auth/profile-setup` 🔒 | Complete profile — name, address |

### Categories
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/categories` 🔒 | Get all toy categories |

### Toys
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/toys` 🔒 | Get catalog — ?search=&category_id=&age_group= |
| GET | `/api/toys/my-toys` 🔒 | Get current user's own toys |
| GET | `/api/toys/{id}` 🔒 | Get single toy detail |
| POST | `/api/toys` 🔒 | Create toy listing |
| PUT | `/api/toys/{id}` 🔒 | Update toy (owner only) |
| DELETE | `/api/toys/{id}` 🔒 | Delete toy (owner only) |
| POST | `/api/toys/{id}/images` 🔒 | Upload image (multipart, field: `image`) |

### Exchange Requests
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/exchange-requests` 🔒 | Get all — ?status=pending\|accepted\|completed |
| GET | `/api/exchange-requests/{id}` 🔒 | Get single request detail |
| POST | `/api/exchange-requests` 🔒 | Create request — requested_toy_id, offered_toy_id, message |
| PUT | `/api/exchange-requests/{id}/accept` 🔒 | Accept (toy owner only) |
| PUT | `/api/exchange-requests/{id}/decline` 🔒 | Decline |

### Reviews
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/reviews` 🔒 | Submit review — request_id, reviewee_user_id, rating_score (1–10) |
| GET | `/api/reviews/user/{userId}` 🔒 | Get reviews for a user |

### Profile
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/profile` 🔒 | Get own profile |
| GET | `/api/profile/{userId}` 🔒 | Get any user's profile |
| PUT | `/api/profile` 🔒 | Update name, address |
| POST | `/api/profile/photo` 🔒 | Upload profile picture (multipart, field: `profile_pic`) |

### Contacts
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/contacts` 🔒 | Get contacts list |
| POST | `/api/contacts` 🔒 | Add contact — `{ "contact_id": 5 }` |
| DELETE | `/api/contacts/{contactId}` 🔒 | Remove contact |

---

## JSON Format

All responses use **snake_case** keys (configured via `JsonNamingPolicy.SnakeCaseLower`) to match the Flutter app models exactly.

**Success example — login:**
```json
{
  "token": "eyJ...",
  "user": {
    "user_id": 1,
    "name": "Alex Johnson",
    "email": "alex@example.com",
    "phone_no": "555-0001",
    "address": "Seattle, WA",
    "profile_pic": "http://server/uploads/profiles/uuid.jpg",
    "rating": 9.5
  }
}
```

**Error example:**
```json
{
  "message": "Invalid email or password.",
  "status_code": 401
}
```

---

## File Uploads

Uploaded images are stored in `wwwroot/uploads/{folder}/` and served as static files.
- Toy images: `POST /api/toys/{id}/images` — field name `image`
- Profile photo: `POST /api/profile/photo` — field name `profile_pic`
- Max file size: **5 MB**
- Allowed types: `.jpg`, `.jpeg`, `.png`, `.webp`
