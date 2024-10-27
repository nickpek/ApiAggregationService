# ApiAggregationService
## Overview
The **API Aggregation Service** is designed to aggregate data from multiple sources, including:
- **Weather API** (e.g., OpenWeather)
- **News API**
- **Football Data API** (e.g., API-Football)

This service combines these datasets into a single, unified API response, allowing consumers to retrieve weather, news, and sports information based on a given city and country.

## Table of Contents
- [Setup](#setup)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [Contributing](#contributing)

## Setup

### Prerequisites
- [.NET 8.0+](https://dotnet.microsoft.com/download)
- API keys for external services:
  - **OpenWeather API**
  - **News API**
  - **API-Football**

### Installation
1. **Clone the Repository**:
   git clone https://github.com/nickpek/ApiAggregationService.git
   cd ApiAggregationService
2. **Install Dependencies**:
    Restore NuGet packages:
    dotnet restore
3. **Set Up Configuration**:
    Create an appsettings.Development.json in the root directory.
    Add your API Football key to the configuration file:
    {
        "ApiKeys": {
                "ApiFootball": "YOUR_API_FOOTBALL_KEY"
        }
    }
4. **Run the application**:
    dotnet run

## Setup
Ensure your configuration files (e.g., appsettings.json and appsettings.Development.json) include your API key and other necessary environment settings.

## API Endpoints

### GET /api/data/aggregated
Fetches aggregated data based on the specified city and country.

Parameters:
- city (string, required): The city for weather data (e.g., "Thessaloniki").
- country (string, required): The country for news and football data (e.g., "Greece").
- sortByTeamName (boolean, optional): If true, sorts football teams by name.

Example Request:
GET http://localhost:5076/api/data/aggregated?city=Thessaloniki&country=Greece&sortByTeamName=true

Response:
- 200 OK: Returns a JSON object with weather, news, and football data, structured as follows:

  {
    "weather": {
    "coord": {
      "lon": 22.9439,
      "lat": 40.6403
    },
    "weather": [
      {
        "id": 802,
        "main": "Clouds",
        "description": "scattered clouds",
        "icon": "03n"
      }
    ],
    "base": "stations",
    "main": {
      "temp": 287.27,
      "feels_like": 286.12,
      "temp_min": 285.48,
      "temp_max": 289.17,
      "pressure": 1026,
      "humidity": 53,
      "sea_level": 1026,
      "grnd_level": 1009
    },
    "visibility": 10000,
    "wind": {
      "speed": 0.45,
      "deg": 69,
      "gust": 0.45
    },
    "clouds": {
      "all": 42
    },
    "dt": 1730064210,
    "sys": {
      "type": 2,
      "id": 2036703,
      "country": "GR",
      "sunrise": 1730004765,
      "sunset": 1730043070
    },
    "timezone": 7200,
    "id": 734077,
    "name": "Thessaloniki",
    "cod": 200
  },
  "news": {
    "status": "ok",
    "totalResults": 38,
    "articles": [
      {
        "source": {
          "id": "associated-press",
          "name": "Associated Press"
        },
        "author": "DARLENE SUPERVILLE, CHRIS MEGERIAN, AAMER MADHANI",
        "title": "'Take our lives seriously,' Michelle Obama pleads as she rallies for Kamala Harris in Michigan - The Associated Press",
        "description": "Michelle Obama delivered a searing speech in support of Kamala Harris during a rally in Kalamazoo, Michigan...",
        "url": "https://apnews.com/article/harris-michelle-obama-biden-michigan-61897674ddc94706031d00aca5c1dc86",
        "urlToImage": "https://dims.apnews.com/dims4/default/7e00c4d/2147483647/strip/true/crop/8640x4860+0+450/resize/1440x810!/quality/90/?url=https%3A%2F%2Fassets.apnews.com%2F4c%2F23%2F6cde48de15c07ea1b91a5802ff17%2F5f0d365030cd4a8c8bada90949f712d0",
        "publishedAt": "2024-10-26T22:27:00Z",
        "content": "KALAMAZOO, Mich. (AP) Michelle Obama challenged men to support Kamala Harris' bid to be Americas first female president..."
      }
    ]
  },
  "football": {
    "response": [
      {
        "team": {
          "id": 15364,
          "name": "Achaiki",
          "code": "ACH",
          "country": "Greece",
          "founded": null,
          "national": false,
          "logo": "https://media.api-sports.io/football/teams/15364.png"
        },
        "venue": {
          "id": 11030,
          "name": "Gipedo Dymis",
          "address": "Dymi",
          "city": "Kato Achaia",
          "capacity": 1500,
          "surface": "grass",
          "image": "https://media.api-sports.io/football/venues/11030.png"
        }
      }
    ]
  }
}

- Error Responses: If an external API call fails, a fallback response is provided for that specific dataset.
Fallback mechanism response:
{
  "weather": {
    "city": "Unknown",
    "description": "Invalid city name provided.",
    "temperature": "N/A",
    "humidity": "N/A"
  },
  "news": {
    "status": "error",
    "message": "Invalid country code provided.",
    "articles": [
      {
        "source": {
          "id": "fallback",
          "name": "Fallback News Source"
        },
        "author": "N/A",
        "title": "News data currently unavailable.",
        "description": "Please try again later.",
        "url": "#",
        "urlToImage": "#",
        "publishedAt": "2024-10-27T22:14:15.0299247Z",
        "content": "N/A"
      }
    ]
  },
  "football": {
    "status": "error",
    "message": "Invalid country code provided.",
    "response": [
      {
        "team": {
          "id": -1,
          "name": "Fallback Team",
          "code": "N/A",
          "country": "Unknown",
          "founded": null,
          "national": false
        }
      }
    ]
  }
}

## Testing

Run the unit tests with the following command:
dotnet test

### Test Coverage
The application includes tests to verify:
- Input Parsing: Validates that country codes and names are correctly parsed.
- API Client Behavior:
  - Max Retry Strategy: Tests fallback response when max retries are reached.
  - Error Handling: Verifies fallback messages for different error scenarios.
  - Data Integrity: Ensures correct response when the API succeeds.
- Data Aggregation:
  - Complete Data Response: Confirms that all API data is returned when calls succeed.
  - Partial Data Handling: Verifies correct behavior when one of the APIs fails.

## Contributing

1. Fork the Repository
2. Create a New Branch
3. Commit Your Changes
4. Push to Your Fork
5. Create a Pull Request

### Contribution Guidelines
- Use clear commit messages.
- Write tests for new features and bug fixes.
- Update documentation as needed.