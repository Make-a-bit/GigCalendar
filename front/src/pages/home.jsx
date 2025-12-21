import React, { useEffect, useState, useMemo, useCallback } from "react";
import { Autocomplete, Box, Button, FormControl, Paper, TextField } from "@mui/material";
import Event from "../components/Event.jsx";
import MonthComponent from "../components/MonthComponent.jsx";
import FavoriteArtists from "../components/favoriteArtists.jsx";
import { isFavoriteArtist, countFavoriteEvents } from "../utils/favoriteHelpers.js";
import theme from "../utils/theme.js";
import FavoriteDrawer from "../components/FavoriteDrawer.jsx";
import FavoriteBorderIcon from "@mui/icons-material/FavoriteBorder";
import FavoriteIcon from "@mui/icons-material/Favorite";

// Function to fetch data from the API
const fetchData = async () => {
  try {
    const apiUrl = process.env.REACT_APP_API_URL;
    const response = await fetch(`${apiUrl}/api/events`);
    const data = await response.json();
    return data;
  } catch (error) {
    console.error("Error fetching data:", error);
  }
};

const Home = () => {
  const [events, setEvents] = useState([]);
  const [selectedCities, setSelectedCities] = useState([]);
  const [selectedVenues, setSelectedVenues] = useState([]);
  const [favoriteArtists, setFavoriteArtists] = useState([]);
  const [showOnlyFavorites, setShowOnlyFavorites] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);

  // Fetch data on component mount
  useEffect(() => {
    const loadEvents = async () => {
      const data = await fetchData();
      setEvents(data || []);
    };
    loadEvents();
  }, []);

  useEffect(() => {
    if (favoriteArtists.length === 0 && showOnlyFavorites) {
      setShowOnlyFavorites(false);
    }
  }, [favoriteArtists, showOnlyFavorites]);

  // Get unique cities:
  const getUniqueCities = (events) => {
    const cities = [...new Set(events.map((event) => event.city_name))];
    return cities.sort() || [];
  };

  // Get filtered venues
  const getFilteredVenues = (events, selectedCities) => {
    let eventsToProcess = events;

    // If cities are selected, filter events by those cities first
    if (selectedCities.length > 0) {
      eventsToProcess = events.filter((event) => selectedCities.includes(event.city_name));
    }

    const venues = [...new Set(eventsToProcess.map((event) => event.venue_name))];
    return venues.sort() || [];
  };

  // Filter events based on selected venue
  const filteredEvents = useMemo(() => {
    return events.filter((event) => {
      const cityMatch = selectedCities.length === 0 || selectedCities.includes(event.city_name);
      const venueMatch = selectedVenues.length === 0 || selectedVenues.includes(event.venue_name);
      const favoriteMatch =
        !showOnlyFavorites || isFavoriteArtist(event.event_artist, favoriteArtists);

      return cityMatch && venueMatch && favoriteMatch;
    });
  }, [events, selectedCities, selectedVenues, showOnlyFavorites, favoriteArtists]);

  // Group events by month
  const groupEventsByMonth = (events) => {
    const grouped = {};

    // Iterate through events and group them by month-year
    events.forEach((event) => {
      const date = new Date(event.event_date);
      const monthYear = date.toLocaleDateString("fi-FI", {
        year: "numeric",
        month: "long",
      });

      if (!grouped[monthYear]) {
        grouped[monthYear] = [];
      }
      grouped[monthYear].push(event);
    });

    return grouped;
  };

  // Get grouped events
  const groupedEvents = groupEventsByMonth(filteredEvents);

  const handleFavoritesChange = useCallback((newFavorites) => {
    setFavoriteArtists(newFavorites);
  }, []);

  return (
    <Box
      component="main"
      sx={{
        borderRadius: 2,
        display: "flex",
        justifyContent: "center",
        bgcolor: theme.background,
        minHeight: "100vh",
        py: 4,
      }}
    >
      <Paper
        elevation={5}
        sx={{
          background: theme.surface,
          borderRadius: 5,
          fontFamily: "Montserrat, Arial, sans-serif",
          maxWidth: 700,
          width: "100%",
          m: { xs: 1, sm: 2, md: 4 },
          p: { xs: 2, sm: 3, md: 4 },
        }}
      >
        <Box sx={{ textAlign: "center", mb: 4 }}>
          <h1
            style={{
              fontFamily: "Montserrat, sans-serif",
              fontSize: "2.3rem",
              fontWeight: 800,
              color: theme.text,
              margin: 0,
              marginBottom: "10px",
            }}
          >
            Keikkakalenteri
          </h1>
          <Box
            sx={{
              width: "280px",
              height: "4px",
              background: `linear-gradient(90deg, ${theme.primary}, ${theme.accent})`,
              margin: "0 auto",
              borderRadius: "2px",
            }}
          />
        </Box>

        {/* Favorite controls */}
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            gap: 2,
            mb: 3,
            flexWrap: "wrap",
          }}
        >
          {/* Button to open favorites drawer */}
          <Button
            variant="outlined"
            onClick={() => setDrawerOpen(true)}
            startIcon={favoriteArtists.length > 0 ? <FavoriteIcon /> : <FavoriteBorderIcon />}
            sx={{
              borderColor: theme.primary,
              color: theme.primary,
              "&:hover": {
                borderColor: theme.accent,
                backgroundColor: "rgba(255, 99, 132, 0.1)",
              },
            }}
          >
            Suosikit ({favoriteArtists.length})
          </Button>

          {/* Toggle to show only favorites */}
          {favoriteArtists.length > 0 && (
            <Button
              variant={showOnlyFavorites ? "contained" : "outlined"}
              onClick={() => setShowOnlyFavorites(!showOnlyFavorites)}
              sx={{
                backgroundColor: showOnlyFavorites ? theme.primary : "transparent",
                color: showOnlyFavorites ? "#fff" : theme.primary,
                borderColor: theme.primary,
                "&:hover": {
                  backgroundColor: showOnlyFavorites ? theme.accent : "rgba(255, 99, 132, 0.1)",
                },
              }}
            >
              {showOnlyFavorites ? "Näytä kaikki" : "Näytä vain suosikit"}
            </Button>
          )}
        </Box>

        <FormControl
          sx={{
            mt: 2,
            width: "100%",
            maxWidth: 300,
          }}
        >
          <Autocomplete
            multiple
            id="city-filter"
            options={getUniqueCities(events)}
            value={selectedCities}
            onChange={(event, newValue) => {
              setSelectedCities(newValue);

              // Clear selected venues if they're not in the new city selection
              if (newValue.length > 0 && selectedVenues.length > 0) {
                const validVenues = getFilteredVenues(events, newValue);
                const stillValidVenues = selectedVenues.filter((venue) =>
                  validVenues.includes(venue)
                );
                setSelectedVenues(stillValidVenues);
              }
            }}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Valitse kaupungit"
                placeholder={selectedCities.length === 0 ? "Valitse kaupungit" : ""}
              />
            )}
            sx={{
              mt: 5,
              width: "100%",
              maxWidth: 300,
            }}
          />

          <Autocomplete
            multiple
            id="venue-filter"
            options={getFilteredVenues(events, selectedCities)}
            value={selectedVenues}
            onChange={(event, newValue) => setSelectedVenues(newValue)}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Valitse tapahtumapaikat"
                placeholder={selectedVenues.length === 0 ? "Valitse tapahtumapaikat" : ""}
              />
            )}
            sx={{
              mt: "10px",
              width: "100%",
              maxWidth: 300,
            }}
          />
        </FormControl>

        {/* Display grouped events by month */}
        {Object.keys(groupedEvents).length > 0 ? (
          Object.entries(groupedEvents).map(([month, monthEvents]) => (
            <Box component="section" key={month} sx={{ mb: 3 }}>
              <MonthComponent month={month} theme={theme} />
              {monthEvents.map((event) => (
                <Event
                  key={event.event_id}
                  props={event}
                  theme={theme}
                  isFavorite={isFavoriteArtist(event.event_artist, favoriteArtists)}
                />
              ))}
            </Box>
          ))
        ) : (
          <Box sx={{ textAlign: "center", color: theme.textSecondary, py: 4 }}>Ei keikkoja</Box>
        )}
      </Paper>

      {/* Drawer for managing favorites */}
      <FavoriteDrawer
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
        theme={theme}
        onFavoritesChange={handleFavoritesChange}
      />
    </Box>
  );
};

export { Home, theme };
