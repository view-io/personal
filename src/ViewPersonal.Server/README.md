# ViewPersonal.Server API

## Version Management API

### Swagger Documentation

The API includes Swagger documentation that is automatically opened in the browser when running in development mode. You can access it at:

- HTTP: http://localhost:5210

Swagger provides interactive documentation that allows you to:
- View all available endpoints
- See request and response models
- Test API calls directly from the browser

This API provides endpoints for managing application versions with OS-specific download links. It follows the repository pattern and uses Entity Framework Core for data access.

### Setup

1. The application uses SQL Server LocalDB by default. Make sure you have it installed.
2. The database will be automatically created and migrated on first run.

### API Endpoints

#### GET /api/versions

Returns all versions ordered by release date (newest first).

**Response:**
```json
[
  {
    "id": 1,
    "versionNumber": "1.0.0",
    "releaseDate": "2023-05-01T00:00:00",
    "description": "Initial release",
    "isLatest": true,
    "osDetails": [
      {
        "id": 1,
        "operatingSystem": "Windows",
        "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-windows.exe"
      },
      {
        "id": 2,
        "operatingSystem": "Mac Intel",
        "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-intel.dmg"
      },
      {
        "id": 3,
        "operatingSystem": "Mac Apple Silicon",
        "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-arm64.dmg"
      }
    ]
  }
]
```

#### GET /api/versions/latest

Returns the latest version.

**Response:**
```json
{
  "id": 1,
  "versionNumber": "1.0.0",
  "releaseDate": "2023-05-01T00:00:00",
  "description": "Initial release",
  "isLatest": true,
  "osDetails": [
    {
      "id": 1,
      "operatingSystem": "Windows",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-windows.exe"
    },
    {
      "id": 2,
      "operatingSystem": "Mac Intel",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-intel.dmg"
    },
    {
      "id": 3,
      "operatingSystem": "Mac Apple Silicon",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-arm64.dmg"
    }
  ]
}
```

#### GET /api/versions/{id}

Returns a specific version by ID.

**Response:**
```json
{
  "id": 1,
  "versionNumber": "1.0.0",
  "releaseDate": "2023-05-01T00:00:00",
  "description": "Initial release",
  "isLatest": true,
  "osDetails": [
    {
      "id": 1,
      "operatingSystem": "Windows",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-windows.exe"
    },
    {
      "id": 2,
      "operatingSystem": "Mac Intel",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-intel.dmg"
    },
    {
      "id": 3,
      "operatingSystem": "Mac Apple Silicon",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.0-mac-arm64.dmg"
    }
  ]
}
```

#### POST /api/versions

Creates a new version.

**Request:**
```json
{
  "versionNumber": "1.1.0",
  "releaseDate": "2023-06-15T00:00:00",
  "description": "Bug fixes and performance improvements",
  "isLatest": true,
  "osDetails": [
    {
      "os": "Windows",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-windows.exe"
    },
    {
      "os": "Mac Intel",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-mac-intel.dmg"
    },
    {
      "os": "Mac Apple Silicon",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-mac-arm64.dmg"
    }
  ]
}
```

**Response:**
```json
{
  "id": 2,
  "versionNumber": "1.1.0",
  "releaseDate": "2023-06-15T00:00:00",
  "description": "Bug fixes and performance improvements",
  "isLatest": true,
  "osDetails": [
    {
      "id": 4,
      "operatingSystem": "Windows",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-windows.exe"
    },
    {
      "id": 5,
      "operatingSystem": "Mac Intel",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-mac-intel.dmg"
    },
    {
      "id": 6,
      "operatingSystem": "Mac Apple Silicon",
      "downloadUrl": "https://example.com/download/viewpersonal-1.1.0-mac-arm64.dmg"
    }
  ]
}
```

#### PUT /api/versions/{id}

Updates an existing version.

**Request:**
```json
{
  "versionNumber": "1.0.1",
  "releaseDate": "2023-05-20T00:00:00",
  "description": "Hotfix for critical issues",
  "isLatest": false,
  "osDetails": [
    {
      "os": "Windows",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.1-windows.exe"
    },
    {
      "os": "Mac Intel",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.1-mac-intel.dmg"
    },
    {
      "os": "Mac Apple Silicon",
      "downloadUrl": "https://example.com/download/viewpersonal-1.0.1-mac-arm64.dmg"
    }
  ]
}
```

**Response:** 204 No Content

#### DELETE /api/versions/{id}

Deletes a version.

**Response:** 204 No Content