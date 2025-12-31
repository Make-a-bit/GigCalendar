# GigCalendar

A full-stack event aggregation platform that automatically scrapes and aggregates live music events from multiple venues across Finland. The application provides a centralized calendar view with advanced filtering capabilities and favorite artist tracking.

## Features

- **Automated Event Scraping**: Background service continuously scrapes 10+ venue websites for upcoming events
- **Smart Event Management**: Intelligent duplicate detection and automatic price update tracking
- **Advanced Filtering**: Filter events by city, venue, and favorite artists
- **Favorite Artists**: Mark favorite artists and highlight their upcoming shows
- **Responsive Design**: Material-UI based interface optimized for desktop and mobile
- **Real-time Updates**: Events are automatically updated with latest information from venue websites
- **SSRF Protection**: Built-in URL validation to prevent security vulnerabilities

## Tech Stack

### Backend

- **.NET 8** - Modern cross-platform framework
- **C# Background Service** - Hosted service pattern with dependency injection
- **HtmlAgilityPack** - HTML parsing and web scraping
- **MySqlConnector** - MySQL database connectivity
- **IHttpClientFactory** - Efficient HTTP client management with connection pooling

### API

- **Node.js** - JavaScript runtime
- **Express 5** - Web application framework
- **MySQL2** - MySQL database driver
- **dotenv** - Environment variable management
- **CORS** - Cross-origin resource sharing

### Frontend

- **React 19** - Modern UI library
- **Material-UI (MUI) 7** - Component library
- **Emotion** - CSS-in-JS styling
- **React Testing Library** - Component testing

### Database

- **MySQL** - Relational database
- **Stored Procedures** - Encapsulated business logic

### DevOps

- **PM2** - Production process manager
- **GitHub Actions** - CI/CD automation
- **ESLint** - Code quality and consistency

## Architecture

The application follows a three-tier architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                     React Frontend                          │
│  (Material-UI, Filtering, Favorite Management)              │
└─────────────────────┬───────────────────────────────────────┘
                      │ HTTP/REST
┌─────────────────────┴───────────────────────────────────────┐
│                   Express.js API                            │
│        (RESTful endpoints, MySQL connection pool)           │
└─────────────────────┬───────────────────────────────────────┘
                      │ MySQL Protocol
┌─────────────────────┴───────────────────────────────────────┐
│                    MySQL Database                           │
│         (Events, Venues, Cities, Stored Procedures)         │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────┴───────────────────────────────────────┐
│              .NET 8 Background Service                      │
│  (Automatic scraper discovery, Rate limiting, Event updates)│
└─────────────────────────────────────────────────────────────┘
```

### Scraper Architecture

The scraper service uses a **base class pattern** with automatic service discovery:

- **BaseScraper**: Shared functionality (URL validation, common properties)
- **Abstract Base Classes**: Venue-specific shared logic (e.g., GLiveLabBaseScraper)
- **Concrete Scrapers**: Individual venue implementations
- **Reflection-based Registration**: Automatic dependency injection of all scrapers
- **Rate Limiting**: Configurable delays to prevent 429 errors
- **Smart Comparison**: Distinguishes between new events and price updates

## Project Structure

```
GigCalendar/
├── API/                                    # Express.js backend
│   ├── config/                             # Configuration files
│   │   └── db.js                           # MySQL connection pool
│   ├── routes/                             # API route handlers
│   │   └── events.js                       # Event endpoints
│   ├── server.js                           # Entry point
│   └── package.json                        # Node.js dependencies
│
├── front/                                  # React frontend
│   ├── public/                             # Static assets
│   ├── src/                                # Source code
│   │   ├── components/                     # React components
│   │   │   ├── Event.jsx                   # Event card component
│   │   │   ├── EventList.jsx               # Event list container
│   │   │   ├── FilterDrawer.jsx            # Filter sidebar
│   │   │   └── FavoriteArtistsManager.jsx  # Favorite artist management
│   │   ├── pages/                          # Page components
│   │   │   └── home.jsx                    # Main application page
│   │   └── App.js                          # Root component
│   └── package.json                        # React dependencies
│
├── Scraper/                                # .NET 8 background service
│   ├── Models/                             # Domain models
│   │   ├── Event.cs                        # Event model with comparison logic
│   │   ├── Venue.cs                        # Venue model
│   │   └── City.cs                         # City model
│   ├── Repositories/                       # Data access layer
│   │   ├── EventRepository.cs              # Event database operations
│   │   ├── VenueRepository.cs              # Venue database operations
│   │   └── CityRepository.cs               # City database operations
│   ├── Services/                           # Business logic
│   │   ├── Scrapers/                       # Web scraper implementations
│   │   │   ├── BaseScraper.cs              # Base class with URL validation
│   │   │   ├── IEventScraper.cs            # Scraper interface
│   │   │   ├── GLiveLabBaseScraper.cs      # Abstract GLiveLab scraper
│   │   │   ├── TavaraAsemaScraper.cs       # Tavara-Asema venue scraper
│   │   │   └── ... (10+ scrapers)          # Additional venue scrapers
│   │   ├── EventInspector.cs               # Smart INSERT/UPDATE logic
│   │   ├── Cleaner.cs                      # HTML entity decoding
│   │   └── ScraperBackgroundService.cs     # Background service host
│   ├── Program.cs                          # DI container & auto-discovery
│   └── Scraper.csproj                      # .NET project file
│
└── README.md                               # Project documentation
```

## Key Implementation Details

### Event Comparison Logic

The application uses two comparison methods to handle events intelligently:

```csharp
// Full equality check (includes price)
public override bool Equals(object obj)

// Same event check (ignores price for update detection)
public bool IsSameEvent(Event other)
```

This allows the system to:

- Skip exact duplicates
- Update prices when they change
- Insert genuinely new events

### Security Features

**SSRF Protection** - All scraped URLs are validated before making HTTP requests:

- Schema validation (http/https only)
- Private IP range blocking (10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16)
- Localhost blocking
- Cloud metadata endpoint blocking (169.254.169.254)

### Rate Limiting

Scrapers implement configurable delays between requests:

- Random delays (3-6 seconds)
- Per-event delays for detail page scraping
- Logged progress indicators

### Timezone Handling

All dates are stored and displayed in Europe/Helsinki timezone:

- Database: `timezone: '+00:00'` config prevents conversion
- API: Returns dates as strings without timezone info
- Frontend: Parses strings directly without Date object conversion

### Deployment

The project uses a GitHub Actions CI/CD pipeline that:

1. Runs ESLint on frontend code
2. Validates build process
3. Automatically deploys on push to master

## License

This project is licensed under the ISC License.

## Acknowledgments

- Venue websites for providing publicly accessible event information
- HtmlAgilityPack for robust HTML parsing
- Material-UI for the component library
- The .NET and React communities
