# Webjet Movie Solution

A full-stack movie price comparison app built with .NET 8 (backend) and React 19 (frontend). Users can browse movies, view details, and compare prices from two providers (Cinemaworld and Filmworld). The solution is designed for resilience, performance, and a great user experience, even if external APIs are flaky.

---

## Solution & Design Approach

- **Backend**: .NET 8 Web API using Vertical Slice architecture (Carter + MediatR). Aggregates data from both providers in parallel, with in-memory caching and advanced resilience (Polly: retry, timeout, circuit breaker, fallback). Exposes two main endpoints: paginated movie list and movie details with price comparison.
- **Frontend**: React 19 + TypeScript, styled with Tailwind CSS. Responsive UI with loading skeletons, error handling, and price comparison features.
- **Testing**: xUnit, Moq, and FluentAssertions for backend unit tests.

---

## Features

- **Browse all movies** (merged from both providers, paginated, searchable, sortable)
- **View movie details** (all metadata, best price, provider breakdown)
- **Compare prices** (see which provider is cheapest)
- **Resilient backend** (handles provider failures, caches data)
- **Fast, responsive UI** (skeleton loading, error messages, mobile-friendly)
- **Health checks, logging, validation, and Swagger/OpenAPI docs**

---

## How to Run

### Backend (Webjet.Movie.API)

1. **Configure**: Edit `Webjet.Movie.API/appsettings.json` as needed. The default config includes:
   - API keys for providers (see `MovieProviders` section)
   - Caching settings
   - Polly (resilience) settings for each provider (timeout, retry, circuit breaker)
2. **Run with .NET CLI**:
   ```bash
   cd Webjet.Movie.API
   dotnet run
   ```
   - The API will be available at: **http://localhost:5000** (default)
   - Swagger UI: **http://localhost:5000/swagger**
   - Health check: **http://localhost:5000/health**
3. **Or with Docker**:
   ```bash
   cd Webjet.Movie.API
   docker build -t webjet-movie-api .
   docker run -p 5000:8080 webjet-movie-api
   ```

### Frontend (webjet-movie-web)

1. **Install dependencies**:
   ```bash
   cd webjet-movie-web
   npm install
   ```
2. **Start the app**:
   ```bash
   npm start
   ```
   - The app will run at: **http://localhost:3000**
   - It proxies API requests to the backend at **http://localhost:5000**

### Backend Unit Tests

To run unit tests:
   ```
   dotnet test Webjet.Movie.API.Tests
   ```
---

## Backend Configuration (appsettings.json)

```
"MovieProviders": {
  "Cinemaworld": {
    "BaseUrl": "https://.../cinemaworld/",
    "ApiKey": "...",
    "TimeoutSeconds": 2,
    "RetryCount": 3,
    "CircuitBreakerFailures": 3,
    "CircuitBreakerDurationSeconds": 10
  },
  "Filmworld": {
    "BaseUrl": "https://.../filmworld/",
    "ApiKey": "...",
    "TimeoutSeconds": 2,
    "RetryCount": 3,
    "CircuitBreakerFailures": 3,
    "CircuitBreakerDurationSeconds": 10
  }
},
"CacheSettings": {
  "MoviesCacheMinutes": 5,
  "MovieDetailsCacheMinutes": 10
}
```
- **API Key**: Required for both providers, set in `appsettings.json` (never exposed to frontend)
- **Caching**: In-memory, configurable duration for movies and details
- **Polly Policies**: Timeout, retry, and circuit breaker are all configurable per provider

> **Note**: For production environments, consider using Azure Key Vault for sensitive configuration (API keys) and Azure App Configuration for feature flags and application settings. This project uses local configuration files for simplicity.

---

## API Endpoints

- `GET /movies` : Paginated, searchable, sortable list of all movies (merged from both providers)
- `GET /movies/details?title={title}` : Detailed info and price comparison for a movie

---

## Assumptions

- This project uses local configuration files for simplicity. For production environments, Azure Key Vault for sensitive configuration (API keys) and Azure App Configuration for feature flags and application settings shall be used.
- External APIs return a manageable number of movies (can be cached in memory without performance issues)
- Movie and price data does not change frequently; in-memory caching is effective
- The main movie list does **not** show the lowest price, only the details page does
- Backend APIs are public for now; in the future, authentication (e.g., JWT) may be added
- All providers return exactly the same titles for each movie (matching by title is reliable)
- API keys are kept server-side and never exposed to the frontend
- Providers may be flaky; the app is designed to always serve something (from cache or partial data)
- All provider calls are made in parallel for speed
- The solution is designed to be easily extended to more providers
- The backend is stateless except for in-memory cache
- The frontend assumes the backend is available at http://localhost:5000

---

## Project Structure

- **Webjet.Movie.API** : .NET 8 backend (Carter, MediatR, Polly, MemoryCache, Swagger, HealthChecks)
- **Webjet.Movie.API.Tests** : xUnit, Moq, FluentAssertions for backend unit tests
- **webjet-movie-web** : React 19 + TypeScript frontend (Tailwind CSS, Axios, React Router)

---

## Quick Start (Dev)

1. Start backend: `cd Webjet.Movie.API && dotnet run`
2. Start frontend: `cd webjet-movie-web && npm start`
3. Open [http://localhost:3000](http://localhost:3000) in your browser
